// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using VRageMath;

namespace Syntrix.Gui
{
    /// <summary>One item of a list, tree, table, tab row or drop-down.</summary>
    public class GuiItem
    {
        public string Key;
        public string ParentKey;
        public string Text = "";
        public string[] Icons;
        public bool Enabled = true;
        public bool Expanded;
        public string Extra;
        public Color? Color;
        public string Tooltip;
        public string Badge;
        public int Depth;
        public bool Shown = true;
        public GuiRichText[] Cells;
        public float Height;
    }

    /// <summary>Base of all item elements: item storage, own scrolling when the height is fixed, selection.</summary>
    public abstract class GuiItemsBase : GuiElement
    {
        public readonly List<GuiItem> Items = new List<GuiItem>();
        public int Selected = -1;
        public float RowHeight;
        public Vector2 IconSize;
        protected float ScrollPos;
        protected float ContentHeight;
        protected int HoverIndex = -1;
        private bool _drag;
        private float _dragStart, _dragPos;

        protected GuiItemsBase(int kind) : base(kind)
        {
        }

        public override bool Interactive => true;

        public override int ItemCount => Items.Count;

        /// <summary>True when the element has a fixed height and scrolls itself.</summary>
        protected bool Scrolls => Size.Y > 0f || FillHeight;

        protected float BarWidth => 12f * K;

        protected float ScrollMax => Scrolls ? Math.Max(0f, ContentHeight - ContentRect.Height) : 0f;

        protected float Row(GuiItem item)
        {
            float line = GuiDraw.LineHeight(EffectiveFont, TextScale);
            float icon = item.Icons != null ? (IconSize.Y > 0f ? IconSize.Y : 32f) * K : 0f;
            float h = RowHeight > 0f ? RowHeight * K : Math.Max(line + 10f * K, icon + 6f * K);
            return h;
        }

        public override int ItemAdd(string key, string parentKey, string text, string icons)
        {
            var item = new GuiItem
            {
                Key = key ?? Items.Count.ToString(),
                ParentKey = string.IsNullOrEmpty(parentKey) ? null : parentKey,
                Text = text ?? "",
                Icons = string.IsNullOrEmpty(icons) ? null : icons.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries),
            };
            Items.Add(item);
            ItemsChanged();
            return Items.Count - 1;
        }

