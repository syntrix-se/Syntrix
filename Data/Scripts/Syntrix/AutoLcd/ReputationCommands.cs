// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace Syntrix.AutoLcd
{
    /// <summary>
    /// Reputation [tags ...] [Pilot] and ReputationTop n [tags ...] [Pilot]: game reputation of the screen owner (or
    /// of the pilot of the screen block with "Pilot") with every NPC faction, sorted best first. Range -1500 to 1500,
    /// hostile below -500, friendly above 500. Other mods can replace the command through the provider interface.
    /// </summary>
    internal static class ReputationCommands
    {
        private const int Min = -1500;
        private const int Max = 1500;

        public static void Register()
        {
            CommandRegistry.Add("Reputation", CommandRegistry.Heavy, c => Run(c, false));
            CommandRegistry.Add("ReputationTop", CommandRegistry.Heavy, c => Run(c, true));
        }

        private static IEnumerable<int> Run(CommandContext context, bool top)
        {
            int wanted = int.MaxValue;
            var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            bool pilot = false;
            for (int i = 0; i < context.Line.Args.Count; i++)
            {
                string arg = context.Line.Args[i];
                double number;
                if (top && i == 0 && Formatting.TryNumber(arg, out number))
                    wanted = Math.Max(1, (int)number);
                else if (string.Equals(arg, "Pilot", StringComparison.OrdinalIgnoreCase))
                    pilot = true;
                else
                    foreach (string tag in arg.Split(','))
                        if (tag.Length > 0)
                            tags.Add(tag);
            }
            if (top && wanted == int.MaxValue)
                wanted = 5;

            long identity = context.Source.Owner;
            if (pilot)
            {
                var player = MyAPIGateway.Players.GetPlayerControllingEntity(context.ScreenBlock);
                if (player == null)
                {
                    context.Canvas.Text(context.T("SyxAutoLcd_NoPilot", "Nobody is seated."));
                    yield break;
                }
                identity = player.IdentityId;
            }

            var rows = new List<KeyValuePair<int, IMyFaction>>();
            foreach (var kv in MyAPIGateway.Session.Factions.Factions)
            {
                var faction = kv.Value;
                if (!faction.IsEveryoneNpc())
                    continue;
                if (tags.Count > 0 && !tags.Contains(faction.Tag))
                    continue;
                int reputation = MyAPIGateway.Session.Factions.GetReputationBetweenPlayerAndFaction(identity, faction.FactionId);
                rows.Add(new KeyValuePair<int, IMyFaction>(reputation, faction));
            }
            yield return 0;
            rows.Sort((a, b) =>
            {
                int c = b.Key.CompareTo(a.Key);
                return c != 0 ? c : string.Compare(a.Value.Tag, b.Value.Tag, StringComparison.OrdinalIgnoreCase);
            });
            if (rows.Count == 0)
            {
                context.Canvas.Text(context.T("SyxAutoLcd_NoFactions", "No factions found."));
                yield break;
            }
            for (int i = 0; i < rows.Count && i < wanted; i++)
            {
                int reputation = rows[i].Key;
                var faction = rows[i].Value;
                string state = reputation < -500
                    ? context.T("SyxAutoLcd_Hostile", "hostile")
                    : reputation > 500 ? context.T("SyxAutoLcd_Friendly", "friendly") : context.T("SyxAutoLcd_Neutral", "neutral");
                context.Canvas.Pair(faction.Tag + " " + faction.Name, state);
                double fraction = (double)(reputation - Min) / (Max - Min);
                context.Canvas.PairBar("", fraction, reputation.ToString(LcdText.Culture));
            }
        }
    }
}
