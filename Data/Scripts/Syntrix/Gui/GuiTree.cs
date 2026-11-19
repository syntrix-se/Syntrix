// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using VRageMath;

namespace Syntrix.Gui
{
    /// <summary>Tree: items with ParentKey below their parent, arrow to expand (event Expand), selection (event Select).</summary>
    public class GuiTree : GuiItemsBase
    {
        private readonly List<int> _order = new List<int>();
        private bool _orderValid;

        /// <summary>Creates a tree with the list panel style.</summary>
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
}
