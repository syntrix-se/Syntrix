// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Syntrix.Gui;
using VRage.Game;
using VRage.Input;

namespace Syntrix.Wiki
{
    /// <summary>
    /// In-game wiki (API 3): reads Data/Syntrix-Wiki.xml of every loaded mod, adds the pages of generating mods
    /// (Wiki.AddSource) and fills automatic tables and lists (Wiki.AddTables). Opens with F11 or /syxwiki;
    /// players only, nothing runs on the dedicated server.
    /// </summary>
    public sealed class WikiService
    {
        public static WikiService Instance { get; private set; }

        private const string ExpandedFile = "SyxWiki-Expanded.txt";
        private const string SeenFile = "SyxWiki-Seen.txt";

        private readonly SyntrixCore _core;
        private readonly Dictionary<string, Func<string, string>> _sources = new Dictionary<string, Func<string, string>>();
        private readonly Dictionary<string, Func<string, string, string, string, string, string>> _tables =
            new Dictionary<string, Func<string, string, string, string, string, string>>();
        private readonly Dictionary<string, Action> _closed = new Dictionary<string, Action>();
        private WikiData _data;
        private string _context;
        private bool _dirty = true;
        private bool _loading;
        private WikiWindow _window;
        private DateTime _lastToggle;

        public WikiService(SyntrixCore core)
        {
            _core = core;
            Instance = this;
        }

        public bool Usable => _core.IsPlayer;

        /// <summary>Data last built (null before the first opening).</summary>
        public WikiData Data => _data;

        public static bool German => MyAPIGateway.Session?.Config?.Language == MyLanguagesEnum.German;

        public void Start()
        {
            if (!Usable)
                return;
            _core.Chat.Register("Syntrix", "syxwiki", 0, false, "open the wiki (/syxwiki <page>, /syxwiki reload)", OnChat);
            _core.Gui.OnKeys(Keys);
        }

        public void Stop()
        {
            if (_window != null)
                _window.CloseNow();
            _window = null;
            _data = null;
            _sources.Clear();
            _tables.Clear();
            _closed.Clear();
            if (Instance == this)
                Instance = null;
        }

        // ---------- endpoints ----------

        /// <summary>A mod adds generated pages: it gets the context XML (systems, page stubs) and returns pages as XML.</summary>
        public void AddSource(string mod, Func<string, string> source)
        {
            if (source == null)
                _sources.Remove(mod);
            else
                _sources[mod] = source;
            _dirty = true;
        }

        /// <summary>A mod fills automatic tables and lists: (kind "Table"|"List", auto, system, planet, page) → XML or null.</summary>
        public void AddTables(string mod, Func<string, string, string, string, string, string> tables)
        {
            if (tables == null)
                _tables.Remove(mod);
            else
                _tables[mod] = tables;
            _dirty = true;
        }

        /// <summary>Handler of a mod run when the wiki window closes.</summary>
        public void OnClosed(string mod, Action handler)
        {
            if (handler == null)
                _closed.Remove(mod);
            else
                _closed[mod] = handler;
        }

        /// <summary>Context XML: systems and page stubs of all wiki files (without generated pages).</summary>
        public string Context()
        {
            if (!Usable)
                return null;
            if (_context == null)
                _context = BuildContext(WikiLoader.Load());
            return _context;
        }

        public bool IsOpen => _window != null && _window.IsOpen;

        /// <summary>Opens the wiki (page id or empty for the last page); false if the window could not open.</summary>
        public bool Open(string page)
        {
            if (!Usable)
                return false;
            try
            {
                EnsureLoaded();
                if (_window == null)
                {
                    _window = new WikiWindow(this, German);
                    _window.SetExpanded(ReadLines(ExpandedFile));
                    _window.SetSeen(ReadLines(SeenFile));
                    _window.SetData(_data);
                }
                if (!string.IsNullOrWhiteSpace(page))
                    _window.ShowId(page.Trim());
                _lastToggle = DateTime.UtcNow;
                return _window.Open();
            }
            catch (Exception e)
            {
                Log.Error("wiki open", e);
                return false;
            }
        }

        public void Close()
        {
            if (_window != null)
                _window.Close();
        }

        /// <summary>Reads all files and sources again (also used by /syxwiki reload).</summary>
        public void Reload()
        {
            _dirty = true;
            _context = null;
            if (_window != null && Usable)
            {
                EnsureLoaded();
                _window.SetData(_data);
            }
        }

        // ---------- loading ----------

