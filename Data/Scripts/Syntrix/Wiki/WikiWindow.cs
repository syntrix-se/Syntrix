// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Syntrix.Gui;
using VRageMath;

namespace Syntrix.Wiki
{
    /// <summary>
    /// Wiki window in the Vanilla style: on the left search and page tree, on the right back/forward, path,
    /// language and the page. Built from the elements of the window service.
    /// </summary>
    public class WikiWindow
    {
        private const string NoTarget = "\u0001";
        private const int MaxHistory = 50;

        private readonly WikiService _service;
        private readonly GuiWindow _window;
        private readonly GuiTextField _search;
        private readonly GuiTree _tree;
        private readonly GuiButton _back, _forward, _language;
        private readonly GuiLabel _path;
        private readonly GuiScroll _page;

        private readonly HashSet<string> _expanded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _history = new List<string>();
        private readonly List<string> _future = new List<string>();
        private WikiData _data;
        private WikiPage _current;
        private string _searchText = "";
        private bool _german;
        private bool _filling;

        /// <summary>Builds the window (search, page tree, navigation, page area) in the language.</summary>
        public WikiWindow(WikiService service, bool german)
        {
            _service = service;
            _german = german;
            _window = new GuiWindow { Title = "Wiki", Pause = false };
            _window.Handler = OnWindowEvent;
            _window.KeyHook = () => _service.WindowKeys(_window.Focus is GuiTextField);

            var body = new GuiStack { Horizontal = true, FillWidth = true, FillHeight = true, Spacing = 14f };
            _window.Add(body);

            var left = new GuiStack { Size = new Vector2(330f, 0f), FillHeight = true, Spacing = 8f };
            body.Add(left);
            _search = new GuiTextField { FillWidth = true, MaxLength = 60 };
            _search.Handler = OnSearchEvent;
            left.Add(_search);
            _tree = new GuiTree { FillWidth = true, FillHeight = true };
            _tree.SetVector(GuiProp.IconSize, new Vector2(24f, 24f));
            _tree.Handler = OnTreeEvent;
            left.Add(_tree);

            var right = new GuiStack { FillWidth = true, FillHeight = true, Spacing = 8f };
            body.Add(right);
            var bar = new GuiStack { Horizontal = true, FillWidth = true, Spacing = 8f };
            right.Add(bar);
            _back = new GuiButton { Size = new Vector2(48f, 0f) };
            _back.SetText(GuiProp.Text, "<");
            _back.Handler = (e, ev, i, t) => { if (ev == GuiEvent.Click && i == 0) Back(); };
            bar.Add(_back);
            _forward = new GuiButton { Size = new Vector2(48f, 0f) };
            _forward.SetText(GuiProp.Text, ">");
            _forward.Handler = (e, ev, i, t) => { if (ev == GuiEvent.Click && i == 0) Forward(); };
            bar.Add(_forward);
            _path = new GuiLabel { FillWidth = true, Wrap = false, TextColor = GuiStyle.TextQuiet, Anchor = 3 };
            bar.Add(_path);
            _language = new GuiButton { Size = new Vector2(130f, 0f) };
            _language.Handler = (e, ev, i, t) => { if (ev == GuiEvent.Click && i == 0) SwitchLanguage(); };
            bar.Add(_language);

            _page = new GuiScroll { FillWidth = true, FillHeight = true, Spacing = 10f, Padding = new Vector2(28f, 20f) };
            right.Add(_page);
            LanguageTexts();
        }

        /// <summary>The window is open.</summary>
        public bool IsOpen => _window.IsOpen;

        /// <summary>Opens the window; false if the game refused.</summary>
        public bool Open() => _window.Open();

        /// <summary>Closes the window.</summary>
        public void Close() => _window.Close();

        /// <summary>Closes the window immediately.</summary>
        public void CloseNow() => _window.CloseNow();

        /// <summary>Sets the ids of the expanded tree pages (restored from the saved file).</summary>
        public void SetExpanded(IEnumerable<string> ids)
        {
            _expanded.Clear();
            foreach (var id in ids)
                if (!string.IsNullOrWhiteSpace(id))
                    _expanded.Add(id.Trim());
        }

        /// <summary>Sets the keys of the pages marked as new that the player has already seen.</summary>
        public void SetSeen(IEnumerable<string> ids)
        {
            _seen.Clear();
            foreach (var id in ids)
                if (!string.IsNullOrWhiteSpace(id))
                    _seen.Add(id.Trim());
        }

        /// <summary>New data: rebuild the tree, keep the current page if it still exists.</summary>
        public void SetData(WikiData data)
        {
            _data = data;
            WikiPage page = null;
            if (_current != null)
                _data.Pages.TryGetValue(_current.Id, out page);
            if (page == null)
                _data.Pages.TryGetValue(WikiData.StartId, out page);
            _current = null;
            BuildTree();
            Show(page, false);
        }

