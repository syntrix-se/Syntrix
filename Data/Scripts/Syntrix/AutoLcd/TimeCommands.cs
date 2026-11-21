// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Syntrix.AutoLcd
{
    /// <summary>
    /// Time, Date, DateTime (each with C for centred and an hour offset like +2 or -0.5), Countdown (C, R).
    /// DateTime takes a .NET format; text in '…' is kept as it is (no AltGr needed), \ escapes one character.
    /// The time is the clock of the server.
    /// </summary>
    internal static class TimeCommands
    {
        public static void Register()
        {
            CommandRegistry.Add("Time", CommandRegistry.Fast, c => Clock(c, "t"));
            CommandRegistry.Add("Date", CommandRegistry.Normal, c => Clock(c, "d"));
            CommandRegistry.Add("DateTime", CommandRegistry.Fast, c => Clock(c, null));
            CommandRegistry.Add("Countdown", CommandRegistry.Fast, Countdown);
        }

        // Splits "C+2" into centred and offset hours.
        private static void ReadSuffix(string suffix, out bool center, out double offset)
        {
            center = suffix.StartsWith("C", StringComparison.Ordinal);
            if (center)
                suffix = suffix.Substring(1);
            offset = 0;
            if (suffix.Length > 0 && !double.TryParse(suffix, NumberStyles.Float, CultureInfo.InvariantCulture, out offset))
                offset = 0;
        }

        private static IEnumerable<int> Clock(CommandContext context, string standardFormat)
        {
            bool center;
            double offset;
            ReadSuffix(context.Suffix, out center, out offset);
            DateTime now = DateTime.Now.AddHours(offset);
            string text;
            if (standardFormat == null)
            {
                string format = context.Line.Rest.Trim();
                try
                {
                    text = format.Length == 0 ? now.ToString("g", LcdText.Culture) : now.ToString(format, LcdText.Culture);
                }
                catch (FormatException)
                {
                    text = format;
                }
            }
            else
            {
                string title = context.Line.Rest;
                string value = now.ToString(standardFormat, LcdText.Culture);
                text = title.Length > 0 ? title.TrimEnd() + " " + value : value;
            }
            if (center)
                context.Canvas.Center(text);
            else
                context.Canvas.Add(text);
            yield break;
        }

        private static IEnumerable<int> Countdown(CommandContext context)
        {
            DateTime target;
            string text;
            if (!DateTime.TryParseExact(context.Line.Rest.Trim(), "H:mm d.M.yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out target))
                text = context.T("SyxAutoLcd_ErrorCountdown", "Countdown needs a time like 22:00 31.12.2026");
            else
            {
                var left = target - DateTime.Now;
                if (left.TotalSeconds <= 0)
                    text = context.T("SyxAutoLcd_Expired", "EXPIRED");
                else
                    text = string.Format(LcdText.Culture, "{0}d {1:00}:{2:00}:{3:00}", (int)left.TotalDays, left.Hours, left.Minutes, left.Seconds);
            }
            if (context.SuffixIs("C"))
                context.Canvas.Center(text);
            else if (context.SuffixIs("R"))
                context.Canvas.Right(text);
            else
                context.Canvas.Add(text);
            yield break;
        }
    }
}
