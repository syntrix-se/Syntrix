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
        /// <summary>Lay out the children side by side instead of one below the other.</summary>
        public bool Horizontal;
        /// <summary>Gap between the children in reference pixels.</summary>
        public float Spacing = 6f;

        /// <summary>Creates a stack.</summary>
        public GuiStack() : base(GuiKind.Stack)
        {
        }

        /// <summary>Creates a stack-based element of another kind.</summary>
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
}