        /// <summary>Shows a page by id (unknown ids are ignored).</summary>
        public void ShowId(string id)
        {
            WikiPage page;
            if (_data != null && id != null && _data.Pages.TryGetValue(id, out page))
                Show(page, true);
        }

        // ---------- events ----------

        private void OnWindowEvent(GuiElement e, int ev, int index, string text)
        {
            if (ev == GuiEvent.Closed)
                _service.WindowClosed();
        }

        private void OnSearchEvent(GuiElement e, int ev, int index, string text)
        {
            if (ev != GuiEvent.Changed && ev != GuiEvent.Submit)
                return;
            var search = (text ?? "").Trim();
            if (search == _searchText && ev == GuiEvent.Changed)
                return;
            _searchText = search;
            BuildTree();
            if (ev == GuiEvent.Submit && _searchText.Length > 0)
            {
                // Enter: first hit
                foreach (var item in _tree.Items)
                {
                    WikiPage page;
                    if (_data.Pages.TryGetValue(item.Key, out page) && WikiData.Contains(page, _searchText))
                    {
                        Show(page, true);
                        break;
                    }
                }
            }
        }

        private void OnTreeEvent(GuiElement e, int ev, int index, string text)
        {
            if (_filling)
                return;
            if (ev == GuiEvent.Select)
                ShowId(text);
            else if (ev == GuiEvent.Expand && _searchText.Length == 0 && index >= 0 && index < _tree.Items.Count)
            {
                var key = _tree.Items[index].Key;
                if (text == "1")
                    _expanded.Add(key);
                else
                    _expanded.Remove(key);
                _service.SaveExpanded(_expanded);
            }
        }

        private void OnLink(GuiElement e, int ev, int index, string text)
        {
            if (ev == GuiEvent.Link)
                ShowId(text);
            else if (ev == GuiEvent.Click && index == 0 && text != null && !text.StartsWith(NoTarget))
                ShowId(text);
        }

        // ---------- navigation ----------

        private void Show(WikiPage page, bool remember)
        {
            if (page == null)
                return;
            if (remember && _current != null && _current != page)
            {
                _history.Add(_current.Id);
                if (_history.Count > MaxHistory)
                    _history.RemoveAt(0);
                _future.Clear();
            }
            _current = page;
            if (page.Level == WikiLevel.New && _seen.Add(page.NewKey))
            {
                _service.SaveSeen(_seen);
                int i = _tree.ItemFind(page.Id);
                if (i >= 0)
                    _tree.ItemSet(i, GuiItemProp.Badge, "");
            }
            SelectInTree();
            BuildPage();
            UpdateBar();
        }

        private void Back()
        {
            if (_history.Count == 0 || _data == null)
                return;
            var id = _history[_history.Count - 1];
            _history.RemoveAt(_history.Count - 1);
            if (_current != null)
                _future.Add(_current.Id);
            WikiPage page;
            if (_data.Pages.TryGetValue(id, out page))
                Show(page, false);
        }

        private void Forward()
        {
            if (_future.Count == 0 || _data == null)
                return;
            var id = _future[_future.Count - 1];
            _future.RemoveAt(_future.Count - 1);
            if (_current != null)
                _history.Add(_current.Id);
            WikiPage page;
            if (_data.Pages.TryGetValue(id, out page))
                Show(page, false);
        }

        private void SwitchLanguage()
        {
            _german = !_german;
            LanguageTexts();
            BuildTree();
            BuildPage();
            UpdateBar();
        }

        private void LanguageTexts()
        {
            _language.SetText(GuiProp.Text, _german ? "English" : "Deutsch");
            _search.Placeholder = _german ? "Suchen …" : "Search …";
            _back.Tooltip = _german ? "Zurück" : "Back";
            _forward.Tooltip = _german ? "Vor" : "Forward";
        }

        private void UpdateBar()
        {
            _back.Enabled = _history.Count > 0;
            _forward.Enabled = _future.Count > 0;
            var parts = new List<string>();
            var p = _current;
            int guard = 0;
            while (p != null && guard++ < 50)
            {
                parts.Insert(0, WikiMarkup.Escape(p.Title(_german)));
                if (string.IsNullOrEmpty(p.Parent) || !_data.Pages.TryGetValue(p.Parent, out p))
                    break;
            }
            _path.SetText(GuiProp.Text, string.Join("  ›  ", parts));
        }

        // ---------- tree ----------