        private void EnsureLoaded()
        {
            if (!_dirty && _data != null || _loading)
                return;
            _loading = true;
            try
            {
                GuiDraw.ResetCache();
                var data = WikiLoader.Load();
                _context = BuildContext(data);
                foreach (var kv in _sources.ToArray())
                    MergeSource(data, kv.Key, kv.Value);
                data.Finish(FillAuto);
                _data = data;
                _dirty = false;
                Log.Info($"Wiki: {data.Pages.Count} pages from {data.Sources.Count} files, {data.Errors.Count} problems");
                foreach (var error in data.Errors)
                    Log.Info("Wiki: " + error);
                foreach (var notice in data.Notices)
                    Log.Info("Wiki: " + notice);
                if (_window != null)
                    _window.SetData(_data);
            }
            finally
            {
                _loading = false;
            }
        }

        private static string BuildContext(WikiData data)
        {
            var x = new WikiFileXml();
            x.Systems.AddRange(data.SystemSources);
            foreach (var page in data.Pages.Values)
                x.Pages.Add(new WikiPageXml
                {
                    Id = page.Id,
                    Parent = page.Parent,
                    Order = page.Order,
                    Source = page.Source,
                    Title = new LangTextXml { DeShort = page.TitleDe, EnShort = page.TitleEn },
                });
            return MyAPIGateway.Utilities.SerializeToXML(x);
        }

        private void MergeSource(WikiData data, string mod, Func<string, string> source)
        {
            var origin = mod + ": Wiki.AddSource";
            try
            {
                var xml = source(_context);
                if (string.IsNullOrWhiteSpace(xml))
                    return;
                data.Merge(MyAPIGateway.Utilities.SerializeFromXML<WikiFileXml>(xml), origin, mod);
            }
            catch (Exception e)
            {
                data.Errors.Add(origin + " failed: " + (e.InnerException != null ? e.Message + " " + e.InnerException.Message : e.Message));
            }
        }

        /// <summary>Fills automatic tables and lists from the mods that provide them, then resolves block icons.</summary>
        private void FillAuto(WikiData data)
        {
            data._filling = true;
            try
            {
                foreach (var page in data.Pages.Values.ToArray())
                    foreach (var block in page.Blocks)
                        if (!string.IsNullOrEmpty(block.Auto))
                            FillBlock(data, page, block);
            }
            finally
            {
                data._filling = false;
            }
            ResolveBlockIcons(data);
        }

        private void FillBlock(WikiData data, WikiPage page, WikiBlock block)
        {
            bool table = block.Kind == WikiBlockKind.Table;
            string xml = null, mod = null;
            foreach (var kv in _tables)
            {
                try
                {
                    xml = kv.Value(table ? "Table" : "List", block.Auto, block.AutoSystem ?? "", block.AutoPlanet ?? "", page.Id);
                }
                catch (Exception e)
                {
                    data.Errors.Add($"{kv.Key}: Auto '{block.Auto}' on page '{page.Id}' failed: {e.Message}");
                    xml = null;
                }
                if (xml != null)
                {
                    mod = kv.Key;
                    break;
                }
            }
            if (xml == null)
            {
                data.Errors.Add($"Page '{page.Id}' ({page.Source}): Auto '{block.Auto}' is provided by no loaded mod");
                Empty(block);
                return;
            }
            var origin = $"{mod}: Auto '{block.Auto}'";
            try
            {
                var x = MyAPIGateway.Utilities.SerializeFromXML<WikiFileXml>(xml);
                if (x != null && x.Errors != null)
                    foreach (var error in x.Errors)
                        if (!string.IsNullOrWhiteSpace(error))
                            data.Errors.Add(origin + ": " + error.Trim());
                var blocks = x?.Pages != null && x.Pages.Count > 0 ? x.Pages[0].Blocks : null;
                if (blocks != null)
                    foreach (var b in blocks)
                    {
                        if (table && b is TableXml)
                        {
                            var t = data.ReadTable(page, (TableXml)b, origin);
                            if (t != null)
                            {
                                if (block.HeaderDe == null && block.HeaderEn == null)
                                {
                                    block.HeaderDe = t.HeaderDe;
                                    block.HeaderEn = t.HeaderEn;
                                }
                                if (block.Widths == null)
                                    block.Widths = t.Widths;
                                block.RowsDe.AddRange(t.RowsDe);
                                block.RowsEn.AddRange(t.RowsEn);
                            }
                            break;
                        }
                        if (!table && b is ListXml)
                        {
                            var l = data.ReadList(page, (ListXml)b, origin);
                            if (l != null)
                                block.Entries.AddRange(l.Entries);
                            break;
                        }
                    }
            }
            catch (Exception e)
            {
                data.Errors.Add(origin + " not readable: " + (e.InnerException != null ? e.Message + " " + e.InnerException.Message : e.Message));
            }
            Empty(block);
        }