        public override void ItemSet(int index, int prop, string value)
        {
            if (index < 0 || index >= Items.Count)
                return;
            var item = Items[index];
            switch (prop)
            {
                case GuiItemProp.Text: item.Text = value ?? ""; item.Cells = null; break;
                case GuiItemProp.Icons: item.Icons = string.IsNullOrEmpty(value) ? null : value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries); break;
                case GuiItemProp.Enabled: item.Enabled = value != "0"; break;
                case GuiItemProp.Expanded: item.Expanded = value == "1"; break;
                case GuiItemProp.Extra: item.Extra = string.IsNullOrEmpty(value) ? null : value; break;
                case GuiItemProp.Color:
                    Color c;
                    item.Color = GuiDraw.ParseColor(value, out c) ? c : (Color?)null;
                    break;
                case GuiItemProp.Tooltip: item.Tooltip = string.IsNullOrEmpty(value) ? null : value; break;
                case GuiItemProp.Badge: item.Badge = string.IsNullOrEmpty(value) ? null : value; break;
            }
            ItemsChanged();
        }

        public override void ItemRemove(int index)
        {
            if (index < 0 || index >= Items.Count)
                return;
            Items.RemoveAt(index);
            if (Selected == index)
                Selected = -1;
            else if (Selected > index)
                Selected--;
            ItemsChanged();
        }

        public override void ItemClear()
        {
            Items.Clear();
            Selected = -1;
            HoverIndex = -1;
            ItemsChanged();
        }

        public override int ItemFind(string key)
        {
            for (int i = 0; i < Items.Count; i++)
                if (Items[i].Key == key)
                    return i;
            return -1;
        }

        protected virtual void ItemsChanged()
        {
            Invalidate();
        }

        public void Select(int index, bool fire)
        {
            if (index < -1 || index >= Items.Count)
                return;
            Selected = index;
            if (fire && index >= 0)
                Fire(GuiEvent.Select, index, Items[index].Key);
        }

        // ---------- scrolling ----------

        protected void ClampScroll()
        {
            ScrollPos = MathHelper.Clamp(ScrollPos, 0f, ScrollMax);
        }

        protected RectangleF ListArea
        {
            get
            {
                var c = ContentRect;
                return ScrollMax > 0.5f ? new RectangleF(c.X, c.Y, Math.Max(0f, c.Width - BarWidth - 4f * K), c.Height) : c;
            }
        }

        private RectangleF Track
        {
            get
            {
                var c = ContentRect;
                return new RectangleF(c.X + c.Width - BarWidth, c.Y, BarWidth, c.Height);
            }
        }

        private RectangleF Thumb
        {
            get
            {
                var t = Track;
                float h = Math.Max(30f * K, t.Height * t.Height / Math.Max(1f, ContentHeight));
                float y = t.Y + (t.Height - h) * (ScrollPos / Math.Max(1f, ScrollMax));
                return new RectangleF(t.X, y, t.Width, h);
            }
        }

        protected void DrawBar()
        {
            if (ScrollMax <= 0.5f)
                return;
            var t = Thumb;
            bool hover = _drag;
            GuiDraw.ThreeV(hover ? GuiStyle.ScrollTopHover : GuiStyle.ScrollTop, hover ? GuiStyle.ScrollCenterHover : GuiStyle.ScrollCenter,
                hover ? GuiStyle.ScrollBottomHover : GuiStyle.ScrollBottom, t, Color.White, 6f);
        }

        protected bool BarDown(Vector2 p)
        {
            if (ScrollMax <= 0.5f || !GuiDraw.Inside(Track, p))
                return false;
            var thumb = Thumb;
            if (!GuiDraw.Inside(thumb, p))
                ScrollPos = MathHelper.Clamp((p.Y - Track.Y - thumb.Height / 2f) / Math.Max(1f, Track.Height - thumb.Height), 0f, 1f) * ScrollMax;
            _drag = true;
            _dragStart = p.Y;
            _dragPos = ScrollPos;
            return true;
        }

        public override void MouseDrag(Vector2 p)
        {
            if (!_drag)
                return;
            float range = Math.Max(1f, Track.Height - Thumb.Height);
            ScrollPos = MathHelper.Clamp(_dragPos + (p.Y - _dragStart) / range * ScrollMax, 0f, ScrollMax);
        }

        public override void MouseUp(Vector2 p, int button, bool inside)
        {
            if (_drag)
            {
                _drag = false;
                return;
            }
            ItemUp(p, button, inside);
        }

        protected virtual void ItemUp(Vector2 p, int button, bool inside)
        {
        }

        public override bool Wheel(Vector2 p, int delta)
        {
            if (ScrollMax <= 0.5f)
                return false;
            ScrollPos = MathHelper.Clamp(ScrollPos - Math.Sign(delta) * 80f * K, 0f, ScrollMax);
            return true;
        }

        /// <summary>Scrolls the item into view (own scrolling only).</summary>
        public void ScrollToItem(int index)
        {
            if (!Scrolls || index < 0 || index >= Items.Count)
                return;
            float y = 0f;
            for (int i = 0; i < index; i++)
                if (Items[i].Shown)
                    y += Items[i].Height;
            float h = Items[index].Height;
            float view = ContentRect.Height;
            if (y < ScrollPos)
                ScrollPos = y;
            else if (y + h > ScrollPos + view)
                ScrollPos = y + h - view;
            ClampScroll();
        }

        public override void MouseMove(Vector2 p)
        {
            HoverIndex = IndexAt(p);
        }

        public override void MouseLeave()
        {
            HoverIndex = -1;
        }

        protected virtual int IndexAt(Vector2 p)
        {
            return -1;
        }

        public override string TooltipAt(Vector2 p)
        {
            int i = IndexAt(p);
            if (i >= 0 && Items[i].Tooltip != null)
                return Items[i].Tooltip;
            return Tooltip;
        }

        protected void DrawIcons(string[] icons, RectangleF r, bool enabled)
        {
            if (icons == null)
                return;
            foreach (var m in icons)
                GuiDraw.TextureFit(m, r, enabled ? Color.White : GuiStyle.TextDisabled);
        }

        protected void DrawBadge(string badge, float x, RectangleF row)
        {
            if (string.IsNullOrEmpty(badge))
                return;
            float scale = TextScale * 0.75f;
            float w = GuiDraw.Width(GuiStyle.FontBright, badge, scale) + 10f * K;
            float h = GuiDraw.LineHeight(GuiStyle.FontBright, scale) + 2f * K;
            var r = new RectangleF(x, row.Y + (row.Height - h) / 2f, w, h);
            GuiDraw.Rect(r, new Color(GuiStyle.Important, 0.85f));
            GuiDraw.TextIn(GuiStyle.FontBright, badge, r, scale, new Color(20, 20, 20), 1);
        }

        public override bool SetInt(int prop, int v)
        {
            if (prop == GuiProp.Selected) { Select(v, false); return true; }
            return base.SetInt(prop, v);
        }

        public override bool GetInt(int prop, out int v)
        {
            if (prop == GuiProp.Selected) { v = Selected; return true; }
            return base.GetInt(prop, out v);
        }

        public override bool SetText(int prop, string v)
        {
            if (prop == GuiProp.SelectedKey) { Select(ItemFind(v), false); return true; }
            return base.SetText(prop, v);
        }

        public override bool GetText(int prop, out string v)
        {
            if (prop == GuiProp.SelectedKey) { v = Selected >= 0 && Selected < Items.Count ? Items[Selected].Key : null; return true; }
            return base.GetText(prop, out v);
        }

        public override bool SetFloat(int prop, float v)
        {
            if (prop == GuiProp.RowHeight) { RowHeight = v; Invalidate(); return true; }
            if (prop == GuiProp.ScrollPosition) { ScrollPos = v * K; ClampScroll(); return true; }
            return base.SetFloat(prop, v);
        }

        public override bool GetFloat(int prop, out float v)
        {
            if (prop == GuiProp.RowHeight) { v = RowHeight; return true; }
            if (prop == GuiProp.ScrollPosition) { v = ScrollPos / K; return true; }
            if (prop == GuiProp.ScrollMax) { v = ScrollMax / K; return true; }
            return base.GetFloat(prop, out v);
        }

        public override bool SetVector(int prop, Vector2 v)
        {
            if (prop == GuiProp.IconSize) { IconSize = v; Invalidate(); return true; }
            return base.SetVector(prop, v);
        }
    }

    /// <summary>List with icons, second text (Extra), badge, selection; several columns with Columns &gt; 1 (tiles).</summary>
    public class GuiList : GuiItemsBase
    {
        public int Columns = 1;

        public GuiList() : base(GuiKind.List)
        {
            Style = GuiStyleId.List;
            Padding = new Vector2(4f, 4f);
        }

        private float ColumnWidth(float width)
        {
            int cols = Math.Max(1, Columns);
            return (width - (cols - 1) * 8f * K) / cols;
        }

        private void LayoutItems(float width)
        {
            int cols = Math.Max(1, Columns);
            float colW = ColumnWidth(width);
            float total = 0f, rowMax = 0f;
            int col = 0;
            foreach (var item in Items)
            {
                item.Height = Row(item);
                if (!string.IsNullOrEmpty(item.Extra) && cols > 1)
                    item.Height += GuiDraw.LineHeight(EffectiveFont, TextScale * 0.85f);
                rowMax = Math.Max(rowMax, item.Height);
                if (++col == cols)
                {
                    total += rowMax;
                    col = 0;
                    rowMax = 0f;
                }
            }
            total += rowMax;
            ContentHeight = total;
        }

        public override Vector2 Measure(float width, float height)
        {
            float pw = Padding.X * K * 2f, ph = Padding.Y * K * 2f;
            LayoutItems(Math.Max(0f, width - pw));
            return new Vector2(width, ContentHeight + ph);
        }

        public override void Arrange(RectangleF rect, RectangleF clip)
        {
            base.Arrange(rect, clip);
            LayoutItems(ListArea.Width);
            ClampScroll();
        }

        private RectangleF Cell(int index, out bool visible)
        {
            var area = ListArea;
            int cols = Math.Max(1, Columns);
            float colW = ColumnWidth(area.Width);
            float y = area.Y - ScrollPos, rowMax = 0f;
            int col = 0;
            for (int i = 0; i < Items.Count; i++)
            {
                var h = Items[i].Height;
                if (i == index)
                {
                    var r = new RectangleF(area.X + col * (colW + 8f * K), y, colW, h);
                    visible = r.Y + r.Height > area.Y && r.Y < area.Y + area.Height;
                    return r;
                }
                rowMax = Math.Max(rowMax, h);
                if (++col == cols)
                {
                    y += rowMax;
                    rowMax = 0f;
                    col = 0;
                }
            }
            visible = false;
            return new RectangleF();
        }

        protected override int IndexAt(Vector2 p)
        {
            var area = ListArea;
            if (!GuiDraw.Inside(area, p) || !GuiDraw.Inside(ClipArea, p))
                return -1;
            for (int i = 0; i < Items.Count; i++)
            {
                bool visible;
                var r = Cell(i, out visible);
                if (visible && GuiDraw.Inside(r, p))
                    return i;
            }
            return -1;
        }

        protected override void DrawContent()
        {
            var area = ListArea;
            using (GuiDraw.PushClip(area))
            {
                int cols = Math.Max(1, Columns);
                for (int i = 0; i < Items.Count; i++)
                {
                    bool visible;
                    var r = Cell(i, out visible);
                    if (!visible)
                        continue;
                    var item = Items[i];
                    bool enabled = item.Enabled && IsEnabled;
                    if (i == Selected)
                        GuiDraw.Texture(GuiStyle.ItemSelected, r, Color.White);
                    else if (i == HoverIndex && enabled && Hovered)
                        GuiDraw.Texture(GuiStyle.ItemHover, r, Color.White);
                    float x = r.X + 6f * K;
                    float rowH = cols > 1 && !string.IsNullOrEmpty(item.Extra) ? r.Height - GuiDraw.LineHeight(EffectiveFont, TextScale * 0.85f) : r.Height;
                    if (item.Icons != null)
                    {
                        var s = IconSize.X > 0f ? IconSize * K : new Vector2(32f * K, 32f * K);
                        DrawIcons(item.Icons, new RectangleF(x, r.Y + (rowH - s.Y) / 2f, s.X, s.Y), enabled);
                        x += s.X + 8f * K;
                    }
                    float extraW = 0f;
                    if (!string.IsNullOrEmpty(item.Extra) && cols == 1)
                    {
                        extraW = GuiDraw.Width(EffectiveFont, item.Extra, TextScale) + 12f * K;
                        GuiDraw.TextIn(EffectiveFont, item.Extra, new RectangleF(r.X, r.Y, r.Width - 8f * K, r.Height), TextScale, GuiStyle.TextQuiet, 2);
                    }
                    float textW = Math.Max(0f, r.X + r.Width - x - extraW - 6f * K);
                    var font = i == HoverIndex && enabled && Hovered ? GuiStyle.FontBright : EffectiveFont;
                    var plain = GuiRichText.Plain(item.Text);
                    GuiDraw.TextIn(font, plain, new RectangleF(x, r.Y, textW, rowH), TextScale, item.Color ?? (enabled ? TextColor : GuiStyle.TextDisabled), 0);
                    if (item.Badge != null)
                        DrawBadge(item.Badge, Math.Min(x + GuiDraw.Width(font, plain, TextScale) + 8f * K, r.X + r.Width - 60f * K), new RectangleF(r.X, r.Y, r.Width, rowH));
                    if (!string.IsNullOrEmpty(item.Extra) && cols > 1)
                        GuiDraw.TextIn(EffectiveFont, item.Extra, new RectangleF(x, r.Y + rowH - 4f * K, textW, r.Height - rowH), TextScale * 0.85f, GuiStyle.TextQuiet, 0);
                }
            }
        }

        protected override void DrawOver()
        {
            DrawBar();
        }

        private int _downIndex = -1;

        public override bool MouseDown(Vector2 p, int button)
        {
            if (BarDown(p))
                return true;
            _downIndex = IndexAt(p);
            return _downIndex >= 0;
        }

        protected override void ItemUp(Vector2 p, int button, bool inside)
        {
            int i = IndexAt(p);
            if (!inside || i < 0 || i != _downIndex || !Items[i].Enabled || !IsEnabled)
                return;
            if (button == 0)
                Select(i, true);
            Fire(GuiEvent.Click, button, Items[i].Key);
        }

        public override void DoubleClick(Vector2 p)
        {
            int i = IndexAt(p);
            if (i >= 0 && Items[i].Enabled)
                Fire(GuiEvent.DoubleClick, i, Items[i].Key);
        }

        public override bool SetInt(int prop, int v)
        {
            if (prop == GuiProp.Columns) { Columns = MathHelper.Clamp(v, 1, 12); Invalidate(); return true; }
            return base.SetInt(prop, v);
        }

        public override bool GetInt(int prop, out int v)
        {
            if (prop == GuiProp.Columns) { v = Columns; return true; }
            return base.GetInt(prop, out v);
        }
    }

    /// <summary>Tree: items with ParentKey below their parent, arrow to expand (event Expand), selection (event Select).</summary>
    public class GuiTree : GuiItemsBase
    {
        private readonly List<int> _order = new List<int>();
        private bool _orderValid;

        public GuiTree() : base(GuiKind.Tree)
        {
            Style = GuiStyleId.List;
            Padding = new Vector2(4f, 4f);
        }

        protected override void ItemsChanged()
        {
            _orderValid = false;
            base.ItemsChanged();
        }

        /// <summary>Visible items in tree order (children of collapsed items hidden).</summary>
        private void BuildOrder()
        {
            if (_orderValid)
                return;
            _orderValid = true;
            _order.Clear();
            var children = new Dictionary<string, List<int>>();
            var keys = new HashSet<string>();
            foreach (var item in Items)
                keys.Add(item.Key);
            for (int i = 0; i < Items.Count; i++)
            {
                var p = Items[i].ParentKey;
                if (p == null || !keys.Contains(p) || p == Items[i].Key)
                    p = "";
                List<int> list;
                if (!children.TryGetValue(p, out list))
                    children[p] = list = new List<int>();
                list.Add(i);
            }
            foreach (var item in Items)
                item.Shown = false;
            var seen = new HashSet<int>();
            Action<string, int, bool> walk = null;
            walk = (key, depth, shown) =>
            {
                List<int> list;
                if (!children.TryGetValue(key, out list))
                    return;
                foreach (int i in list)
                {
                    if (!seen.Add(i))
                        continue;
                    var item = Items[i];
                    item.Depth = depth;
                    item.Shown = shown;
                    if (shown)
                        _order.Add(i);
                    walk(item.Key, depth + 1, shown && item.Expanded);
                }
            };
            walk("", 0, true);
        }

        private bool HasChildren(GuiItem item)
        {
            foreach (var other in Items)
                if (other.ParentKey == item.Key && other != item)
                    return true;
            return false;
        }

        public override Vector2 Measure(float width, float height)
        {
            BuildOrder();
            float h = 0f;
            foreach (int i in _order)
                h += Items[i].Height = Row(Items[i]);
            ContentHeight = h;
            return new Vector2(width, h + Padding.Y * K * 2f);
        }

        public override void Arrange(RectangleF rect, RectangleF clip)
        {
            base.Arrange(rect, clip);
            Measure(rect.Width, rect.Height);
            ClampScroll();
        }

        public override void Invalidate()
        {
            _orderValid = false;
            base.Invalidate();
        }

        private float Indent => 20f * K;

        protected override int IndexAt(Vector2 p)
        {
            var area = ListArea;
            if (!GuiDraw.Inside(area, p) || !GuiDraw.Inside(ClipArea, p))
                return -1;
            BuildOrder();
            float y = area.Y - ScrollPos;
            foreach (int i in _order)
            {
                float h = Items[i].Height;
                if (p.Y >= y && p.Y < y + h)
                    return i;
                y += h;
            }
            return -1;
        }

        private RectangleF RowRect(int index)
        {
            var area = ListArea;
            float y = area.Y - ScrollPos;
            foreach (int i in _order)
            {
                if (i == index)
                    return new RectangleF(area.X, y, area.Width, Items[i].Height);
                y += Items[i].Height;
            }
            return new RectangleF();
        }

        protected override void DrawContent()
        {
            BuildOrder();
            var area = ListArea;
            using (GuiDraw.PushClip(area))
            {
                float y = area.Y - ScrollPos;
                foreach (int i in _order)
                {
                    var item = Items[i];
                    float h = item.Height;
                    var r = new RectangleF(area.X, y, area.Width, h);
                    y += h;
                    if (r.Y + h < area.Y || r.Y > area.Y + area.Height)
                        continue;
                    bool enabled = item.Enabled && IsEnabled;
                    bool hover = i == HoverIndex && enabled && Hovered;
                    if (i == Selected)
                        GuiDraw.Texture(GuiStyle.ItemSelected, r, Color.White);
                    else if (hover)
                        GuiDraw.Texture(GuiStyle.ItemHover, r, Color.White);
                    float x = r.X + 4f * K + item.Depth * Indent;
                    float arrow = 16f * K;
                    if (HasChildren(item))
                    {
                        var a = new RectangleF(x, r.Y + (h - arrow) / 2f, arrow, arrow);
                        GuiDraw.TextureRotated(GuiStyle.ArrowRight, a, hover ? Color.White : GuiStyle.TextQuiet, item.Expanded ? MathHelper.PiOver2 : 0f);
                    }
                    x += arrow + 4f * K;
                    if (item.Icons != null)
                    {
                        var s = IconSize.X > 0f ? IconSize * K : new Vector2(24f * K, 24f * K);
                        DrawIcons(item.Icons, new RectangleF(x, r.Y + (h - s.Y) / 2f, s.X, s.Y), enabled);
                        x += s.X + 6f * K;
                    }
                    var plain = GuiRichText.Plain(item.Text);
                    var font = hover || i == Selected ? GuiStyle.FontBright : EffectiveFont;
                    float badgeW = item.Badge != null ? GuiDraw.Width(GuiStyle.FontBright, item.Badge, TextScale * 0.75f) + 18f * K : 0f;
                    var textRect = new RectangleF(x, r.Y, Math.Max(0f, r.X + r.Width - x - badgeW - 4f * K), h);
                    GuiDraw.TextIn(font, plain, textRect, TextScale, item.Color ?? (enabled ? TextColor : GuiStyle.TextDisabled), 0);
                    if (item.Badge != null)
                        DrawBadge(item.Badge, Math.Min(x + GuiDraw.Width(font, plain, TextScale) + 8f * K, r.X + r.Width - badgeW), r);
                }
            }
        }

        protected override void DrawOver()
        {
            DrawBar();
        }

        private int _downIndex = -1;

        public override bool MouseDown(Vector2 p, int button)
        {
            if (BarDown(p))
                return true;
            _downIndex = IndexAt(p);
            return _downIndex >= 0;
        }

        protected override void ItemUp(Vector2 p, int button, bool inside)
        {
            int i = IndexAt(p);
            if (!inside || i < 0 || i != _downIndex || !IsEnabled)
                return;
            var item = Items[i];
            var r = RowRect(i);
            float arrowRight = r.X + 4f * K + item.Depth * Indent + 20f * K;
            bool onArrow = p.X < arrowRight && HasChildren(item);
            if (onArrow || button == 0 && i == Selected && HasChildren(item))
            {
                item.Expanded = !item.Expanded;
                _orderValid = false;
                Invalidate();
                Fire(GuiEvent.Expand, i, item.Expanded ? "1" : "0");
                if (onArrow)
                    return;
            }
            if (!item.Enabled)
                return;
            if (button == 0)
                Select(i, true);
            Fire(GuiEvent.Click, button, item.Key);
        }

        public override void DoubleClick(Vector2 p)
        {
            int i = IndexAt(p);
            if (i >= 0 && Items[i].Enabled)
                Fire(GuiEvent.DoubleClick, i, Items[i].Key);
        }

        /// <summary>Expands all parents of an item so it is shown.</summary>
        public void Reveal(int index)
        {
            if (index < 0 || index >= Items.Count)
                return;
            var key = Items[index].ParentKey;
            int guard = 0;
            while (key != null && guard++ < 64)
            {
                int p = ItemFind(key);
                if (p < 0)
                    break;
                Items[p].Expanded = true;
                key = Items[p].ParentKey;
            }
            _orderValid = false;
            Invalidate();
        }
    }

    /// <summary>
    /// Table: header (GuiProp.Header, cells separated by \u001f), rows as items (cells separated by \u001f), relative
    /// column widths (GuiProp.Widths "2|1|3", else by text length), wrapping and markup in cells, links (event Link),
    /// click on a row (event Select). Grows with its rows (put it into a Scroll for long tables).
    /// </summary>
    public class GuiTable : GuiItemsBase
    {
        public const char CellSeparator = '\u001f';
        private string[] _header;
        private GuiRichText[] _headerRich;
        private float[] _widths;
        private float[] _columnWidths;
        private float _headerHeight;
        private float _laidWidth = -1f;
        private string _hoverLink;

        public GuiTable() : base(GuiKind.Table)
        {
        }

        private float CellPad => 8f * K;

        protected override void ItemsChanged()
        {
            _laidWidth = -1f;
            base.ItemsChanged();
        }

        public override void Invalidate()
        {
            _laidWidth = -1f;
            base.Invalidate();
        }

        private int ColumnCount
        {
            get
            {
                int n = _header != null ? _header.Length : 0;
                foreach (var item in Items)
                    n = Math.Max(n, item.Text.Split(CellSeparator).Length);
                return Math.Max(1, n);
            }
        }

        private void LayoutTable(float width)
        {
            if (Math.Abs(width - _laidWidth) < 0.5f)
                return;
            _laidWidth = width;
            int n = ColumnCount;
            // Relative widths: given, else by the longest text per column (capped), at least 1
            var rel = new float[n];
            for (int c = 0; c < n; c++)
            {
                if (_widths != null && c < _widths.Length && _widths[c] > 0f)
                {
                    rel[c] = _widths[c];
                    continue;
                }
                float longest = 4f;
                if (_header != null && c < _header.Length)
                    longest = Math.Max(longest, GuiRichText.Plain(_header[c]).Length);
                foreach (var item in Items)
                {
                    var cells = item.Text.Split(CellSeparator);
                    if (c < cells.Length)
                        longest = Math.Max(longest, Math.Min(40f, GuiRichText.Plain(cells[c]).Length));
                }
                rel[c] = longest;
            }
            float sum = 0f;
            foreach (var r in rel)
                sum += r;
            _columnWidths = new float[n];
            for (int c = 0; c < n; c++)
                _columnWidths[c] = width * rel[c] / Math.Max(0.001f, sum);
            var font = EffectiveFont;
            _headerHeight = 0f;
            if (_header != null)
            {
                _headerRich = new GuiRichText[n];
                for (int c = 0; c < n; c++)
                {
                    var rt = _headerRich[c] = new GuiRichText();
                    rt.Set(c < _header.Length ? _header[c] : "", true, GuiStyle.FontBright, TextScale, GuiStyle.Accent, true);
                    rt.Layout(Math.Max(10f, _columnWidths[c] - 2f * CellPad));
                    _headerHeight = Math.Max(_headerHeight, rt.Height + 2f * CellPad * 0.75f);
                }
            }
            float total = _headerHeight;
            foreach (var item in Items)
            {
                var cells = item.Text.Split(CellSeparator);
                item.Cells = new GuiRichText[n];
                float h = 0f;
                for (int c = 0; c < n; c++)
                {
                    var rt = item.Cells[c] = new GuiRichText();
                    rt.Set(c < cells.Length ? cells[c] : "", true, font, TextScale, item.Color ?? TextColor, true);
                    rt.Layout(Math.Max(10f, _columnWidths[c] - 2f * CellPad));
                    h = Math.Max(h, rt.Height);
                }
                item.Height = h + 2f * CellPad * 0.75f;
                total += item.Height;
            }
            ContentHeight = total;
        }

        public override Vector2 Measure(float width, float height)
        {
            LayoutTable(width - Padding.X * K * 2f);
            return new Vector2(width, ContentHeight + Padding.Y * K * 2f);
        }

        public override void Arrange(RectangleF rect, RectangleF clip)
        {
            base.Arrange(rect, clip);
            LayoutTable(ListArea.Width);
            ClampScroll();
        }

        protected override int IndexAt(Vector2 p)
        {
            var area = ListArea;
            if (!GuiDraw.Inside(area, p) || !GuiDraw.Inside(ClipArea, p))
                return -1;
            float y = area.Y - ScrollPos + _headerHeight;
            for (int i = 0; i < Items.Count; i++)
            {
                if (p.Y >= y && p.Y < y + Items[i].Height)
                    return i;
                y += Items[i].Height;
            }
            return -1;
        }

        private string LinkAt(Vector2 p)
        {
            int i = IndexAt(p);
            if (i < 0 || Items[i].Cells == null)
                return null;
            var area = ListArea;
            float y = area.Y - ScrollPos + _headerHeight;
            for (int j = 0; j < i; j++)
                y += Items[j].Height;
            float x = area.X;
            for (int c = 0; c < Items[i].Cells.Length; c++)
            {
                var link = Items[i].Cells[c].LinkAt(new Vector2(x + CellPad, y + CellPad * 0.75f), _columnWidths[c] - 2f * CellPad, p);
                if (link != null)
                    return link;
                x += _columnWidths[c];
            }
            return null;
        }

        protected override void DrawContent()
        {
            if (_columnWidths == null)
                return;
            var area = ListArea;
            using (GuiDraw.PushClip(area))
            {
                float y = area.Y - ScrollPos;
                int n = _columnWidths.Length;
                if (_headerRich != null)
                {
                    GuiDraw.Rect(new RectangleF(area.X, y, area.Width, _headerHeight), new Color(30, 42, 50, 220));
                    float x = area.X;
                    for (int c = 0; c < n; c++)
                    {
                        _headerRich[c].Draw(new Vector2(x + CellPad, y + CellPad * 0.75f), _columnWidths[c] - 2f * CellPad);
                        x += _columnWidths[c];
                    }
                    y += _headerHeight;
                    GuiDraw.Rect(new RectangleF(area.X, y - Math.Max(1f, K), area.Width, Math.Max(1f, K)), GuiStyle.Line);
                }
                for (int i = 0; i < Items.Count; i++)
                {
                    var item = Items[i];
                    float h = item.Height;
                    if (y + h >= area.Y && y <= area.Y + area.Height && item.Cells != null)
                    {
                        var row = new RectangleF(area.X, y, area.Width, h);
                        if (i == Selected)
                            GuiDraw.Texture(GuiStyle.ItemSelected, row, Color.White);
                        else if (i % 2 == 1)
                            GuiDraw.Rect(row, new Color(255, 255, 255, 10));
                        float x = area.X;
                        for (int c = 0; c < n && c < item.Cells.Length; c++)
                        {
                            item.Cells[c].HoverLink = _hoverLink;
                            item.Cells[c].Draw(new Vector2(x + CellPad, y + CellPad * 0.75f), _columnWidths[c] - 2f * CellPad);
                            x += _columnWidths[c];
                        }
                    }
                    y += h;
                }
            }
        }

        protected override void DrawOver()
        {
            DrawBar();
        }

        public override void MouseMove(Vector2 p)
        {
            base.MouseMove(p);
            _hoverLink = LinkAt(p);
        }

        private int _downIndex = -1;

        public override bool MouseDown(Vector2 p, int button)
        {
            if (BarDown(p))
                return true;
            _downIndex = IndexAt(p);
            return _downIndex >= 0;
        }

        protected override void ItemUp(Vector2 p, int button, bool inside)
        {
            if (!inside)
                return;
            var link = LinkAt(p);
            if (link != null && button == 0)
            {
                Fire(GuiEvent.Link, 0, link);
                return;
            }
            int i = IndexAt(p);
            if (i < 0 || i != _downIndex)
                return;
            if (button == 0)
                Select(i, true);
            Fire(GuiEvent.Click, button, Items[i].Key);
        }

        public override bool SetText(int prop, string v)
        {
            if (prop == GuiProp.Header)
            {
                _header = string.IsNullOrEmpty(v) ? null : v.Split(CellSeparator);
                Invalidate();
                return true;
            }
            if (prop == GuiProp.Widths)
            {
                if (string.IsNullOrEmpty(v))
                    _widths = null;
                else
                {
                    var parts = v.Split('|');
                    _widths = new float[parts.Length];
                    for (int i = 0; i < parts.Length; i++)
                    {
                        float w;
                        _widths[i] = float.TryParse(parts[i].Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out w) && w > 0f ? w : 1f;
                    }
                }
                Invalidate();
                return true;
            }
            return base.SetText(prop, v);
        }
    }

    /// <summary>Row of tabs (items); event Select when the player picks one.</summary>
    public class GuiTabs : GuiItemsBase
    {
        public GuiTabs() : base(GuiKind.Tabs)
        {
            Selected = 0;
        }

        public override Vector2 Measure(float width, float height)
        {
            return new Vector2(width, 44f * K);
        }

        private float TabWidth(int i)
        {
            return GuiDraw.Width(GuiStyle.FontBright, GuiRichText.Plain(Items[i].Text), TextScale) + 40f * K;
        }

        protected override int IndexAt(Vector2 p)
        {
            if (!GuiDraw.Inside(Rect, p))
                return -1;
            float x = Rect.X;
            for (int i = 0; i < Items.Count; i++)
            {
                float w = TabWidth(i);
                if (p.X >= x && p.X < x + w)
                    return i;
                x += w + 4f * K;
            }
            return -1;
        }

        protected override void DrawContent()
        {
            float x = Rect.X;
            for (int i = 0; i < Items.Count; i++)
            {
                var item = Items[i];
                float w = TabWidth(i);
                var r = new RectangleF(x, Rect.Y, w, Rect.Height);
                bool hover = i == HoverIndex && Hovered && item.Enabled;
                bool sel = i == Selected;
                GuiDraw.Texture(sel || hover ? GuiStyle.TabHover : GuiStyle.Tab, r, item.Enabled ? Color.White : new Color(160, 160, 160, 200));
                GuiDraw.TextIn(sel || hover ? GuiStyle.FontBright : EffectiveFont, GuiRichText.Plain(item.Text), r, TextScale,
                    item.Enabled ? (sel ? Color.White : TextColor) : GuiStyle.TextDisabled, 1);
                x += w + 4f * K;
            }
            GuiDraw.Rect(new RectangleF(Rect.X, Rect.Y + Rect.Height - Math.Max(1f, 2f * K), Rect.Width, Math.Max(1f, 2f * K)), GuiStyle.Line);
        }

        private int _downIndex = -1;

        public override bool MouseDown(Vector2 p, int button)
        {
            _downIndex = IndexAt(p);
            return _downIndex >= 0;
        }

        protected override void ItemUp(Vector2 p, int button, bool inside)
        {
            int i = IndexAt(p);
            if (!inside || i < 0 || i != _downIndex || !Items[i].Enabled || button != 0)
                return;
            if (i != Selected)
                Select(i, true);
        }
    }

    /// <summary>Drop-down: shows the selected item, a click opens the list above everything else of the window.</summary>
    public class GuiDropdown : GuiItemsBase
    {
        public bool Open;

        public GuiDropdown() : base(GuiKind.Dropdown)
        {
        }

        public override Vector2 Measure(float width, float height)
        {
            return new Vector2(Math.Max(160f * K, width), 40f * K);
        }

        private float ItemH => GuiDraw.LineHeight(EffectiveFont, TextScale) + 12f * K;

        private RectangleF Popup
        {
            get
            {
                float h = Math.Min(Items.Count, 10) * ItemH + 8f * K;
                return new RectangleF(Rect.X, Rect.Y + Rect.Height, Rect.Width, h);
            }
        }

        protected override void DrawBackground()
        {
            bool hover = Hovered && IsEnabled;
            GuiDraw.Three(hover ? GuiStyle.ComboLeftHover : GuiStyle.ComboLeft, hover ? GuiStyle.ComboCenterHover : GuiStyle.ComboCenter,
                hover ? GuiStyle.ComboRightHover : GuiStyle.ComboRight, Rect, IsEnabled ? Color.White : new Color(160, 160, 160, 200), 8f);
        }

        protected override void DrawContent()
        {
            var text = Selected >= 0 && Selected < Items.Count ? GuiRichText.Plain(Items[Selected].Text) : "";
            GuiDraw.TextIn(EffectiveFont, text, new RectangleF(Rect.X + 12f * K, Rect.Y, Rect.Width - 50f * K, Rect.Height), TextScale, IsEnabled ? TextColor : GuiStyle.TextDisabled, 0);
        }

        /// <summary>Drawn by the window after everything else.</summary>
        public void DrawPopup()
        {
            if (!Open)
                return;
            var r = Popup;
            GuiDraw.Nine(GuiStyle.DarkPrefix, GuiStyle.DarkCenter, r, Color.White, 4f);
            GuiDraw.Rect(r, new Color(15, 20, 25, 235));
            GuiDraw.Border(r, GuiStyle.Line, Math.Max(1f, K));
            float y = r.Y + 4f * K;
            int start = (int)(ScrollPos / Math.Max(1f, ItemH));
            for (int i = start; i < Items.Count && i < start + 10; i++)
            {
                var row = new RectangleF(r.X + 4f * K, y, r.Width - 8f * K, ItemH);
                if (i == HoverIndex)
                    GuiDraw.Texture(GuiStyle.ItemHover, row, Color.White);
                else if (i == Selected)
                    GuiDraw.Texture(GuiStyle.ItemSelected, row, Color.White);
                GuiDraw.TextIn(i == HoverIndex ? GuiStyle.FontBright : EffectiveFont, GuiRichText.Plain(Items[i].Text), new RectangleF(row.X + 8f * K, row.Y, row.Width - 16f * K, row.Height), TextScale,
                    Items[i].Enabled ? TextColor : GuiStyle.TextDisabled, 0);
                y += ItemH;
            }
        }

        /// <summary>Item index under the point in the open list, -1 if none.</summary>
        public int PopupIndexAt(Vector2 p)
        {
            if (!Open || !GuiDraw.Inside(Popup, p))
                return -1;
            int start = (int)(ScrollPos / Math.Max(1f, ItemH));
            int i = start + (int)((p.Y - Popup.Y - 4f * K) / ItemH);
            return i >= 0 && i < Items.Count ? i : -1;
        }

        public bool PopupContains(Vector2 p)
        {
            return Open && GuiDraw.Inside(Popup, p);
        }

        public void PopupHover(Vector2 p)
        {
            HoverIndex = PopupIndexAt(p);
        }

        public bool PopupWheel(int delta)
        {
            if (!Open)
                return false;
            ScrollPos = MathHelper.Clamp(ScrollPos - Math.Sign(delta) * ItemH, 0f, Math.Max(0f, (Items.Count - 10) * ItemH));
            return true;
        }

        public void PopupClick(Vector2 p)
        {
            int i = PopupIndexAt(p);
            Open = false;
            if (i >= 0 && Items[i].Enabled && i != Selected)
                Select(i, true);
        }

        public override bool MouseDown(Vector2 p, int button)
        {
            return IsEnabled && button == 0;
        }

        protected override void ItemUp(Vector2 p, int button, bool inside)
        {
            if (inside && IsEnabled && button == 0)
            {
                Open = !Open;
                ScrollPos = 0f;
                var w = Root as GuiWindow;
                if (w != null)
                    w.Popup = Open ? this : null;
            }
        }
    }
}
