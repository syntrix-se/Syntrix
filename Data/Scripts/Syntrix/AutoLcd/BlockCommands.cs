// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Ingame = Sandbox.ModAPI.Ingame;
using GameIngame = SpaceEngineers.Game.ModAPI.Ingame;

namespace Syntrix.AutoLcd
{
    /// <summary>
    /// BlockCount, EnabledCount, ProdCount, Working, Damage, Docked, Occupied, Dampeners. State words of Working
    /// filters are English and fixed (on, off, idle, work, open, closed, lock, unlock, ready, leak, connected ...),
    /// the shown state text is translated.
    /// </summary>
    internal static class BlockCommands
    {
        private const int Chunk = 32;

        public static void Register()
        {
            CommandRegistry.Add("BlockCount", CommandRegistry.Heavy, c => Count(c, 0));
            CommandRegistry.Add("EnabledCount", CommandRegistry.Heavy, c => Count(c, 1));
            CommandRegistry.Add("ProdCount", CommandRegistry.Normal, c => Count(c, 2));
            CommandRegistry.Add("Working", CommandRegistry.Normal, Working);
            CommandRegistry.Add("Damage", CommandRegistry.Heavy, Damage);
            CommandRegistry.Add("Docked", CommandRegistry.Normal, Docked);
            CommandRegistry.Add("Occupied", CommandRegistry.Fast, Occupied);
            CommandRegistry.Add("Dampeners", CommandRegistry.Fast, Dampeners);
        }

        // mode 0 all, 1 enabled, 2 producing.
        private static IEnumerable<int> Count(CommandContext context, int mode)
        {
            var words = BlockTypes.Words(context.Line, 1);
            var counts = new Dictionary<string, int>();
            var order = new List<string>();
            int n = 0;
            foreach (var block in context.Blocks(context.Line.Arg(0, "*"), b => BlockTypes.MatchesAny(b, words)))
            {
                if (++n % Chunk == 0)
                    yield return 0;
                if (mode == 1)
                {
                    var functional = block as Ingame.IMyFunctionalBlock;
                    if (functional == null || !functional.Enabled)
                        continue;
                }
                else if (mode == 2)
                {
                    var production = block as Ingame.IMyProductionBlock;
                    if (production == null || !production.IsProducing)
                        continue;
                }
                string name = block.DefinitionDisplayNameText;
                int count;
                if (!counts.TryGetValue(name, out count))
                    order.Add(name);
                counts[name] = count + 1;
            }
            order.Sort(StringComparer.CurrentCultureIgnoreCase);
            foreach (string name in order)
                context.Canvas.Pair(name, counts[name].ToString(LcdText.Culture));
            if (order.Count == 0)
                context.Canvas.Pair(context.T("SyxAutoLcd_Blocks", "Blocks"), "0");
        }

        /// <summary>State words of a block: on/off first, then the specific state if any.</summary>
        public static string State(IMyTerminalBlock block, out string onOff, out string extra)
        {
            extra = "";
            var functional = block as Ingame.IMyFunctionalBlock;
            bool on = functional != null ? functional.Enabled : block.IsWorking;
            onOff = on ? "on" : "off";

            var door = block as Ingame.IMyDoor;
            if (door != null)
                return door.Status == Ingame.DoorStatus.Open || door.Status == Ingame.DoorStatus.Opening ? "open" : "closed";
            var connector = block as Ingame.IMyShipConnector;
            if (connector != null)
            {
                if (connector.Status == Ingame.MyShipConnectorStatus.Connected)
                    return "lock";
                return connector.Status == Ingame.MyShipConnectorStatus.Connectable ? "ready" : "unlock";
            }
            var gear = block as GameIngame.IMyLandingGear;
            if (gear != null)
            {
                if (gear.LockMode == GameIngame.LandingGearMode.Locked)
                    return "lock";
                return gear.LockMode == GameIngame.LandingGearMode.ReadyToLock ? "ready" : "unlock";
            }
            if (!on)
                return "off";
            var laser = block as Ingame.IMyLaserAntenna;
            if (laser != null)
            {
                switch (laser.Status)
                {
                    case Ingame.MyLaserAntennaStatus.RotatingToTarget: return "rotating";
                    case Ingame.MyLaserAntennaStatus.SearchingTargetForAntenna: return "searching";
                    case Ingame.MyLaserAntennaStatus.Connecting: return "connecting";
                    case Ingame.MyLaserAntennaStatus.Connected: return "connected";
                    case Ingame.MyLaserAntennaStatus.OutOfRange: return "outofrange";
                    default: return "idle";
                }
            }
            var production = block as Ingame.IMyProductionBlock;
            if (production != null)
                return production.IsProducing ? "work" : "idle";
            var vent = block as GameIngame.IMyAirVent;
            if (vent != null)
            {
                if (!vent.CanPressurize)
                    return "leak";
                extra = Formatting.Percent(vent.GetOxygenLevel());
                return "on";
            }
            var battery = block as Ingame.IMyBatteryBlock;
            if (battery != null)
            {
                double fraction = battery.MaxStoredPower > 0 ? battery.CurrentStoredPower / battery.MaxStoredPower : 0;
                extra = (battery.IsCharging ? "(+) " : "(-) ") + Formatting.Percent(fraction);
                return "on";
            }
            var projector = block as Ingame.IMyProjector;
            if (projector != null && projector.IsProjecting && projector.TotalBlocks > 0)
                extra = Formatting.Percent(1 - (double)projector.RemainingBlocks / projector.TotalBlocks);
            return "on";
        }