        /// <summary>Empty automatic table: one quiet row instead of nothing.</summary>
        private static void Empty(WikiBlock block)
        {
            if (block.Kind == WikiBlockKind.Table && block.RowsDe.Count == 0)
            {
                block.RowsDe.Add(new[] { "Keine Einträge." });
                block.RowsEn.Add(new[] { "No entries." });
            }
        }

        /// <summary>Block="Type/Subtype" on pages and list entries: the definition icon (or a SyxWikiIcon_ material).</summary>
        private static void ResolveBlockIcons(WikiData data)
        {
            foreach (var page in data.Pages.Values)
            {
                if (!string.IsNullOrEmpty(page.IconBlock) && page.Icons == null)
                    page.Icons = BlockIcon(data, page.Id, page.IconBlock);
                foreach (var b in page.Blocks)
                    if (b.Entries != null)
                        foreach (var e in b.Entries)
                            if (!string.IsNullOrEmpty(e.Block) && e.Icons == null)
                                e.Icons = BlockIcon(data, page.Id, e.Block);
            }
        }

        private static string[] BlockIcon(WikiData data, string pageId, string block)
        {
            try
            {
                var parts = block.Split('/');
                MyDefinitionId id;
                MyCubeBlockDefinition def;
                if (parts.Length != 2 || !MyDefinitionId.TryParse("MyObjectBuilder_" + parts[0].Trim(), parts[1].Trim(), out id)
                    || !MyDefinitionManager.Static.TryGetCubeBlockDefinition(id, out def) || def == null)
                {
                    data.Errors.Add($"Page '{pageId}': block '{block}' does not exist");
                    return null;
                }
                return new[] { id.TypeId.ToString() + "/" + id.SubtypeName };
            }
            catch (Exception e)
            {
                data.Errors.Add($"Page '{pageId}': icon for block '{block}' not read: {e.Message}");
                return null;
            }
        }

        // ---------- window events ----------

        internal void WindowClosed()
        {
            _lastToggle = DateTime.UtcNow;
            foreach (var kv in _closed.ToArray())
            {
                try
                {
                    kv.Value();
                }
                catch (Exception e)
                {
                    Log.Error("wiki closed handler of " + kv.Key, e);
                }
            }
        }

        internal void SaveExpanded(IEnumerable<string> ids)
        {
            WriteLines(ExpandedFile, ids);
        }

        internal void SaveSeen(IEnumerable<string> ids)
        {
            WriteLines(SeenFile, ids);
        }

        // ---------- keys and chat ----------

        /// <summary>F11 in the game (not while the chat or another screen is open).</summary>
        private void Keys()
        {
            if (IsOpen || !MyAPIGateway.Input.IsNewKeyPressed(MyKeys.F11))
                return;
            if (MyAPIGateway.Gui.ChatEntryVisible || MyAPIGateway.Gui.IsCursorVisible || TooSoon())
                return;
            Open(null);
        }

        /// <summary>F11 inside the open window closes it (not while typing into the search).</summary>
        internal void WindowKeys(bool typing)
        {
            if (!typing && MyAPIGateway.Input.IsNewKeyPressed(MyKeys.F11) && !TooSoon())
            {
                _lastToggle = DateTime.UtcNow;
                Close();
            }
        }

        private bool TooSoon() => (DateTime.UtcNow - _lastToggle).TotalMilliseconds < 250;

        private string OnChat(ulong sender, string[] args)
        {
            var argument = string.Join(" ", args).Trim();
            GuiSession.Later(() =>
            {
                if (argument.Equals("reload", StringComparison.OrdinalIgnoreCase))
                {
                    Reload();
                    EnsureLoaded();
                    MyAPIGateway.Utilities.ShowMessage("Syntrix", German
                        ? $"Wiki: {_data.Pages.Count} Seiten neu geladen, {_data.Errors.Count} Hinweise (Seite Wiki-Fehler)."
                        : $"Wiki: {_data.Pages.Count} pages reloaded, {_data.Errors.Count} problems (page Wiki errors).");
                    argument = "";
                }
                Open(argument);
            });
            return null;
        }

        // ---------- local files ----------

        private static string[] ReadLines(string file)
        {
            try
            {
                if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(file, typeof(WikiService)))
                    return new string[0];
                using (var reader = MyAPIGateway.Utilities.ReadFileInLocalStorage(file, typeof(WikiService)))
                    return reader.ReadToEnd().Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            }
            catch (Exception e)
            {
                Log.Warn("wiki: " + file + " not read: " + e.Message);
                return new string[0];
            }
        }

        private static void WriteLines(string file, IEnumerable<string> lines)
        {
            try
            {
                using (var writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(file, typeof(WikiService)))
                    writer.Write(string.Join("\n", lines));
            }
            catch (Exception e)
            {
                Log.Warn("wiki: " + file + " not saved: " + e.Message);
            }
        }
    }
}