        private void BuildTree()
        {
            _filling = true;
            try
            {
                _tree.ItemClear();
                if (_data == null)
                    return;
                HashSet<string> hits = null;
                if (_searchText.Length > 0)
                {
                    hits = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var page in _data.Pages.Values)
                    {
                        if (!WikiData.Contains(page, _searchText))
                            continue;
                        var p = page;
                        int guard = 0;
                        hits.Add(p.Id);
                        while (!string.IsNullOrEmpty(p.Parent) && _data.Pages.TryGetValue(p.Parent, out p) && guard++ < 50)
                            hits.Add(p.Id);
                    }
                }
                AddItems("", hits, 0);
                SelectInTree();
            }
            finally
            {
                _filling = false;
            }
        }

        private void AddItems(string parent, HashSet<string> hits, int depth)
        {
            if (depth > 12)
                return;
            foreach (var page in _data.Children(parent))
            {
                if (hits != null && !hits.Contains(page.Id))
                    continue;
                int i = _tree.ItemAdd(page.Id, string.IsNullOrEmpty(parent) ? null : parent, page.Title(_german),
                    string.IsNullOrEmpty(page.Symbol) ? null : "SyxWiki_" + page.Symbol);
                if (hits != null || _expanded.Contains(page.Id))
                    _tree.ItemSet(i, GuiItemProp.Expanded, "1");
                if (page.Level == WikiLevel.Important)
                    _tree.ItemSet(i, GuiItemProp.Color, GuiDraw.Hex(GuiStyle.Important));
                else if (page.Level == WikiLevel.New && !_seen.Contains(page.NewKey))
                    _tree.ItemSet(i, GuiItemProp.Badge, _german ? "Neu" : "New");
                AddItems(page.Id, hits, depth + 1);
            }
        }

        /// <summary>Selects the current page in the tree and expands the way to it.</summary>
        private void SelectInTree()
        {
            if (_current == null)
                return;
            int i = _tree.ItemFind(_current.Id);
            if (i < 0)
            {
                _tree.Select(-1, false);
                return;
            }
            if (_searchText.Length == 0)
            {
                bool changed = false;
                var key = _tree.Items[i].ParentKey;
                int guard = 0;
                while (key != null && guard++ < 50)
                {
                    changed |= _expanded.Add(key);
                    int p = _tree.ItemFind(key);
                    key = p >= 0 ? _tree.Items[p].ParentKey : null;
                }
                if (changed)
                    _service.SaveExpanded(_expanded);
            }
            _tree.Reveal(i);
            _tree.Select(i, false);
            _tree.ScrollToItem(i);
        }

        // ---------- page ----------

        private void Clear()
        {
            foreach (var c in _page.Children.ToArray())
            {
                _window.Forget(c);
                _page.Remove(c);
            }
            _page.Position = 0f;
        }

        private void BuildPage()
        {
            Clear();
            if (_current == null || _data == null)
                return;
            Header();
            foreach (var b in _current.Blocks)
            {
                try
                {
                    Block(b);
                }
                catch (Exception e)
                {
                    Log.Error("wiki block on page " + _current.Id, e);
                }
            }
        }

        private void Header()
        {
            var row = new GuiStack { Horizontal = true, FillWidth = true, Spacing = 14f };
            if (_current.Icons != null)
            {
                var icon = new GuiImage { Size = new Vector2(72f, 72f) };
                icon.SetText(GuiProp.Material, string.Join(";", _current.Icons));
                row.Add(icon);
            }
            var titles = new GuiStack { FillWidth = true, Spacing = 2f, Anchor = 3 };
            titles.Add(Label(WikiMarkup.Escape(_current.Title(_german)), GuiStyle.Text, 1.5f));
            if (!string.IsNullOrEmpty(_current.Source))
                titles.Add(Label(WikiMarkup.Escape(_current.Source), GuiStyle.TextQuiet, 0.85f));
            row.Add(titles);
            _page.Add(row);
            _page.Add(Line());
        }

