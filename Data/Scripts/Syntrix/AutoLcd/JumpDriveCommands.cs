// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using IMyJumpDrive = Sandbox.ModAPI.Ingame.IMyJumpDrive;

namespace Syntrix.AutoLcd
{
    /// <summary>
    /// Charge, ChargeSum, ChargeTime, ChargeSumTime with the variants X and Bar. The charge rate is measured from
    /// the stored energy between runs (smoothed); before the first measurement the electric input is used.
    /// </summary>
    internal static class JumpDriveCommands
    {
        private static readonly Dictionary<long, Sample> _samples = new Dictionary<long, Sample>();

        public static void Register()
        {
            CommandRegistry.Add("Charge", CommandRegistry.Fast, c => Run(c, false, false));
            CommandRegistry.Add("ChargeSum", CommandRegistry.Fast, c => Run(c, true, false));
            CommandRegistry.Add("ChargeTime", CommandRegistry.Fast, c => Run(c, false, true));
            CommandRegistry.Add("ChargeSumTime", CommandRegistry.Fast, c => Run(c, true, true));
        }

        public static void Clear()
        {
            _samples.Clear();
        }

        private static IEnumerable<int> Run(CommandContext context, bool sum, bool time)
        {
            if (context.Line.Args.Count == 0)
                yield break;
            var drives = context.Blocks(context.Line.Arg(0), b => b is IMyJumpDrive);
            string title = context.Line.Arg(1, context.T("SyxAutoLcd_JumpDrives", "Jump drives"));
            bool percentOnly = context.SuffixIs("X");
            bool barOnly = context.SuffixIs("Bar");
            double stored = 0, max = 0, longest = 0;
            foreach (var block in drives)
            {
                var drive = (IMyJumpDrive)block;
                double seconds = Remaining(block, drive, context.Now);
                if (sum)
                {
                    stored += drive.CurrentStoredPower;
                    max += drive.MaxStoredPower;
                    longest = Math.Max(longest, seconds);
                    continue;
                }
                double fraction = drive.MaxStoredPower > 0 ? drive.CurrentStoredPower / drive.MaxStoredPower : 0;
                if (time)
                    ShowTime(context, block.CustomName, seconds, fraction, percentOnly);
                else
                    ShowCharge(context, block.CustomName, drive.CurrentStoredPower, drive.MaxStoredPower, percentOnly, barOnly);
            }
            if (sum && drives.Count > 0)
            {
                if (time)
                    ShowTime(context, title, longest, max > 0 ? stored / max : 0, percentOnly);
                else
                    ShowCharge(context, title, stored, max, percentOnly, barOnly);
            }
        }

        private static void ShowCharge(CommandContext context, string name, double stored, double max, bool percentOnly, bool barOnly)
        {
            var mode = barOnly ? StatMode.Bar : percentOnly ? StatMode.Percent : StatMode.Full;
            context.Canvas.Stat(name, Formatting.Energy(stored) + " / " + Formatting.Energy(max), max > 0 ? stored / max : 0, mode);
        }

        private static void ShowTime(CommandContext context, string name, double seconds, double fraction, bool timeOnly)
        {
            string value = fraction >= 0.9999 ? context.T("SyxAutoLcd_Charged", "charged") : Formatting.Duration(seconds);
            if (timeOnly)
                context.Canvas.Pair(name, value);
            else
                context.Canvas.PairBar(name, fraction, value);
        }

        // Seconds until full; infinity if the drive does not charge.
        private static double Remaining(IMyTerminalBlock block, IMyJumpDrive drive, double now)
        {
            double stored = drive.CurrentStoredPower;
            double missing = drive.MaxStoredPower - stored;
            if (missing <= 1e-9)
                return 0;
            Sample sample;
            if (!_samples.TryGetValue(block.EntityId, out sample))
            {
                sample = new Sample { Time = now, Stored = stored, Rate = double.NaN };
                _samples[block.EntityId] = sample;
            }
            else if (now - sample.Time >= 0.9)
            {
                double rate = (stored - sample.Stored) / (now - sample.Time);
                sample.Rate = double.IsNaN(sample.Rate) ? rate : sample.Rate * 0.6 + rate * 0.4;
                sample.Time = now;
                sample.Stored = stored;
            }
            double perSecond = sample.Rate;
            if (double.IsNaN(perSecond))
            {
                double current, most;
                perSecond = PowerCommands.Input(block, out current, out most) ? current / 3600 : 0;
            }
            return perSecond > 1e-12 ? missing / perSecond : double.PositiveInfinity;
        }

        private class Sample
        {
            public double Time;

            public double Stored;

            public double Rate;
        }
    }
}
