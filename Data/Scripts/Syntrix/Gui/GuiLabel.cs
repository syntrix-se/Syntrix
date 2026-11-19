// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Globalization;
using VRageMath;

namespace Syntrix.Gui
{
    /// <summary>Text with wrapping, markup and clickable links (event Link).</summary>
    public class GuiLabel : GuiElement
    {
        /// <summary>Text, with markup if Markup is set.</summary>
        public string Text = "";
        /// <summary>Wrap lines at the width of the element.</summary>
        public bool Wrap = true;
        /// <summary>Interpret markup tags and links in Text.</summary>
        public bool Markup = true;
        /// <summary>Laid out text.</summary>
        protected readonly GuiRichText Rich = new GuiRichText();

        /// <summary>Creates a label.</summary>
        public GuiLabel() : base(GuiKind.Label)
        {
        }

        /// <summary>Creates a label-based element of another kind.</summary>
        protected GuiLabel(int kind) : base(kind)
        {
        }

        public override bool Interactive => Rich.HasLinks || Tooltip != null;

        /// <summary>Hands text and base style to the rich text.</summary>
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

        /// <summary>Removes the link highlight.</summary>
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
}
