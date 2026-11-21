// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game.ModAPI.Ingame;
using IMyBatteryBlock = Sandbox.ModAPI.Ingame.IMyBatteryBlock;
using IMyPowerProducer = Sandbox.ModAPI.Ingame.IMyPowerProducer;
using IMyReactor = Sandbox.ModAPI.Ingame.IMyReactor;
using IMySolarPanel = SpaceEngineers.Game.ModAPI.Ingame.IMySolarPanel;

namespace Syntrix.AutoLcd
{
    /// <summary>
    /// Power, PowerStored, PowerSummary, PowerUsed, PowerUsedTop, PowerTime. Reactors count every block of the
    /// reactor type, also modded fuel generators; their fuel energy comes from the reactor definition
    /// (FuelProductionToCapacityMultiplier and the ratio of each fuel), so any fuel item counts.
    /// </summary>
    internal static class PowerCommands
    {
        private const int Chunk = 32;

        private static readonly MyDefinitionId _electricity = new MyDefinitionId(typeof(MyObjectBuilder_GasProperties), "Electricity");

        private enum Kind
        {
            Reactor,
            Engine,
            Turbine,
            Solar,
            Battery,
            Other,
        }

        public static void Register()
        {
            CommandRegistry.Add("Power", CommandRegistry.Normal, Power);
            CommandRegistry.Add("PowerStored", CommandRegistry.Normal, Stored);
            CommandRegistry.Add("PowerSummary", CommandRegistry.Normal, Summary);
            CommandRegistry.Add("PowerUsed", CommandRegistry.Heavy, Used);
            CommandRegistry.Add("PowerUsedTop", CommandRegistry.Heavy, UsedTop);
            CommandRegistry.Add("PowerTime", CommandRegistry.Normal, Time);
        }

        /// <summary>Electric input of a block now and at most (MW), from its resource sink.</summary>
        public static bool Input(IMyTerminalBlock block, out double current, out double max)
        {
            current = max = 0;
            var sink = block.Components.Get<MyResourceSinkComponent>();
            if (sink == null)
                return false;
            bool accepts = false;
            foreach (var id in sink.AcceptedResources)
                if (id == _electricity)
                    accepts = true;
            if (!accepts)
                return false;
            current = sink.CurrentInputByType(_electricity);
            max = sink.MaxRequiredInputByType(_electricity);
            return true;
        }

        private static Kind KindOf(IMyTerminalBlock block)
        {
            if (block is IMyBatteryBlock)
                return Kind.Battery;
            if (block is IMyReactor)
                return Kind.Reactor;
            if (block is IMySolarPanel)
                return Kind.Solar;
            string type = BlockTypes.TypeOf(block);
            if (type == "HydrogenEngine")
                return Kind.Engine;
            if (type == "WindTurbine")
                return Kind.Turbine;
            return Kind.Other;
        }

        private static string Title(CommandContext context, Kind kind)
        {
            switch (kind)
            {
                case Kind.Reactor: return context.T("SyxAutoLcd_Reactors", "Reactors:");
                case Kind.Engine: return context.T("SyxAutoLcd_Engines", "Engines:");
                case Kind.Turbine: return context.T("SyxAutoLcd_Turbines", "Turbines:");
                case Kind.Solar: return context.T("SyxAutoLcd_Solars", "Solars:");
                case Kind.Battery: return context.T("SyxAutoLcd_Batteries", "Batteries:");
                default: return context.T("SyxAutoLcd_Generators", "Generators:");
            }
        }