        /// <summary>Translated text of a state word.</summary>
        public static string StateText(string state)
        {
            switch (state)
            {
                case "on": return LcdText.Get("SyxAutoLcd_StateOn", "ON");
                case "off": return LcdText.Get("SyxAutoLcd_StateOff", "OFF");
                case "idle": return LcdText.Get("SyxAutoLcd_StateIdle", "IDLE");
                case "work": return LcdText.Get("SyxAutoLcd_StateWork", "WORK");
                case "open": return LcdText.Get("SyxAutoLcd_StateOpen", "OPEN");
                case "closed": return LcdText.Get("SyxAutoLcd_StateClosed", "CLOSED");
                case "lock": return LcdText.Get("SyxAutoLcd_StateLock", "LOCK");
                case "unlock": return LcdText.Get("SyxAutoLcd_StateUnlock", "UNLOCK");
                case "ready": return LcdText.Get("SyxAutoLcd_StateReady", "READY");
                case "leak": return LcdText.Get("SyxAutoLcd_StateLeak", "LEAK");
                case "rotating": return LcdText.Get("SyxAutoLcd_StateRotating", "ROTATING");
                case "searching": return LcdText.Get("SyxAutoLcd_StateSearching", "SEARCHING");
                case "connecting": return LcdText.Get("SyxAutoLcd_StateConnecting", "CONNECTING");
                case "connected": return LcdText.Get("SyxAutoLcd_StateConnected", "CONNECTED");
                case "outofrange": return LcdText.Get("SyxAutoLcd_StateOutOfRange", "OUT OF RANGE");
                default: return state.ToUpperInvariant();
            }
        }

        private static IEnumerable<int> Working(CommandContext context)
        {
            bool onOffOnly = context.SuffixIs("X");
            var words = BlockTypes.Words(context.Line, 1);
            int n = 0;
            foreach (var block in context.Blocks(context.Line.Arg(0, "*"), b => BlockTypes.MatchesAny(b, words)))
            {
                if (++n % Chunk == 0)
                    yield return 0;
                string onOff, extra;
                string state = State(block, out onOff, out extra);
                if (!StateAllowed(block, words, onOffOnly ? onOff : state, onOff))
                    continue;
                string text = StateText(onOffOnly ? onOff : state);
                if (extra.Length > 0)
                    text = state == "on" && block is Ingame.IMyBatteryBlock ? extra : text + " " + extra;
                context.Canvas.Pair(block.CustomName, text);
            }
        }

        // A word "type:state" or ":state" limits the states shown for its type; types without state show all.
        private static bool StateAllowed(IMyTerminalBlock block, List<string> words, string state, string onOff)
        {
            if (words.Count == 0)
                return true;
            bool limited = false;
            foreach (string word in words)
            {
                int colon = word.IndexOf(':');
                string type = colon >= 0 ? word.Substring(0, colon) : word;
                if (type.Length > 0 && !BlockTypes.Matches(block, type))
                    continue;
                if (colon < 0)
                    return true;
                limited = true;
                string wanted = word.Substring(colon + 1).ToLowerInvariant();
                if (wanted == state || wanted == onOff)
                    return true;
            }
            return !limited;
        }

