// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using VRageMath;

namespace Syntrix.Gui
{
    /// <summary>List with icons, second text (Extra), badge, selection; several columns with Columns &gt; 1 (tiles).</summary>
    public class GuiList : GuiItemsBase
    {
        /// <summary>Number of columns; above 1 the items are shown as tiles.</summary>
        public int Columns = 1;

        /// <summary>Creates a list with the list panel style.</summary>
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
}
