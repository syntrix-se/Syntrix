// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Globalization;
using VRageMath;

namespace Syntrix.Gui
{
    /// <summary>Lays out its children one below the other (or side by side with Horizontal).</summary>
    public class GuiStack : GuiElement
    {
        public bool Horizontal;
        public float Spacing = 6f;

        public GuiStack() : base(GuiKind.Stack)
        {
        }

        protected GuiStack(int kind) : base(kind)
        {
        }

        public override Vector2 Measure(float width, float height)
        {
            float pw = Padding.X * K * 2f, ph = Padding.Y * K * 2f;
            float inner = Math.Max(0f, (Horizontal ? height - ph : width - pw));
            float sum = 0f, max = 0f;
            int n = 0;
            // Horizontal: children with FillWidth share what the others leave of the available width
            float share = 0f;
            if (Horizontal)
            {
                float used = 0f;
                int fill = 0, count = 0;
                foreach (var c in Children)
                {
                    if (!c.Visible)
                        continue;
                    count++;
                    if (c.FillWidth)
                        fill++;
                    else
                        used += c.ResolveSize(0f, inner).X;
                }
                used += Math.Max(0, count - 1) * Spacing * K;
                share = fill > 0 ? Math.Max(0f, (width - pw - used) / fill) : 0f;
            }
            foreach (var c in Children)
            {
                if (!c.Visible)
                    continue;
                var s = Horizontal ? (c.FillWidth ? new Vector2(share, c.ResolveSize(share, inner).Y) : c.ResolveSize(0f, inner)) : c.ResolveSize(inner, 0f);
                sum += Horizontal ? s.X : s.Y;
                max = Math.Max(max, Horizontal ? s.Y : s.X);
                n++;
            }
            sum += Math.Max(0, n - 1) * Spacing * K;
            return Horizontal ? new Vector2(sum + pw, max + ph) : new Vector2(Math.Max(max, inner) + pw, sum + ph);
        }

        protected override void ArrangeChildren(RectangleF content, RectangleF clip)
        {
            float gap = Spacing * K;
            if (!Horizontal)
            {
                // Fixed and measured children first, the rest of the height goes to children with FillHeight
                float usedH = 0f;
                int fillH = 0, count = 0;
                foreach (var c in Children)
                {
                    if (!c.Visible)
                        continue;
                    count++;
                    if (c.FillHeight)
                        fillH++;
                    else
                        usedH += c.ResolveSize(content.Width, 0f).Y;
                }
                usedH += Math.Max(0, count - 1) * gap;
                float shareH = fillH > 0 ? Math.Max(0f, (content.Height - usedH) / fillH) : 0f;
                float y = content.Y;
                foreach (var c in Children)
                {
                    if (!c.Visible)
                        continue;
                    var s = c.FillHeight ? new Vector2(c.ResolveSize(content.Width, shareH).X, shareH) : c.ResolveSize(content.Width, 0f);
                    float w = c.FillWidth || c.Size.X <= 0f && !(c is GuiButton) ? content.Width : s.X;
                    int hx = c.Anchor % 3;
                    float x = content.X + hx * (content.Width - w) / 2f + c.Offset.X * K;
                    c.Arrange(new RectangleF(x, y + c.Offset.Y * K, w, s.Y), clip);
                    y += s.Y + gap;
                }
            }
            else
            {
                // Fixed and measured children first, the rest of the width goes to children with FillWidth
                float used = 0f;
                int fill = 0, n = 0;
                foreach (var c in Children)
                {
                    if (!c.Visible)
                        continue;
                    n++;
                    if (c.FillWidth)
                        fill++;
                    else
                        used += c.ResolveSize(0f, content.Height).X;
                }
                used += Math.Max(0, n - 1) * gap;
                float share = fill > 0 ? Math.Max(0f, (content.Width - used) / fill) : 0f;
                float x = content.X;
                foreach (var c in Children)
                {
                    if (!c.Visible)
                        continue;
                    var s = c.FillWidth ? new Vector2(share, c.ResolveSize(share, content.Height).Y) : c.ResolveSize(0f, content.Height);
                    float h = c.FillHeight ? content.Height : s.Y;
                    int vy = c.Anchor / 3;
                    float y = content.Y + vy * (content.Height - h) / 2f + c.Offset.Y * K;
                    c.Arrange(new RectangleF(x + c.Offset.X * K, y, s.X, h), clip);
                    x += s.X + gap;
                }
            }
        }