        private void Block(WikiBlock b)
        {
            switch (b.Kind)
            {
                case WikiBlockKind.Heading:
                    _page.Add(Space(8f));
                    _page.Add(Label(WikiMarkup.ToGui(b.Text(_german), _data, _german), GuiStyle.Accent, 1.2f));
                    break;
                case WikiBlockKind.Note:
                    var note = new GuiElement(GuiKind.Panel) { Style = GuiStyleId.Dark, Padding = new Vector2(16f, 12f), BorderColor = GuiStyle.Line };
                    note.Add(Label(WikiMarkup.ToGui(b.Text(_german), _data, _german), GuiStyle.Important, 1f, true));
                    _page.Add(note);
                    break;
                case WikiBlockKind.Table:
                    _page.Add(Table(b));
                    break;
                case WikiBlockKind.List:
                    _page.Add(List(b));
                    break;
                case WikiBlockKind.Image:
                    var image = new GuiImage { Size = new Vector2(b.Width, b.Height), Anchor = 1 };
                    image.SetText(GuiProp.Material, b.Material);
                    _page.Add(image);
                    var caption = b.Text(_german);
                    if (!string.IsNullOrEmpty(caption))
                    {
                        var label = Label(WikiMarkup.ToGui(caption, _data, _german), GuiStyle.TextQuiet, 0.85f);
                        label.Align = 1;
                        _page.Add(label);
                    }
                    break;
                case WikiBlockKind.FactSheet:
                    _page.Add(FactSheet(b));
                    break;
                default:
                    _page.Add(Label(WikiMarkup.ToGui(b.Text(_german), _data, _german), GuiStyle.Text, 1f));
                    break;
            }
        }

        private GuiElement Table(WikiBlock b)
        {
            var table = new GuiTable { FillWidth = true };
            table.Handler = OnLink;
            var header = b.Header(_german);
            if (header != null)
                table.SetText(GuiProp.Header, Join(header));
            if (b.Widths != null)
            {
                var parts = new string[b.Widths.Length];
                for (int i = 0; i < parts.Length; i++)
                    parts[i] = b.Widths[i].ToString(CultureInfo.InvariantCulture);
                table.SetText(GuiProp.Widths, string.Join("|", parts));
            }
            foreach (var row in b.Rows(_german))
                table.ItemAdd(null, null, Join(row), null);
            return table;
        }

        private string Join(string[] cells)
        {
            var parts = new string[cells.Length];
            for (int i = 0; i < cells.Length; i++)
                parts[i] = WikiMarkup.ToGui(cells[i], _data, _german);
            return string.Join(GuiTable.CellSeparator.ToString(), parts);
        }

        private GuiElement List(WikiBlock b)
        {
            if (b.Entries == null || b.Entries.Count == 0)
                return Label(_german ? "Keine Einträge." : "No entries.", GuiStyle.TextQuiet, 1f);
            var list = new GuiList { FillWidth = true, Columns = Math.Max(1, b.Columns) };
            list.SetVector(GuiProp.IconSize, new Vector2(40f, 40f));
            list.Handler = OnLink;
            for (int n = 0; n < b.Entries.Count; n++)
            {
                var e = b.Entries[n];
                var text = e.Text(_german);
                if (string.IsNullOrEmpty(text) && e.Target != null)
                {
                    WikiPage target;
                    text = _data.Pages.TryGetValue(e.Target, out target) ? target.Title(_german) : e.Target;
                }
                int i = list.ItemAdd(e.Target ?? NoTarget + n, null, text ?? "", e.Icons != null ? string.Join(";", e.Icons) : null);
                var extra = e.Extra(_german);
                if (!string.IsNullOrEmpty(extra))
                    list.ItemSet(i, GuiItemProp.Extra, extra);
                if (e.Target == null)
                    list.ItemSet(i, GuiItemProp.Color, GuiDraw.Hex(GuiStyle.Text));
            }
            return list;
        }

        private GuiElement FactSheet(WikiBlock b)
        {
            var text = Label(WikiMarkup.ToGui(b.Text(_german), _data, _german), GuiStyle.Text, 1f, true);
            if (string.IsNullOrEmpty(b.Material))
                return text;
            var row = new GuiStack { Horizontal = true, FillWidth = true, Spacing = 20f };
            var left = new GuiStack { Size = new Vector2(b.Width, 0f), Spacing = 4f };
            var image = new GuiImage { Size = new Vector2(b.Width, b.Height) };
            image.SetText(GuiProp.Material, b.Material);
            left.Add(image);
            if (!string.IsNullOrEmpty(b.ImageSource))
                left.Add(Label(WikiMarkup.Escape((_german ? "Bild: " : "Image: ") + b.ImageSource), GuiStyle.TextQuiet, 0.7f));
            row.Add(left);
            row.Add(text);
            return row;
        }

        // ---------- small parts ----------

        private GuiLabel Label(string markup, Color color, float scale, bool fill = false)
        {
            var label = new GuiLabel { FillWidth = true, TextColor = color, TextScale = GuiStyle.TextScale * scale };
            label.SetText(GuiProp.Text, markup);
            label.Handler = OnLink;
            return label;
        }

        private static GuiElement Space(float height)
        {
            return new GuiElement(GuiKind.Panel) { Size = new Vector2(0f, height) };
        }

        private static GuiElement Line()
        {
            return new GuiElement(GuiKind.Panel) { Size = new Vector2(0f, 1f), Color = GuiStyle.Line };
        }
    }
}
