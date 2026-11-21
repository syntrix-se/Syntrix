// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using Sandbox.ModAPI;
using VRage;
using VRage.Utils;

namespace Syntrix.AutoLcd
{
    /// <summary>
    /// Texts written onto the screens. The server builds every screen text for all players (like the programmable
    /// block), so they come in one language per server: setting Language empty = the language the server loaded
    /// (MyTexts), otherwise AutoLCD reads the resx files of that language itself (game texts for item and block
    /// names, and the files named in LanguageFiles from every loaded mod). Own texts from the settings win.
    /// </summary>
    internal static class LcdText
    {
        private static readonly Regex _resxEntry = new Regex(
            "<data\\s+name=\"([^\"]+)\"[^>]*>\\s*<value>(.*?)</value>", RegexOptions.Singleline);

        private static Dictionary<string, string> _table;
        private static readonly Dictionary<string, string> _own = new Dictionary<string, string>();
        private static CultureInfo _culture = CultureInfo.InvariantCulture;

        /// <summary>Culture of the screen language (dates, times, number formats of DateTime).</summary>
        public static CultureInfo Culture
        {
            get { return _culture; }
        }

        /// <summary>Reads the texts for the configured language (server).</summary>
        public static void Load(AutoLcdSettings settings)
        {
            _own.Clear();
            foreach (var text in settings.Texts)
                if (!string.IsNullOrEmpty(text.Key))
                    _own[text.Key] = text.Value ?? "";
            _table = null;
            string language = (settings.Language ?? "").Trim();
            string gameCulture = GameCulture();
            _culture = MakeCulture(language.Length > 0 ? language : gameCulture);
            if (language.Length == 0 || SameLanguage(language, gameCulture))
                return;
            var table = new Dictionary<string, string>();
            string code = language.ToLowerInvariant();
            bool english = code == "en" || code.StartsWith("en-");
            string suffix = english ? "" : "." + language;
            ReadGameFile("Data\\Localization\\MyTexts" + suffix + ".resx", table);
            if (!english && !table.ContainsKey("DisplayName_Item_SteelPlate") && language.Contains("-"))
                ReadGameFile("Data\\Localization\\MyTexts." + language.Split('-')[0] + ".resx", table);
            var mods = MyAPIGateway.Session.Mods;
            if (mods != null)
            {
                foreach (var mod in mods)
                {
                    ReadModFile(mod, "Data\\Localization\\MyTexts" + suffix + ".resx", table);
                    foreach (var file in settings.LanguageFiles)
                        if (!string.IsNullOrWhiteSpace(file))
                            ReadModFile(mod, "Data\\Localization\\" + file.Trim() + suffix + ".resx", table);
                }
            }
            _table = table;
            AutoLcdCore.Log.Info("screen language " + language + ": " + table.Count + " texts read");
        }

        /// <summary>Screen text of a key: own text, then language table, then the server's texts, then fallback.</summary>
        public static string Get(string key, string fallback)
        {
            string text;
            if (_own.TryGetValue(key, out text))
                return text;
            if (_table != null && _table.TryGetValue(key, out text))
                return text;
            var id = MyStringId.GetOrCompute(key);
            if (MyTexts.Exists(id))
                return MyTexts.GetString(id);
            return fallback ?? key;
        }

        /// <summary>Screen text with {0}, {1} ... filled in.</summary>
        public static string Format(string key, string fallback, params object[] args)
        {
            string text = Get(key, fallback);
            try
            {
                return string.Format(text, args);
            }
            catch (FormatException)
            {
                return text;
            }
        }

        /// <summary>Display name from a definition text key (DisplayName_...) in the screen language.</summary>
        public static string Name(string key, string current)
        {
            if (string.IsNullOrEmpty(key))
                return current;
            string text;
            if (_own.TryGetValue(key, out text))
                return text;
            if (_table != null && _table.TryGetValue(key, out text))
                return text;
            return current;
        }

        private static string GameCulture()
        {
            try
            {
                var language = MyAPIGateway.Session.Config.Language;
                MyTexts.MyLanguageDescription description;
                if (MyTexts.Languages.TryGetValue(language, out description))
                    return description.FullCultureName;
            }
            catch (Exception)
            {
                // Dedicated servers may have no client config; English then.
            }
            return "en-US";
        }

        private static bool SameLanguage(string a, string b)
        {
            string x = a.Split('-')[0].ToLowerInvariant();
            string y = (b ?? "").Split('-')[0].ToLowerInvariant();
            return x == y;
        }

        private static CultureInfo MakeCulture(string name)
        {
            try
            {
                if (name == "de")
                    name = "de-DE";
                else if (name == "en")
                    name = "en-US";
                return new CultureInfo(name);
            }
            catch (Exception)
            {
                return CultureInfo.InvariantCulture;
            }
        }

        private static void ReadGameFile(string file, Dictionary<string, string> table)
        {
            try
            {
                if (!MyAPIGateway.Utilities.FileExistsInGameContent(file))
                    return;
                using (TextReader reader = MyAPIGateway.Utilities.ReadFileInGameContent(file))
                    Parse(reader.ReadToEnd(), table, "DisplayName_");
            }
            catch (Exception e)
            {
                AutoLcdCore.Log.Warn("cannot read " + file + ": " + e.Message);
            }
        }

        private static void ReadModFile(VRage.Game.MyObjectBuilder_Checkpoint.ModItem mod, string file, Dictionary<string, string> table)
        {
            try
            {
                if (!MyAPIGateway.Utilities.FileExistsInModLocation(file, mod))
                    return;
                using (TextReader reader = MyAPIGateway.Utilities.ReadFileInModLocation(file, mod))
                    Parse(reader.ReadToEnd(), table, null);
            }
            catch (Exception e)
            {
                AutoLcdCore.Log.Warn("cannot read " + file + " of " + mod.Name + ": " + e.Message);
            }
        }

        private static void Parse(string text, Dictionary<string, string> table, string prefix)
        {
            foreach (Match match in _resxEntry.Matches(text))
            {
                string key = match.Groups[1].Value;
                if (prefix != null && !key.StartsWith(prefix, StringComparison.Ordinal))
                    continue;
                table[key] = Unescape(match.Groups[2].Value);
            }
        }

        private static string Unescape(string value)
        {
            if (value.IndexOf('&') < 0)
                return value;
            return value.Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"")
                .Replace("&apos;", "'").Replace("&#xD;", "\r").Replace("&#xA;", "\n").Replace("&amp;", "&");
        }
    }
}