        public override bool SetBool(int prop, bool v)
        {
            if (prop == GuiProp.Horizontal)
            {
                Horizontal = v;
                Invalidate();
                return true;
            }
            return base.SetBool(prop, v);
        }

        public override bool GetBool(int prop, out bool v)
        {
            if (prop == GuiProp.Horizontal)
            {
                v = Horizontal;
                return true;
            }
            return base.GetBool(prop, out v);
        }

        public override bool SetFloat(int prop, float v)
        {
            if (prop == GuiProp.Spacing)
            {
                Spacing = Math.Max(0f, v);
                Invalidate();
                return true;
            }
            return base.SetFloat(prop, v);
        }

        public override bool GetFloat(int prop, out float v)
        {
            if (prop == GuiProp.Spacing)
            {
                v = Spacing;
                return true;
            }
            return base.GetFloat(prop, out v);
        }
    }

    /// <summary>Text with wrapping, markup and clickable links (event Link).</summary>
    public class GuiLabel : GuiElement
    {
        public string Text = "";
        public bool Wrap = true;
        public bool Markup = true;
        protected readonly GuiRichText Rich = new GuiRichText();

        public GuiLabel() : base(GuiKind.Label)
        {
        }

        protected GuiLabel(int kind) : base(kind)
        {
        }

        public override bool Interactive => Rich.HasLinks || Tooltip != null;

        protected void Prepare()
        {
            Rich.Set(Text, Markup, EffectiveFont, TextScale, IsEnabled ? TextColor : GuiStyle.TextDisabled, Wrap);
        }

        public override Vector2 Measure(float width, float height)
        {
            Prepare();
            float pw = Padding.X * K * 2f, ph = Padding.Y * K * 2f;
            if (Wrap && width > 0f)
            {
                Rich.Layout(width - pw);
                return new Vector2(width, Rich.Height + ph);
            }
            Rich.Layout(0f);
            return new Vector2(Rich.Width + pw, Rich.Height + ph);
        }

        public override void Arrange(RectangleF rect, RectangleF clip)
        {
            base.Arrange(rect, clip);
            Prepare();
            Rich.Layout(Wrap ? ContentRect.Width : 0f);
        }

        protected override void DrawContent()
        {
            var c = ContentRect;
            float y = c.Y;
            // Fixed height: centre the text vertically
            if (Size.Y > 0f || FillHeight)
                y += Math.Max(0f, (c.Height - Rich.Height) / 2f);
            TextTop = y;
            Rich.Draw(new Vector2(c.X, y), c.Width);
        }

        /// <summary>Top of the drawn text (for link hits).</summary>
        protected float TextTop;

        public override void MouseMove(Vector2 p)
        {
            Rich.HoverLink = Rich.LinkAt(new Vector2(ContentRect.X, TextTop), ContentRect.Width, p);
        }

        public override bool MouseDown(Vector2 p, int button)
        {
            return button == 0 && Rich.LinkAt(new Vector2(ContentRect.X, TextTop), ContentRect.Width, p) != null;
        }

        public override void MouseUp(Vector2 p, int button, bool inside)
        {
            if (!inside || button != 0)
                return;
            var link = Rich.LinkAt(new Vector2(ContentRect.X, TextTop), ContentRect.Width, p);
            if (link != null)
                Fire(GuiEvent.Link, 0, link);
        }

        public void ClearHover()
        {
            Rich.HoverLink = null;
        }

        public override void MouseLeave()
        {
            Rich.HoverLink = null;
        }

        public override bool SetText(int prop, string v)
        {
            if (prop == GuiProp.Text)
            {
                Text = v ?? "";
                Invalidate();
                return true;
            }
            return base.SetText(prop, v);
        }

        public override bool GetText(int prop, out string v)
        {
            if (prop == GuiProp.Text)
            {
                v = Text;
                return true;
            }
            return base.GetText(prop, out v);
        }

        public override bool SetBool(int prop, bool v)
        {
            if (prop == GuiProp.Wrap) { Wrap = v; Invalidate(); return true; }
            if (prop == GuiProp.Markup) { Markup = v; Invalidate(); return true; }
            return base.SetBool(prop, v);
        }

        public override bool GetBool(int prop, out bool v)
        {
            if (prop == GuiProp.Wrap) { v = Wrap; return true; }
            if (prop == GuiProp.Markup) { v = Markup; return true; }
            return base.GetBool(prop, out v);
        }

