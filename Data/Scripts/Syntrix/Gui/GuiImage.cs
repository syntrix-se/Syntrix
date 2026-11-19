// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Globalization;
using VRageMath;

namespace Syntrix.Gui
{
    /// <summary>
    /// Image or icon: TransparentMaterial, GUI or LCD texture name, or definition id ("MyObjectBuilder_Ore/Iron");
    /// several materials separated by ';' are drawn on top of each other. Keeps the aspect ratio unless Style is 1.
    /// </summary>
    public class GuiImage : GuiElement
    {
        /// <summary>Material or definition id; several separated by ';' are drawn on top of each other.</summary>
        public string Material;
        private string[] _layers;

        /// <summary>Creates an image.</summary>
        public GuiImage() : base(GuiKind.Image)
        {
        }

        public override bool Interactive => Tooltip != null;

        public override Vector2 Measure(float width, float height)
        {
            return new Vector2(64f * K, 64f * K);
        }

        protected override void DrawContent()
        {
            if (_layers == null)
                return;
            var tint = Color.White;
            foreach (var m in _layers)
            {
                if (Style == 1)
                    GuiDraw.Texture(m, Rect, TextColor == GuiStyle.Text ? tint : TextColor);
                else
                    GuiDraw.TextureFit(m, Rect, TextColor == GuiStyle.Text ? tint : TextColor);
            }
        }

        protected override void DrawBackground()
        {
            if (BorderColor.A > 0)
                GuiDraw.Border(Rect, BorderColor, Math.Max(1f, BorderWidth * K));
        }

        public override bool SetText(int prop, string v)
        {
            if (prop == GuiProp.Material)
            {
                Material = v;
                _layers = string.IsNullOrEmpty(v) ? null : v.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                return true;
            }
            return base.SetText(prop, v);
        }

        public override bool GetText(int prop, out string v)
        {
            if (prop == GuiProp.Material) { v = Material; return true; }
            return base.GetText(prop, out v);
        }
    }
}
