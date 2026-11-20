// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;

namespace Syntrix.AutoLcd
{
    /// <summary>Number, unit and time formats of the screen texts (in the culture of the screen language).</summary>
    internal static class Formatting
    {
        private const string Prefixes = " kMGTPEZY";

        /// <summary>Item amount: whole numbers below 1000, else three digits with k, M, G ...</summary>
        public static string Amount(double value)
        {
            double abs = Math.Abs(value);
            if (abs < 1000)
            {
                if (Math.Abs(value - Math.Round(value)) < 0.005)
                    return Math.Round(value).ToString("0", LcdText.Culture);
                return value.ToString(abs < 10 ? "0.##" : "0.#", LcdText.Culture);
            }
            return Scaled(value, "");
        }

        /// <summary>Value with unit prefix, e.g. 12.3 kW (value in the base unit).</summary>
        public static string Si(double value, string unit)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                return "-";
            double abs = Math.Abs(value);
            if (abs < 1000)
                return value.ToString(abs < 10 ? "0.##" : abs < 100 ? "0.#" : "0", LcdText.Culture) + " " + unit;
            return Scaled(value, " ") + unit;
        }

        /// <summary>Power from megawatt.</summary>
        public static string Power(double megawatt)
        {
            return Si(megawatt * 1e6, "W");
        }

        /// <summary>Energy from megawatt hours.</summary>
        public static string Energy(double megawattHours)
        {
            return Si(megawattHours * 1e6, "Wh");
        }

        /// <summary>Volume from cubic metres, shown in litres.</summary>
        public static string Volume(double cubicMetres)
        {
            return Si(cubicMetres * 1000, "L");
        }

        /// <summary>Mass from kilograms, shown in grams with prefix (kg, Mg = t, Gg ...).</summary>
        public static string Mass(double kilograms)
        {
            return Si(kilograms * 1000, "g");
        }

        /// <summary>Distance from metres.</summary>
        public static string Distance(double metres)
        {
            return Si(metres, "m");
        }

        /// <summary>Percent with one decimal.</summary>
        public static string Percent(double fraction)
        {
            if (double.IsNaN(fraction) || double.IsInfinity(fraction))
                return "-";
            return (fraction * 100).ToString("0.0", LcdText.Culture) + "%";
        }

        /// <summary>Plain number with up to two decimals.</summary>
        public static string Number(double value)
        {
            return value.ToString("0.##", LcdText.Culture);
        }

        /// <summary>Duration like 2d 3h 4m, 3h 4m 5s or 4m 5s; above about a year "> 1y".</summary>
        public static string Duration(double seconds)
        {
            if (double.IsNaN(seconds) || seconds < 0)
                return "-";
            if (double.IsInfinity(seconds) || seconds > 3.15e7)
                return "> 1y";
            var span = TimeSpan.FromSeconds(seconds);
            if (span.Days > 0)
                return span.Days + "d " + span.Hours + "h " + span.Minutes + "m";
            if (span.Hours > 0)
                return span.Hours + "h " + span.Minutes + "m " + span.Seconds + "s";
            if (span.Minutes > 0)
                return span.Minutes + "m " + span.Seconds + "s";
            return span.Seconds + "s";
        }

        /// <summary>Value with SI prefix multiplier from a prefix letter (k, M, G ...); 1 for none.</summary>
        public static double PrefixFactor(string prefix)
        {
            if (string.IsNullOrEmpty(prefix))
                return 1;
            int index = Prefixes.IndexOf(prefix[0]);
            return index <= 0 ? 1 : Math.Pow(1000, index);
        }

        /// <summary>Parses a number in invariant or screen culture.</summary>
        public static bool TryNumber(string text, out double value)
        {
            if (double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value))
                return true;
            return double.TryParse(text, System.Globalization.NumberStyles.Float, LcdText.Culture, out value);
        }

        private static string Scaled(double value, string gap)
        {
            double abs = Math.Abs(value);
            int index = 0;
            while (abs >= 1000 && index < Prefixes.Length - 1)
            {
                abs /= 1000;
                value /= 1000;
                index++;
            }
            string number = value.ToString(abs < 10 ? "0.00" : abs < 100 ? "0.0" : "0", LcdText.Culture);
            return number + gap + Prefixes[index];
        }
    }
}
