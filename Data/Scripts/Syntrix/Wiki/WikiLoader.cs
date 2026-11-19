// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game;

namespace Syntrix.Wiki
{
    /// <summary>
    /// Reads Data/Syntrix-Wiki.xml from every loaded mod of the world, plus the files listed there under &lt;File&gt;.
    /// The mods need no code of their own for this.
    /// </summary>
    public static class WikiLoader
    {
        public const string MainFile = "Data/Syntrix-Wiki.xml";
        private const int MaxFilesPerMod = 100;

        /// <summary>Reads the wiki files of all loaded mods (no generated pages, no Finish).</summary>
        public static WikiData Load()
        {
            var data = new WikiData();
            var mods = MyAPIGateway.Session?.Mods;
            if (mods != null)
                foreach (var mod in mods)
                    LoadMod(data, mod);
            return data;
        }

        private static void LoadMod(WikiData data, MyObjectBuilder_Checkpoint.ModItem mod)
        {
            var name = string.IsNullOrEmpty(mod.FriendlyName) ? mod.Name : mod.FriendlyName;
            string mainFile;
            try
            {
                if (MyAPIGateway.Utilities.FileExistsInModLocation(MainFile, mod))
                    mainFile = MainFile;
                else
                    return;
            }
            catch (Exception)
            {
                return;
            }

            var queue = new Queue<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            queue.Enqueue(mainFile);
            while (queue.Count > 0 && seen.Count < MaxFilesPerMod)
            {
                var file = queue.Dequeue();
                if (!seen.Add(file))
                    continue;
                var origin = $"{name}: {file}";
                try
                {
                    if (!MyAPIGateway.Utilities.FileExistsInModLocation(file, mod))
                    {
                        data.Errors.Add($"{origin} missing");
                        continue;
                    }
                    string xml;
                    using (var reader = MyAPIGateway.Utilities.ReadFileInModLocation(file, mod))
                        xml = reader.ReadToEnd();
                    var x = MyAPIGateway.Utilities.SerializeFromXML<WikiFileXml>(xml);
                    data.Merge(x, origin, name);
                    if (x?.Files != null)
                        foreach (var next in x.Files)
                            if (!string.IsNullOrWhiteSpace(next))
                                queue.Enqueue(next.Trim().Replace('\\', '/'));
                }
                catch (Exception e)
                {
                    // XmlSerializer usually wraps the real error (with line/column) in InnerException
                    var message = e.InnerException != null ? e.Message + " " + e.InnerException.Message : e.Message;
                    data.Errors.Add($"{origin} not readable: {message}");
                }
            }
        }
    }
}
