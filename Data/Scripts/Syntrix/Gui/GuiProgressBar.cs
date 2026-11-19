// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Globalization;
using VRageMath;

namespace Syntrix.Gui
{
    /// <summary>Progress bar 0..1 with optional text in the middle.</summary>
    public class GuiProgressBar : GuiElement
    {
        /// <summary>Progress 0..1.</summary>
        public float Value;
        /// <summary>Text in the middle; null = none.</summary>
        public string Text;

        /// <summary>Creates a progress bar in the accent colour.</summary>
        public GuiProgressBar() : base(GuiKind.ProgressBar)
        {
            Color = GuiStyle.Accent;
        }

        public override Vector2 Measure(float width, float height)
        {
            return new Vector2(Math.Max(100f * K, width), 24f * K);
        }

        protected override void DrawBackground()
        {
            GuiDraw.Nine(GuiStyle.DarkPrefix, GuiStyle.DarkCenter, Rect, Color.White, 4f);
            float t = MathHelper.Clamp(Value, 0f, 1f);
            float pad = 3f * K;
            var inner = new RectangleF(Rect.X + pad, Rect.Y + pad, (Rect.Width - 2f * pad) * t, Rect.Height - 2f * pad);
            GuiDraw.Rect(inner, Color);
            if (BorderColor.A > 0)
                GuiDraw.Border(Rect, BorderColor, Math.Max(1f, BorderWidth * K));
        }

        protected override void DrawContent()
        {
            if (!string.IsNullOrEmpty(Text))
                GuiDraw.TextIn(EffectiveFont, Text, Rect, TextScale, TextColor, 1);
        }

        public override bool SetFloat(int prop, float v)
        {
            if (prop == GuiProp.Value) { Value = v; return true; }
            return base.SetFloat(prop, v);
        }

        public override bool GetFloat(int prop, out float v)
        {
            if (prop == GuiProp.Value) { v = Value; return true; }
            return base.GetFloat(prop, out v);
        }

        public override bool SetText(int prop, string v)
        {
            if (prop == GuiProp.Text) { Text = v; return true; }
            return base.SetText(prop, v);
        }
    }
}