        public override bool GetFloat(int prop, out float v)
        {
            if (prop == GuiProp.TextHeight)
            {
                v = Rich.Height / K;
                return true;
            }
            return base.GetFloat(prop, out v);
        }
    }

    /// <summary>Vanilla button (styles: default, red, flat, link, toggle); optional icon left of the text.</summary>
    public class GuiButton : GuiElement
    {
        public string Text = "";
        public string Material;
        public bool Checked;
        public Vector2 IconSize;

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

    /// <summary>
    /// Image or icon: TransparentMaterial, GUI or LCD texture name, or definition id ("MyObjectBuilder_Ore/Iron");
    /// several materials separated by ';' are drawn on top of each other. Keeps the aspect ratio unless Style is 1.
    /// </summary>
    public class GuiImage : GuiElement
    {
        public string Material;
        private string[] _layers;

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
            var tint = Color.A > 0 && Style == GuiStyleId.Default ? Color.White : Color.White;
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

    /// <summary>Vanilla check box with text on the right.</summary>
    public class GuiCheckBox : GuiElement
    {
        public string Text = "";
        public bool Checked;

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

    /// <summary>Vanilla slider with value text on the right (Format, e.g. "0.0" or "0 %").</summary>
    public class GuiSlider : GuiElement
    {
        public float Value, Min, Max = 1f, Step;
        public string Format = "0.##";
        private bool _drag;

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

    /// <summary>Progress bar 0..1 with optional text in the middle.</summary>
    public class GuiProgressBar : GuiElement
    {
        public float Value;
        public string Text;

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

    /// <summary>
    /// Drawing area: the owner draws into it from a callback (screen pixels rect, alpha), clipped to the area.
    /// Mouse events go to the owner as GuiEvent.Mouse with coordinates relative to the area in reference pixels.
    /// </summary>
    public class GuiCanvas : GuiElement
    {
        /// <summary>Draw callback: (x, y, width, height in screen pixels), alpha.</summary>
        public Action<Vector4, float> DrawCallback;
        public bool ReportMoves;

        public GuiCanvas() : base(GuiKind.Canvas)
        {
            Clip = true;
        }

        public override bool Interactive => true;

        public override Vector2 Measure(float width, float height)
        {
            return new Vector2(width, Math.Max(100f * K, height));
        }

        protected override void DrawContent()
        {
            if (DrawCallback == null)
                return;
            using (GuiDraw.PushClip(Rect))
            {
                try
                {
                    DrawCallback(new Vector4(Rect.X, Rect.Y, Rect.Width, Rect.Height), GuiDraw.Alpha);
                }
                catch (Exception e)
                {
                    Log.Error("canvas draw callback of " + (Owner != null ? Owner.ModName : "Syntrix"), e);
                    DrawCallback = null;
                }
            }
        }

        private string Local(Vector2 p, string extra)
        {
            var l = (p - new Vector2(Rect.X, Rect.Y)) / K;
            return l.X.ToString("0.#", CultureInfo.InvariantCulture) + ";" + l.Y.ToString("0.#", CultureInfo.InvariantCulture) + ";" + extra;
        }

        public override bool MouseDown(Vector2 p, int button)
        {
            Fire(GuiEvent.Mouse, 0, Local(p, button.ToString(CultureInfo.InvariantCulture)));
            return true;
        }

        public override void MouseDrag(Vector2 p)
        {
            if (ReportMoves)
                Fire(GuiEvent.Mouse, 2, Local(p, "0"));
        }

        public override void MouseMove(Vector2 p)
        {
            if (ReportMoves)
                Fire(GuiEvent.Mouse, 2, Local(p, "-1"));
        }

        public override void MouseUp(Vector2 p, int button, bool inside)
        {
            Fire(GuiEvent.Mouse, 1, Local(p, button.ToString(CultureInfo.InvariantCulture)));
            if (inside)
                Fire(GuiEvent.Click, button);
        }

        public override bool Wheel(Vector2 p, int delta)
        {
            Fire(GuiEvent.Mouse, 3, Local(p, delta.ToString(CultureInfo.InvariantCulture)));
            return true;
        }

        public override bool SetBool(int prop, bool v)
        {
            if (prop == GuiProp.MouseMove) { ReportMoves = v; return true; }
            return base.SetBool(prop, v);
        }
    }
}
