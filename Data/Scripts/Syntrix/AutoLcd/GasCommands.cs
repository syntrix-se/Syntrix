// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Ingame = Sandbox.ModAPI.Ingame;
using GameIngame = SpaceEngineers.Game.ModAPI.Ingame;

namespace Syntrix.AutoLcd
{
    /// <summary>Oxygen (Level, X, LevelX) and Tanks (V, P, X, Bar), also for modded gases.</summary>
    internal static class GasCommands
    {
        private const int Chunk = 32;

        public static void Register()
        {
            CommandRegistry.Add("Oxygen", CommandRegistry.Normal, Oxygen);
            CommandRegistry.Add("Tanks", CommandRegistry.Normal, Tanks);
        }

        /// <summary>Subtype of the gas a tank stores ("Oxygen", "Hydrogen", mod gases), empty if unknown.</summary>
        public static string GasOf(IMyTerminalBlock block)
        {
            var definition = MyDefinitionManager.Static.GetCubeBlockDefinition(block.BlockDefinition) as MyGasTankDefinition;
            return definition == null ? "" : definition.StoredGasId.SubtypeName;
        }

        private static IEnumerable<int> Oxygen(CommandContext context)
        {
            string suffix = context.Suffix;
            bool level = suffix.StartsWith("LEVEL", StringComparison.Ordinal);
            if (level)
                suffix = suffix.Substring(5);
            bool percentOnly = suffix == "X";
            string ownTitle = context.Line.Arg(1, null);
            double farmSum = 0, tankFill = 0, tankCapacity = 0;
            int farms = 0, tanks = 0, n = 0;
            foreach (var block in context.Blocks(context.Line.Arg(0, "*")))
            {
                if (++n % Chunk == 0)
                    yield return 0;
                var vent = block as GameIngame.IMyAirVent;
                if (vent != null)
                {
                    string title = ownTitle ?? block.CustomName;
                    double fraction = vent.GetOxygenLevel();
                    if (!level && !vent.CanPressurize)
                        context.Canvas.Pair(title, BlockCommands.StateText("leak"));
                    else if (percentOnly)
                        context.Canvas.Pair(title, Formatting.Percent(fraction));
                    else
                    {
                        context.Canvas.Pair(title, Formatting.Percent(fraction));
                        context.Canvas.Bar(fraction);
                    }
                    continue;
                }
                var farm = block as GameIngame.IMyOxygenFarm;
                if (farm != null)
                {
                    farmSum += farm.GetOutput();
                    farms++;
                    continue;
                }
                var tank = block as Ingame.IMyGasTank;
                if (tank != null && GasOf(block) == "Oxygen")
                {
                    tankFill += tank.FilledRatio * tank.Capacity;
                    tankCapacity += tank.Capacity;
                    tanks++;
                }
            }
            if (farms > 0)
            {
                double average = farmSum / farms;
                string title = ownTitle ?? context.T("SyxAutoLcd_OxygenFarms", "Oxygen farms") + " (" + farms + ")";
                context.Canvas.Pair(title, Formatting.Percent(average));
                if (!percentOnly)
                    context.Canvas.Bar(average);
            }
            if (tanks > 0)
            {
                double average = tankCapacity > 0 ? tankFill / tankCapacity : 0;
                string title = ownTitle ?? context.T("SyxAutoLcd_OxygenTanks", "Oxygen tanks") + " (" + tanks + ")";
                context.Canvas.Pair(title, Formatting.Percent(average));
                if (!percentOnly)
                    context.Canvas.Bar(average);
            }
        }

        private static IEnumerable<int> Tanks(CommandContext context)
        {
            string gas = context.Line.Arg(1, "");
            if (context.Line.Args.Count < 2 || gas.Length == 0)
            {
                context.Screen.AddError(context.Line, context.T("SyxAutoLcd_ErrorTankType", "Tanks needs a name filter and a gas type."));
                yield break;
            }
            gas = char.ToUpperInvariant(gas[0]) + gas.Substring(1).ToLowerInvariant();
            double fill = 0, capacity = 0;
            int count = 0;
            foreach (var block in context.Blocks(context.Line.Arg(0, "*"), b => b is Ingame.IMyGasTank))
            {
                if (!string.Equals(GasOf(block), gas, StringComparison.OrdinalIgnoreCase))
                    continue;
                var tank = (Ingame.IMyGasTank)block;
                fill += tank.FilledRatio * tank.Capacity;
                capacity += tank.Capacity;
                if (++count % Chunk == 0)
                    yield return 0;
            }
            string title = context.Line.Arg(2, context.T("SyxAutoLcd_Tanks" + gas, gas + " " + context.T("SyxAutoLcd_TanksSuffix", "tanks")));
            double fraction = capacity > 0 ? fill / capacity : 0;
            // Tank capacity is in litres.
            context.Canvas.Stat(title, Formatting.Volume(fill / 1000) + " / " + Formatting.Volume(capacity / 1000), fraction, context.Mode(context.Suffix));
        }
    }
}
