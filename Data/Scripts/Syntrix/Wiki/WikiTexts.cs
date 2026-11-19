// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game;

namespace Syntrix.Wiki
{
    /// <summary>
    /// Texts of resx files in both languages (the game only loads the current one, the wiki shows both):
    /// Data/Localization/&lt;file&gt;.resx (English) and &lt;file&gt;.de.resx (German) of every loaded mod, read once per file.
    /// </summary>
    public static class WikiTexts
    {
        private static readonly Dictionary<string, Dictionary<string, string>> Files =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Forgets all read files (for /syxwiki reload).</summary>
        public static void Reset()
        {
            Files.Clear();
        }

        /// <summary>Text of the key in the language, null if no loaded mod has it.</summary>
        /// <param name="file">Base name of the resx file, e.g. "MyTexts_GridCore".</param>
        /// <param name="key">Text key.</param>
        /// <param name="german">True for German (*.de.resx).</param>
        public static string Get(string file, string key, bool german)
        {
            if (string.IsNullOrWhiteSpace(file) || string.IsNullOrEmpty(key))
                return null;
            var path = "Data/Localization/" + file.Trim() + (german ? ".de.resx" : ".resx");
            Dictionary<string, string> texts;
            if (!Files.TryGetValue(path, out texts))
                Files[path] = texts = Read(path);
            string s;
            return texts.TryGetValue(key, out s) && !string.IsNullOrEmpty(s) ? s : null;
        }

        private static Dictionary<string, string> Read(string path)
        {
            var target = new Dictionary<string, string>(StringComparer.Ordinal);
            var mods = MyAPIGateway.Session?.Mods;
            if (mods == null)
                return target;
            foreach (var mod in mods)
                ReadResx(mod, path, target);
            return target;
        }

        /// <summary>Picks name and value of every &lt;data&gt; entry by hand (no XmlDocument in the mod API); first mod wins.</summary>
        private static void ReadResx(MyObjectBuilder_Checkpoint.ModItem mod, string path, Dictionary<string, string> target)
        {
            try
            {
                if (!MyAPIGateway.Utilities.FileExistsInModLocation(path, mod))
                    return;
                string t;
                using (var reader = MyAPIGateway.Utilities.ReadFileInModLocation(path, mod))
                    t = reader.ReadToEnd();
                int i = 0;
                while ((i = t.IndexOf("<data name=\"", i, StringComparison.Ordinal)) >= 0)
                {
                    i += 12;
                    int e = t.IndexOf('"', i);
                    if (e < 0)
                        break;
                    var name = t.Substring(i, e - i);
                    int end = t.IndexOf("</data>", e, StringComparison.Ordinal);
                    int v = t.IndexOf("<value>", e, StringComparison.Ordinal);
                    if (v < 0 || end < 0 || v > end)
                    {
                        i = e;
                        continue;
                    }
                    v += 7;
                    int valueEnd = t.IndexOf("</value>", v, StringComparison.Ordinal);
                    if (valueEnd < 0)
                        break;
                    if (!target.ContainsKey(name))
                        target[name] = t.Substring(v, valueEnd - v).Replace("&lt;", "<").Replace("&gt;", ">").Replace("&quot;", "\"").Replace("&apos;", "'").Replace("&amp;", "&");
                    i = valueEnd;
                }
            }
            catch (Exception e)
            {
                Log.Warn("wiki texts: " + path + " not read: " + e.Message);
            }
        }
    }
}