        private static IEnumerable<int> Power(CommandContext context)
        {
            string suffix = context.Suffix;
            bool onlyIn = suffix.StartsWith("IN", StringComparison.Ordinal);
            bool onlyOut = suffix.StartsWith("OUT", StringComparison.Ordinal);
            if (onlyIn)
                suffix = suffix.Substring(2);
            else if (onlyOut)
                suffix = suffix.Substring(3);
            var mode = context.Mode(suffix);
            var current = new double[6];
            var max = new double[6];
            var counts = new int[6];
            double batteryIn = 0, batteryInMax = 0;
            var blocks = context.Blocks(context.Line.Arg(0, "*"), b => b is IMyPowerProducer);
            int n = 0;
            foreach (var block in blocks)
            {
                var producer = (IMyPowerProducer)block;
                int kind = (int)KindOf(block);
                current[kind] += producer.CurrentOutput;
                max[kind] += producer.MaxOutput;
                counts[kind]++;
                var battery = block as IMyBatteryBlock;
                if (battery != null)
                {
                    batteryIn += battery.CurrentInput;
                    batteryInMax += battery.MaxInput;
                }
                if (++n % Chunk == 0)
                    yield return 0;
            }
            string ownTitle = context.Line.Arg(1, null);
            if (!onlyIn && !onlyOut)
            {
                foreach (Kind kind in new[] { Kind.Reactor, Kind.Engine, Kind.Turbine, Kind.Solar, Kind.Other })
                {
                    int k = (int)kind;
                    if (counts[k] == 0)
                        continue;
                    ShowPower(context, ownTitle ?? Title(context, kind), counts[k], current[k], max[k], mode);
                }
            }
            int b2 = (int)Kind.Battery;
            if (counts[b2] > 0)
            {
                string title = ownTitle ?? Title(context, Kind.Battery);
                if (!onlyIn)
                    ShowPower(context, title + " " + context.T("SyxAutoLcd_Output", "out"), counts[b2], current[b2], max[b2], mode);
                if (!onlyOut)
                    ShowPower(context, title + " " + context.T("SyxAutoLcd_Input", "in"), counts[b2], batteryIn, batteryInMax, mode);
            }
        }

        private static void ShowPower(CommandContext context, string title, int count, double current, double max, StatMode mode)
        {
            string label = count > 1 ? title + " (" + count + ")" : title;
            context.Canvas.Stat(label, Formatting.Power(current) + " / " + Formatting.Power(max), max > 0 ? current / max : 0, mode);
        }

        private static IEnumerable<int> Stored(CommandContext context)
        {
            double stored = 0, max = 0;
            int count = 0;
            foreach (var block in context.Blocks(context.Line.Arg(0, "*"), b => b is IMyBatteryBlock))
            {
                var battery = (IMyBatteryBlock)block;
                stored += battery.CurrentStoredPower;
                max += battery.MaxStoredPower;
                if (++count % Chunk == 0)
                    yield return 0;
            }
            string title = context.Line.Arg(1, context.T("SyxAutoLcd_StoredPower", "Stored power:"));
            context.Canvas.Stat(title, Formatting.Energy(stored) + " / " + Formatting.Energy(max), max > 0 ? stored / max : 0, context.Mode(context.Suffix));
        }

        private static IEnumerable<int> Summary(CommandContext context)
        {
            double current = 0, max = 0;
            int count = 0;
            foreach (var block in context.Blocks(context.Line.Arg(0, "*"), b => b is IMyPowerProducer))
            {
                var producer = (IMyPowerProducer)block;
                current += producer.CurrentOutput;
                max += producer.MaxOutput;
                if (++count % Chunk == 0)
                    yield return 0;
            }
            string title = context.Line.Arg(1, context.T("SyxAutoLcd_TotalOutput", "Total output:"));
            context.Canvas.Stat(title, Formatting.Power(current) + " / " + Formatting.Power(max), max > 0 ? current / max : 0, context.Mode(context.Suffix));
        }

        private static IEnumerable<int> Used(CommandContext context)
        {
            double current = 0, max = 0;
            int count = 0;
            foreach (var block in context.Blocks(context.Line.Arg(0, "*"), b => !(b is IMyPowerProducer)))
            {
                double now, most;
                if (Input(block, out now, out most))
                {
                    current += now;
                    max += most;
                }
                if (++count % Chunk == 0)
                    yield return 0;
            }
            string title = context.Line.Arg(1, context.T("SyxAutoLcd_PowerUsed", "Power used:"));
            context.Canvas.Stat(title, Formatting.Power(current) + " / " + Formatting.Power(max), max > 0 ? current / max : 0, context.Mode(context.Suffix));
        }

