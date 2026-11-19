// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using VRageMath;

namespace Syntrix.Gui
{
    /// <summary>
    /// Table: header (GuiProp.Header, cells separated by \u001f), rows as items (cells separated by \u001f), relative
    /// column widths (GuiProp.Widths "2|1|3", else by text length), wrapping and markup in cells, links (event Link),
    /// click on a row (event Select). Grows with its rows (put it into a Scroll for long tables).
    /// </summary>
    public class GuiTable : GuiItemsBase
    {
        /// <summary>Separator of the cells in the header and in row texts.</summary>
        public const char CellSeparator = '\u001f';
        private string[] _header;
        private GuiRichText[] _headerRich;
        private float[] _widths;
        private float[] _columnWidths;
        private float _headerHeight;
        private float _laidWidth = -1f;
        private string _hoverLink;

        /// <summary>Creates a table.</summary>
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
}
