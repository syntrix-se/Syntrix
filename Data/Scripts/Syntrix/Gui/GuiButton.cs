// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Globalization;
using VRageMath;

namespace Syntrix.Gui
{
    /// <summary>Vanilla button (styles: default, red, flat, link, toggle); optional icon left of the text.</summary>
    public class GuiButton : GuiElement
    {
        /// <summary>Button text (markup stripped when measured and drawn).</summary>
        public string Text = "";
        /// <summary>Material of the icon drawn left of the text; null = no icon.</summary>
        public string Material;
        /// <summary>Pressed state of a toggle button (style Toggle).</summary>
        public bool Checked;
        /// <summary>Icon size in reference pixels; 0 = 24 x 24.</summary>
        public Vector2 IconSize;

        /// <summary>Creates a button, text centred.</summary>
        public GuiButton() : base(GuiKind.Button)
        {
            Align = 1;
        }

        public override bool Interactive => true;

        public override Vector2 Measure(float width, float height)
        {
            float w = GuiDraw.Width(EffectiveFont, GuiRichText.Plain(Text), TextScale);
            float icon = !string.IsNullOrEmpty(Material) ? (IconSize.X > 0f ? IconSize.X : 24f) * K + 8f * K : 0f;
            float h = Style == GuiStyleId.List ? GuiDraw.LineHeight(EffectiveFont, TextScale) + 4f * K : 40f * K;
            return new Vector2(Math.Max(w + icon + 36f * K, Style == GuiStyleId.List ? 0f : 120f * K), h);
        }

        protected override void DrawBackground()
        {
            bool enabled = IsEnabled;
            bool hover = Hovered && enabled;
            bool down = Pressed && hover || Style == GuiStyleId.Toggle && Checked;
            Color tint = enabled ? Color.White : new Color(180, 180, 180, 200);
            switch (Style)
            {
                case GuiStyleId.Dark:
                    GuiDraw.Texture(hover ? GuiStyle.ButtonRedHover : GuiStyle.ButtonRed, Rect, tint);
                    break;
                case GuiStyleId.Border:
                    GuiDraw.Texture(down ? GuiStyle.ButtonPressed : hover ? GuiStyle.ButtonFocus : GuiStyle.ButtonFlat, Rect, tint);
                    break;
                case GuiStyleId.List:
                    if (hover)
                        GuiDraw.Texture(GuiStyle.ItemHover, Rect, Color.White);
                    break;
                case GuiStyleId.None:
                    break;
                default:
                    GuiDraw.Texture(down ? GuiStyle.ButtonPressed : hover ? GuiStyle.ButtonHover : GuiStyle.Button, Rect, tint);
                    break;
            }
            if (Color.A > 0)
                GuiDraw.Rect(Rect, Color);
            if (BorderColor.A > 0)
                GuiDraw.Border(Rect, BorderColor, Math.Max(1f, BorderWidth * K));
        }

        protected override void DrawContent()
        {
            bool enabled = IsEnabled;
            bool hover = Hovered && enabled;
            var r = Rect;
            float pad = (Style == GuiStyleId.List ? 4f : 14f) * K;
            var inner = new RectangleF(r.X + pad, r.Y, r.Width - 2f * pad, r.Height);
            if (!string.IsNullOrEmpty(Material))
            {
                var s = IconSize.X > 0f ? IconSize * K : new Vector2(24f * K, 24f * K);
                float textW = string.IsNullOrEmpty(Text) ? 0f : GuiDraw.Width(EffectiveFont, GuiRichText.Plain(Text), TextScale) + 8f * K;
                float x = Align == 1 ? inner.X + (inner.Width - s.X - textW) / 2f : Align == 2 ? inner.X + inner.Width - s.X - textW : inner.X;
                GuiDraw.TextureFit(Material, new RectangleF(x, r.Y + (r.Height - s.Y) / 2f, s.X, s.Y), enabled ? Color.White : GuiStyle.TextDisabled);
                inner = new RectangleF(x + s.X + 8f * K, r.Y, Math.Max(0f, inner.X + inner.Width - x - s.X - 8f * K), r.Height);
                if (Align == 1)
                {
                    GuiDraw.TextIn(EffectiveFont, GuiRichText.Plain(Text), inner, TextScale, ButtonTextColor(enabled, hover), 0);
                    return;
                }
            }
            var font = hover && Style != GuiStyleId.List ? GuiStyle.FontBright : EffectiveFont;
            GuiDraw.TextIn(font, GuiRichText.Plain(Text), inner, TextScale, ButtonTextColor(enabled, hover), Align);
        }

        private Color ButtonTextColor(bool enabled, bool hover)
        {
            if (!enabled)
                return GuiStyle.TextDisabled;
            if (Style == GuiStyleId.List)
                return hover ? Color.White : TextColor == GuiStyle.Text ? GuiStyle.Link : TextColor;
            return TextColor;
        }

        public override bool MouseDown(Vector2 p, int button)
        {
            return IsEnabled;
        }

        public override void MouseUp(Vector2 p, int button, bool inside)
        {
            if (!inside || !IsEnabled)
                return;
            if (Style == GuiStyleId.Toggle && button == 0)
            {
                Checked = !Checked;
                Fire(GuiEvent.Changed, Checked ? 1 : 0, Checked ? "1" : "0");
            }
            Fire(GuiEvent.Click, button);
        }

        public override bool SetText(int prop, string v)
        {
            if (prop == GuiProp.Text) { Text = v ?? ""; Invalidate(); return true; }
            if (prop == GuiProp.Material) { Material = string.IsNullOrEmpty(v) ? null : v; Invalidate(); return true; }
            return base.SetText(prop, v);
        }

        public override bool GetText(int prop, out string v)
        {
            if (prop == GuiProp.Text) { v = Text; return true; }
            if (prop == GuiProp.Material) { v = Material; return true; }
            return base.GetText(prop, out v);
        }

        public override bool SetBool(int prop, bool v)
        {
            if (prop == GuiProp.Checked) { Checked = v; return true; }
            return base.SetBool(prop, v);
        }

        public override bool GetBool(int prop, out bool v)
        {
            if (prop == GuiProp.Checked) { v = Checked; return true; }
            return base.GetBool(prop, out v);
        }

        public override bool SetVector(int prop, Vector2 v)
        {
            if (prop == GuiProp.IconSize) { IconSize = v; Invalidate(); return true; }
            return base.SetVector(prop, v);
        }

        public override bool GetVector(int prop, out Vector2 v)
        {
            if (prop == GuiProp.IconSize) { v = IconSize; return true; }
            return base.GetVector(prop, out v);
        }
    }
}