        private static IEnumerable<int> UsedTop(CommandContext context)
        {
            double top;
            int wanted = Formatting.TryNumber(context.Line.Arg(1, "5"), out top) ? Math.Max(1, (int)top) : 5;
            var users = new List<KeyValuePair<double, IMyTerminalBlock>>();
            var maxima = new Dictionary<long, double>();
            int count = 0;
            foreach (var block in context.Blocks(context.Line.Arg(0, "*"), b => !(b is IMyPowerProducer)))
            {
                double now, most;
                if (Input(block, out now, out most) && now > 0)
                {
                    users.Add(new KeyValuePair<double, IMyTerminalBlock>(now, block));
                    maxima[block.EntityId] = most;
                }
                if (++count % Chunk == 0)
                    yield return 0;
            }
            users.Sort((a, b) => b.Key.CompareTo(a.Key));
            for (int i = 0; i < users.Count && i < wanted; i++)
            {
                double most = maxima[users[i].Value.EntityId];
                context.Canvas.PairBar(users[i].Value.CustomName, most > 0 ? users[i].Key / most : 0, Formatting.Power(users[i].Key));
            }
        }

        private static IEnumerable<int> Time(CommandContext context)
        {
            double stored = 0, storedMax = 0, batteryNet = 0;
            double fuelEnergy = 0, reactorOut = 0;
            var items = new List<MyInventoryItem>();
            int count = 0;
            foreach (var block in context.Blocks(context.Line.Arg(0, "*"), b => b is IMyBatteryBlock || b is IMyReactor))
            {
                var battery = block as IMyBatteryBlock;
                if (battery != null)
                {
                    stored += battery.CurrentStoredPower;
                    storedMax += battery.MaxStoredPower;
                    batteryNet += battery.CurrentInput - battery.CurrentOutput;
                }
                else
                {
                    reactorOut += ((IMyReactor)block).CurrentOutput;
                    fuelEnergy += FuelEnergy(block, items);
                }
                if (++count % Chunk == 0)
                    yield return 0;
            }
            // The stored pool (battery charge and fuel) shrinks by the reactor output and grows by the battery net input.
            double seconds;
            bool charging = false;
            double loss = reactorOut - batteryNet;
            if (loss > 1e-9)
                seconds = (stored + fuelEnergy) / loss * 3600;
            else if (batteryNet > 1e-9 && reactorOut <= 1e-9)
            {
                charging = true;
                seconds = (storedMax - stored) / batteryNet * 3600;
            }
            else
                seconds = double.PositiveInfinity;

            double hours, minutes, secs;
            Formatting.TryNumber(context.Line.Arg(1, "0"), out hours);
            Formatting.TryNumber(context.Line.Arg(2, "0"), out minutes);
            Formatting.TryNumber(context.Line.Arg(3, "0"), out secs);
            double quota = hours * 3600 + minutes * 60 + secs;
            string title = charging ? context.T("SyxAutoLcd_ChargeTime", "Charged in:") : context.T("SyxAutoLcd_PowerTime", "Power time:");
            string value = Formatting.Duration(seconds);
            if (quota <= 0)
            {
                context.Canvas.Pair(title, value);
                yield break;
            }
            double fraction = double.IsInfinity(seconds) ? 1 : Math.Min(1, seconds / quota);
            if (charging)
                fraction = 1 - fraction;
            context.Canvas.Stat(title, value, fraction, context.Mode(context.Suffix));
        }

        // Energy (MWh) of the fuel in a reactor: per item FuelProductionToCapacityMultiplier / ratio megawatt seconds.
        private static double FuelEnergy(IMyTerminalBlock block, List<MyInventoryItem> items)
        {
            var definition = MyDefinitionManager.Static.GetCubeBlockDefinition(block.BlockDefinition) as MyReactorDefinition;
            var inventory = block.GetInventory(0);
            if (definition == null || inventory == null || definition.FuelInfos == null)
                return 0;
            items.Clear();
            inventory.GetItems(items);
            double energy = 0;
            foreach (var item in items)
            {
                foreach (var fuel in definition.FuelInfos)
                {
                    if (fuel.FuelId.TypeId.ToString() != item.Type.TypeId || fuel.FuelId.SubtypeName != item.Type.SubtypeId || fuel.Ratio <= 0)
                        continue;
                    energy += (double)item.Amount * definition.FuelProductionToCapacityMultiplier / fuel.Ratio / 3600;
                }
            }
            return energy;
        }
    }
}
