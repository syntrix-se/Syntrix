// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using VRageMath;

namespace Syntrix.Gui
{
    /// <summary>Base of all item elements: item storage, own scrolling when the height is fixed, selection.</summary>
    public abstract class GuiItemsBase : GuiElement
    {
        /// <summary>Items in order.</summary>
        public readonly List<GuiItem> Items = new List<GuiItem>();
        /// <summary>Index of the selected item, -1 = none.</summary>
        public int Selected = -1;
        /// <summary>Fixed row height in reference pixels; 0 = from text and icon.</summary>
        public float RowHeight;
        /// <summary>Icon size in reference pixels; 0 = default.</summary>
        public Vector2 IconSize;
        /// <summary>Scroll position in screen pixels.</summary>
        protected float ScrollPos;
        /// <summary>Height of all rows in screen pixels.</summary>
        protected float ContentHeight;
        /// <summary>Index of the item under the mouse, -1 = none.</summary>
        protected int HoverIndex = -1;
        private bool _drag;
        private float _dragStart, _dragPos;

        /// <summary>Creates the item element of the given kind.</summary>
        protected GuiItemsBase(int kind) : base(kind)
        {
        }

        public override bool Interactive => true;

        public override int ItemCount => Items.Count;

        /// <summary>True when the element has a fixed height and scrolls itself.</summary>
        protected bool Scrolls => Size.Y > 0f || FillHeight;

        /// <summary>Width of the scroll bar in screen pixels.</summary>
        protected float BarWidth => 12f * K;

        /// <summary>Largest scroll position in screen pixels; 0 when the element does not scroll itself.</summary>
        protected float ScrollMax => Scrolls ? Math.Max(0f, ContentHeight - ContentRect.Height) : 0f;

        /// <summary>Row height of an item in screen pixels.</summary>
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

        /// <summary>Called after the items changed; invalidates the layout.</summary>
        protected virtual void ItemsChanged()
        {
            Invalidate();
        }

        /// <summary>Selects an item (-1 = none); with fire it raises GuiEvent.Select.</summary>
        public void Select(int index, bool fire)
        {
            if (index < -1 || index >= Items.Count)
                return;
            Selected = index;
            if (fire && index >= 0)
                Fire(GuiEvent.Select, index, Items[index].Key);
        }

        // ---------- scrolling ----------

        /// <summary>Keeps the scroll position within 0 and the maximum.</summary>
        protected void ClampScroll()
        {
            ScrollPos = MathHelper.Clamp(ScrollPos, 0f, ScrollMax);
        }

        /// <summary>Area for the items: the content area minus the scroll bar when one is shown.</summary>
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

        /// <summary>Draws the scroll bar thumb when the content is higher than the area.</summary>
        protected void DrawBar()
        {
            if (ScrollMax <= 0.5f)
                return;
            var t = Thumb;
            bool hover = _drag;
            GuiDraw.ThreeV(hover ? GuiStyle.ScrollTopHover : GuiStyle.ScrollTop, hover ? GuiStyle.ScrollCenterHover : GuiStyle.ScrollCenter,
                hover ? GuiStyle.ScrollBottomHover : GuiStyle.ScrollBottom, t, Color.White, 6f);
        }

        /// <summary>Starts dragging the bar when the point is on the track (a click beside the thumb jumps there); false if not on the bar.</summary>
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

        /// <summary>Mouse released on the items (not on the bar); lists and drop-downs select here.</summary>
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

        /// <summary>Index of the item under the point, -1 if none.</summary>
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

        /// <summary>Draws the icon layers of an item into the rectangle.</summary>
        protected void DrawIcons(string[] icons, RectangleF r, bool enabled)
        {
            if (icons == null)
                return;
            foreach (var m in icons)
                GuiDraw.TextureFit(m, r, enabled ? Color.White : GuiStyle.TextDisabled);
        }

        /// <summary>Draws the badge tag of a row at x, centred vertically in the row.</summary>
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
}
