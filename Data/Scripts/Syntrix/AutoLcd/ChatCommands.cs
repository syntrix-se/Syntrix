// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;

namespace Syntrix.AutoLcd
{
    /// <summary>
    /// Chat command /syxlcd (runs on the server): help, scroll up|down "screen" [lines] (screens within view
    /// distance the player may use), stats, reload, pause, resume (admins).
    /// </summary>
    internal static class ChatCommands
    {
        public static string Handle(ulong sender, string[] args)
        {
            try
            {
                var words = CommandLine.Split(string.Join(" ", args ?? new string[0]));
                string sub = words.Count > 0 ? words[0].ToLowerInvariant() : "help";
                bool admin = sender == 0 || AutoLcdCore.Syx.PromoteLevel(sender) >= MyPromoteLevel.Admin;
                switch (sub)
                {
                    case "scroll":
                        return Scroll(sender, words);
                    case "stats":
                        return admin ? Stats() : NoRight();
                    case "reload":
                        if (!admin)
                            return NoRight();
                        AutoLcdCore.Syx.SettingsLoad(AutoLcdCore.SettingsFile, AutoLcdSettings.DefaultText, true, AutoLcdSession.ApplySettings);
                        return LcdText.Get("SyxAutoLcd_ChatReloaded", "AutoLCD settings read again.");
                    case "pause":
                        if (!admin)
                            return NoRight();
                        AutoLcdCore.Paused = true;
                        return LcdText.Get("SyxAutoLcd_ChatPaused", "AutoLCD paused: screens are not updated.");
                    case "resume":
                        if (!admin)
                            return NoRight();
                        AutoLcdCore.Paused = false;
                        return LcdText.Get("SyxAutoLcd_ChatResumed", "AutoLCD runs again.");
                    default:
                        return LcdText.Get("SyxAutoLcd_ChatHelp",
                            "/syxlcd scroll up|down \"screen name\" [lines] - scroll screens near you\n"
                            + "/syxlcd stats - load per construct (admin)\n"
                            + "/syxlcd reload - read Syntrix-AutoLcd.xml again (admin)\n"
                            + "/syxlcd pause | resume - stop or restart all screens (admin)\n"
                            + "Screens: !LCD! or [LCD] in the name, commands in the custom data.");
                }
            }
            catch (Exception e)
            {
                AutoLcdCore.Log.Error("chat", e);
                return "AutoLCD: error, see log.";
            }
        }

        private static string NoRight()
        {
            return LcdText.Get("SyxAutoLcd_ChatAdminOnly", "Only admins may do that.");
        }

        private static string Scroll(ulong sender, List<string> words)
        {
            if (words.Count < 3)
                return LcdText.Get("SyxAutoLcd_ChatScrollUsage", "Use: /syxlcd scroll up|down \"screen name\" [lines]");
            bool up = string.Equals(words[1], "up", StringComparison.OrdinalIgnoreCase);
            int lines = 1;
            if (words.Count >= 4)
                int.TryParse(words[3], out lines);
            lines = Math.Max(1, lines);
            var players = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(players, p => p.SteamUserId == sender);
            if (players.Count == 0 || players[0].Character == null)
                return LcdText.Get("SyxAutoLcd_ChatNoPlayer", "No character found.");
            var player = players[0];
            Vector3D position = player.Character.GetPosition();
            double range = AutoLcdCore.Settings.ViewDistance;
            int count = 0;
            foreach (var construct in ScreenRegistry.Constructs)
            {
                foreach (var screen in construct.Screens)
                {
                    var block = screen.Block;
                    if (screen.Master != null || block.Closed || block.CustomName == null)
                        continue;
                    if (block.CustomName.IndexOf(words[2], StringComparison.Ordinal) < 0 || !block.HasPlayerAccess(player.IdentityId))
                        continue;
                    if (Vector3D.Distance(block.GetPosition(), position) > range)
                        continue;
                    screen.Scroll(up ? -lines : lines);
                    count++;
                }
            }
            return LcdText.Format("SyxAutoLcd_ChatScrolled", "{0} screens scrolled.", count);
        }

        private static string Stats()
        {
            var text = new StringBuilder();
            text.Append(LcdText.Format("SyxAutoLcd_ChatStats", "AutoLCD: {0} screens on {1} blocks, {2} programmable blocks, {3} provider commands.",
                ScreenRegistry.ScreenCount, ScreenRegistry.BlockCount, ScreenRegistry.ProgramCount, CommandRegistry.ProviderCount));
            var list = new List<ScreenRegistry.Construct>(ScreenRegistry.Constructs);
            list.Sort((a, b) => b.Milliseconds.CompareTo(a.Milliseconds));
            for (int i = 0; i < list.Count && i < 10; i++)
                text.Append('\n').Append(list[i].Name).Append(": ").Append(list[i].Screens.Count).Append(" / ")
                    .Append(list[i].Milliseconds.ToString("0.0", LcdText.Culture)).Append(" ms");
            if (AutoLcdCore.Paused)
                text.Append('\n').Append(LcdText.Get("SyxAutoLcd_ChatIsPaused", "(paused)"));
            ScreenRegistry.ResetStatistics();
            return text.ToString();
        }
    }
}
