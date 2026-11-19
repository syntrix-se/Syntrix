// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Globalization;
using VRageMath;

namespace Syntrix.Gui
{
    /// <summary>Vanilla check box with text on the right.</summary>
    public class GuiCheckBox : GuiElement
    {
        /// <summary>Text right of the box.</summary>
        public string Text = "";
        /// <summary>Box is ticked.</summary>
        public bool Checked;

        /// <summary>Creates a check box.</summary>
        public GuiCheckBox() : base(GuiKind.CheckBox)
        {
        }

        public override bool Interactive => true;

        public override Vector2 Measure(float width, float height)
        {
            float box = 30f * K;
            return new Vector2(box + 10f * K + GuiDraw.Width(EffectiveFont, GuiRichText.Plain(Text), TextScale), Math.Max(box, GuiDraw.LineHeight(EffectiveFont, TextScale)));
        }

        protected override void DrawContent()
        {
            bool hover = Hovered && IsEnabled;
            float box = 30f * K;
            var b = new RectangleF(Rect.X, Rect.Y + (Rect.Height - box) / 2f, box, box);
            string tex = Checked ? (hover ? GuiStyle.CheckOnHover : GuiStyle.CheckOn) : (hover ? GuiStyle.CheckOffHover : GuiStyle.CheckOff);
            GuiDraw.Texture(tex, b, IsEnabled ? Color.White : new Color(160, 160, 160, 200));
            var t = new RectangleF(b.X + box + 10f * K, Rect.Y, Math.Max(0f, Rect.Width - box - 10f * K), Rect.Height);
            GuiDraw.TextIn(hover ? GuiStyle.FontBright : EffectiveFont, GuiRichText.Plain(Text), t, TextScale, IsEnabled ? TextColor : GuiStyle.TextDisabled, 0);
        }

        public override bool MouseDown(Vector2 p, int button)
        {
            return IsEnabled && button == 0;
        }

        public override void MouseUp(Vector2 p, int button, bool inside)
        {
            if (!inside || !IsEnabled || button != 0)
                return;
            Checked = !Checked;
            Fire(GuiEvent.Changed, Checked ? 1 : 0, Checked ? "1" : "0");
        }

        public override bool SetText(int prop, string v)
        {
            if (prop == GuiProp.Text) { Text = v ?? ""; Invalidate(); return true; }
            return base.SetText(prop, v);
        }

        public override bool GetText(int prop, out string v)
        {
            if (prop == GuiProp.Text) { v = Text; return true; }
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
    }
}