        private static IEnumerable<int> Damage(CommandContext context)
        {
            string suffix = context.Suffix;
            bool percentOnly = suffix.StartsWith("X", StringComparison.Ordinal);
            if (percentOnly)
                suffix = suffix.Substring(1);
            bool onlyDamaged = suffix == "NOC";
            bool onlyBuilding = suffix == "C";
            double limit;
            if (!Formatting.TryNumber(context.Line.Arg(1, "100"), out limit))
                limit = 100;
            int n = 0, shown = 0;
            foreach (var block in context.Blocks(context.Line.Arg(0, "*")))
            {
                if (++n % Chunk == 0)
                    yield return 0;
                var slim = block.SlimBlock;
                if (slim == null || slim.MaxIntegrity <= 0)
                    continue;
                bool damaged = slim.CurrentDamage > 0;
                bool building = slim.BuildIntegrity < slim.MaxIntegrity - 0.0001f;
                if (onlyDamaged ? !damaged : onlyBuilding ? !building : !(damaged || building))
                    continue;
                double integrity = slim.Integrity;
                double fraction = integrity / slim.MaxIntegrity;
                if (fraction * 100 >= limit && limit < 100)
                    continue;
                shown++;
                if (percentOnly)
                    context.Canvas.PairBar(block.CustomName, fraction, Formatting.Percent(fraction));
                else
                {
                    context.Canvas.Pair(block.CustomName, Formatting.Amount(integrity) + " / " + Formatting.Amount(slim.MaxIntegrity));
                    context.Canvas.PairBar("", fraction, Formatting.Percent(fraction));
                }
            }
            if (shown == 0)
                context.Canvas.Text(context.T("SyxAutoLcd_NoDamage", "No damaged blocks found."));
        }

        private static IEnumerable<int> Docked(CommandContext context)
        {
            bool empty = context.Suffix.EndsWith("E", StringComparison.Ordinal);
            bool names = context.Suffix.StartsWith("CN", StringComparison.Ordinal);
            int n = 0, shown = 0;
            foreach (var block in context.Blocks(context.Line.Arg(0, "*"), b => b is Ingame.IMyShipConnector))
            {
                if (++n % Chunk == 0)
                    yield return 0;
                var connector = (Ingame.IMyShipConnector)block;
                string ship = null;
                if (connector.Status == Ingame.MyShipConnectorStatus.Connected && connector.OtherConnector != null)
                    ship = connector.OtherConnector.CubeGrid.CustomName;
                if (ship == null && !empty)
                    continue;
                shown++;
                if (names)
                    context.Canvas.Pair(block.CustomName, ship ?? "-");
                else
                    context.Canvas.Text(ship ?? "-");
            }
            if (shown == 0)
                context.Canvas.Text(context.T("SyxAutoLcd_NothingDocked", "Nothing docked."));
        }

        private static IEnumerable<int> Occupied(CommandContext context)
        {
            IMyTerminalBlock found = null;
            foreach (var block in context.Blocks(context.Line.Arg(0, "*"), b => b is Ingame.IMyShipController))
            {
                found = block;
                break;
            }
            if (found == null)
            {
                context.Canvas.Text(context.T("SyxAutoLcd_NoController", "No ship controller found."));
                yield break;
            }
            var controller = (Ingame.IMyShipController)found;
            var cockpit = found as IMyCockpit;
            bool occupied = controller.IsUnderControl || (cockpit != null && cockpit.Pilot != null);
            context.Canvas.Pair(found.CustomName, occupied
                ? context.T("SyxAutoLcd_Occupied", "occupied")
                : context.T("SyxAutoLcd_Free", "free"));
        }

        private static IEnumerable<int> Dampeners(CommandContext context)
        {
            var controller = CargoCommands.Controller(context);
            string title = context.T("SyxAutoLcd_Dampeners", "Inertia dampeners:");
            if (controller == null)
                context.Canvas.Pair(title, "-");
            else
                context.Canvas.Pair(title, StateText(controller.DampenersOverride ? "on" : "off"));
            yield break;
        }
    }
}
