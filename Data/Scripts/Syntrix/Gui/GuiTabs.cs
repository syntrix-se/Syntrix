// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using VRageMath;

namespace Syntrix.Gui
{
    /// <summary>Row of tabs (items); event Select when the player picks one.</summary>
    public class GuiTabs : GuiItemsBase
    {
        /// <summary>Creates a tab row with the first tab selected.</summary>
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
                GuiDraw.Texture(sel ? GuiStyle.TabSelected : hover ? GuiStyle.TabHover : GuiStyle.Tab, r,item.Enabled ? Color.White : new Color(160, 160, 160, 200));
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
}
