// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using VRageMath;

namespace Syntrix.Gui
{
    /// <summary>Drop-down: shows the selected item, a click opens the list above everything else of the window.</summary>
    public class GuiDropdown : GuiItemsBase
    {
        /// <summary>The list is open.</summary>
        public bool Open;

        /// <summary>Creates a drop-down.</summary>
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

        /// <summary>True if the open list is under the point.</summary>
        public bool PopupContains(Vector2 p)
        {
            return Open && GuiDraw.Inside(Popup, p);
        }

        /// <summary>Highlights the list item under the point.</summary>
        public void PopupHover(Vector2 p)
        {
            HoverIndex = PopupIndexAt(p);
        }

        /// <summary>Scrolls the open list by a wheel step; false if closed.</summary>
        public bool PopupWheel(int delta)
        {
            if (!Open)
                return false;
            ScrollPos = MathHelper.Clamp(ScrollPos - Math.Sign(delta) * ItemH, 0f, Math.Max(0f, (Items.Count - 10) * ItemH));
            return true;
        }

        /// <summary>Closes the list and selects the enabled item under the point.</summary>
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
