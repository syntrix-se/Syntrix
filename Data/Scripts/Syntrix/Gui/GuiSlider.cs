// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Globalization;
using VRageMath;

namespace Syntrix.Gui
{
    /// <summary>Vanilla slider with value text on the right (Format, e.g. "0.0" or "0 %").</summary>
    public class GuiSlider : GuiElement
    {
        /// <summary>Current value, range and step size; Step 0 = no snapping.</summary>
        public float Value, Min, Max = 1f, Step;
        /// <summary>Number format of the value shown (.NET custom format).</summary>
        public string Format = "0.##";
        private bool _drag;

        /// <summary>Creates a slider with the range 0..1.</summary>
        public GuiSlider() : base(GuiKind.Slider)
        {
        }

        public override bool Interactive => true;

        public override Vector2 Measure(float width, float height)
        {
            return new Vector2(Math.Max(200f * K, width), 36f * K);
        }

        private float ValueWidth => 80f * K;

        private RectangleF Rail => new RectangleF(Rect.X, Rect.Y + (Rect.Height - 26f * K) / 2f, Math.Max(10f, Rect.Width - ValueWidth - 8f * K), 26f * K);

        protected override void DrawContent()
        {
            bool hover = (Hovered || _drag) && IsEnabled;
            var rail = Rail;
            GuiDraw.Three(hover ? GuiStyle.RailLeftHover : GuiStyle.RailLeft, hover ? GuiStyle.RailCenterHover : GuiStyle.RailCenter,
                hover ? GuiStyle.RailRightHover : GuiStyle.RailRight, rail, IsEnabled ? Color.White : new Color(160, 160, 160, 200), 11f);
            float t = Max > Min ? (Value - Min) / (Max - Min) : 0f;
            float thumb = 26f * K;
            float x = rail.X + thumb / 2f + MathHelper.Clamp(t, 0f, 1f) * (rail.Width - thumb);
            GuiDraw.Texture(hover ? GuiStyle.ThumbHover : GuiStyle.Thumb, new RectangleF(x - thumb / 2f, rail.Y, thumb, thumb), Color.White);
            string text;
            try
            {
                text = Value.ToString(Format ?? "0.##", CultureInfo.CurrentCulture);
            }
            catch (FormatException)
            {
                text = Value.ToString("0.##", CultureInfo.CurrentCulture);
            }
            GuiDraw.TextIn(EffectiveFont, text, new RectangleF(Rect.X + Rect.Width - ValueWidth, Rect.Y, ValueWidth, Rect.Height), TextScale, IsEnabled ? TextColor : GuiStyle.TextDisabled, 2);
        }

        public override bool MouseDown(Vector2 p, int button)
        {
            if (!IsEnabled || button != 0)
                return false;
            _drag = true;
            MouseDrag(p);
            return true;
        }

        public override void MouseDrag(Vector2 p)
        {
            if (!_drag)
                return;
            var rail = Rail;
            float thumb = 26f * K;
            float t = MathHelper.Clamp((p.X - rail.X - thumb / 2f) / Math.Max(1f, rail.Width - thumb), 0f, 1f);
            SetValue(Min + t * (Max - Min), true);
        }

        public override void MouseUp(Vector2 p, int button, bool inside)
        {
            _drag = false;
        }

        public override bool Wheel(Vector2 p, int delta)
        {
            if (!IsEnabled)
                return false;
            float step = Step > 0f ? Step : (Max - Min) / 20f;
            SetValue(Value + Math.Sign(delta) * step, true);
            return true;
        }

        private void SetValue(float v, bool fire)
        {
            if (Step > 0f)
                v = Min + (float)Math.Round((v - Min) / Step) * Step;
            v = MathHelper.Clamp(v, Math.Min(Min, Max), Math.Max(Min, Max));
            if (v == Value)
                return;
            Value = v;
            if (fire)
                Fire(GuiEvent.Changed, 0, Value.ToString("R", CultureInfo.InvariantCulture));
        }

        public override bool SetFloat(int prop, float v)
        {
            switch (prop)
            {
                case GuiProp.Value: SetValue(v, false); return true;
                case GuiProp.Min: Min = v; SetValue(Value, false); return true;
                case GuiProp.Max: Max = v; SetValue(Value, false); return true;
                case GuiProp.Step: Step = Math.Max(0f, v); return true;
            }
            return base.SetFloat(prop, v);
        }

        public override bool GetFloat(int prop, out float v)
        {
            switch (prop)
            {
                case GuiProp.Value: v = Value; return true;
                case GuiProp.Min: v = Min; return true;
                case GuiProp.Max: v = Max; return true;
                case GuiProp.Step: v = Step; return true;
            }
            return base.GetFloat(prop, out v);
        }

        public override bool SetText(int prop, string v)
        {
            if (prop == GuiProp.Format) { Format = string.IsNullOrEmpty(v) ? "0.##" : v; return true; }
            return base.SetText(prop, v);
        }
    }
}
