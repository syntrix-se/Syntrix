// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using VRageMath;

namespace Syntrix.Gui
{
    /// <summary>
    /// Scroll area: children one below the other, clipped, Vanilla scroll bar on the right when the content is higher
    /// than the area. Mouse wheel and dragging the bar. Clicks on clipped children never arrive (hit test uses the clip).
    /// </summary>
    public class GuiScroll : GuiStack
    {
        public float Position;
        public bool ToEnd;
        private float _content;
        private float _view;
        private bool _drag;
        private float _dragStart, _dragPosition;
        private bool _barHover;

        public GuiScroll() : base(GuiKind.Scroll)
        {
            Clip = true;
            Spacing = 6f;
            Style = GuiStyleId.List;
        }

        private float BarWidth => 12f * K;

        public float ScrollMax => Math.Max(0f, _content - _view);

        public override Vector2 Measure(float width, float height)
        {
            return new Vector2(width, Math.Max(height, 60f * K));
        }

        public override void Arrange(RectangleF rect, RectangleF clip)
        {
            Rect = rect;
            ClipArea = clip;
            var content = ContentRect;
            float bar = BarWidth + 4f * K;
            var inner = new RectangleF(content.X, content.Y, Math.Max(0f, content.Width - bar), content.Height);
            // Height of the content
            float sum = 0f;
            int n = 0;
            foreach (var c in Children)
            {
                if (!c.Visible)
                    continue;
                sum += c.ResolveSize(inner.Width, 0f).Y;
                n++;
            }
            sum += Math.Max(0, n - 1) * Spacing * K;
            bool wasAtEnd = Position >= ScrollMax - 1f;
            _content = sum;
            _view = content.Height;
            if (ToEnd && wasAtEnd)
                Position = ScrollMax;
            Position = MathHelper.Clamp(Position, 0f, ScrollMax);
            var shifted = new RectangleF(inner.X, inner.Y - Position, inner.Width, Math.Max(inner.Height, sum));
            ArrangeChildren(shifted, GuiDraw.Intersect(clip, content));
        }

        private RectangleF Track
        {
            get
            {
                var c = ContentRect;
                return new RectangleF(c.X + c.Width - BarWidth, c.Y, BarWidth, c.Height);
            }
        }

        private RectangleF Thumb
        {
            get
            {
                var t = Track;
                if (_content <= _view || _content <= 0f)
                    return new RectangleF(t.X, t.Y, t.Width, t.Height);
                float h = Math.Max(30f * K, t.Height * _view / _content);
                float y = t.Y + (t.Height - h) * (Position / Math.Max(1f, ScrollMax));
                return new RectangleF(t.X, y, t.Width, h);
            }
        }

        protected override void DrawOver()
        {
            if (ScrollMax <= 0.5f)
                return;
            var t = Thumb;
            bool hover = _barHover || _drag;
            GuiDraw.ThreeV(hover ? GuiStyle.ScrollTopHover : GuiStyle.ScrollTop, hover ? GuiStyle.ScrollCenterHover : GuiStyle.ScrollCenter,
                hover ? GuiStyle.ScrollBottomHover : GuiStyle.ScrollBottom, t, Color.White, 6f);
        }

        public override GuiElement Hit(Vector2 p)
        {
            if (!Visible || !GuiDraw.Inside(ClipArea, p))
                return null;
            _barHover = ScrollMax > 0.5f && GuiDraw.Inside(Track, p);
            if (_barHover)
                return this;
            return base.Hit(p);
        }

        public override bool Interactive => ScrollMax > 0.5f;

        public override bool MouseDown(Vector2 p, int button)
        {
            if (button != 0 || ScrollMax <= 0.5f || !GuiDraw.Inside(Track, p))
                return false;
            var thumb = Thumb;
            if (!GuiDraw.Inside(thumb, p))
            {
                // Click on the track: jump there
                float t = (p.Y - Track.Y - thumb.Height / 2f) / Math.Max(1f, Track.Height - thumb.Height);
                Position = MathHelper.Clamp(t, 0f, 1f) * ScrollMax;
                Invalidate();
            }
            _drag = true;
            _dragStart = p.Y;
            _dragPosition = Position;
            return true;
        }

        public override void MouseDrag(Vector2 p)
        {
            if (!_drag)
                return;
            var thumb = Thumb;
            float range = Math.Max(1f, Track.Height - thumb.Height);
            Position = MathHelper.Clamp(_dragPosition + (p.Y - _dragStart) / range * ScrollMax, 0f, ScrollMax);
            Invalidate();
        }

        public override void MouseUp(Vector2 p, int button, bool inside)
        {
            _drag = false;
        }

        public override bool Wheel(Vector2 p, int delta)
        {
            if (ScrollMax <= 0.5f)
                return false;
            float before = Position;
            Position = MathHelper.Clamp(Position - Math.Sign(delta) * 80f * K, 0f, ScrollMax);
            if (Position != before)
                Invalidate();
            return true;
        }

        /// <summary>Scrolls so that the element is visible.</summary>
        public void ScrollTo(GuiElement e)
        {
            if (e == null)
                return;
            float top = e.Rect.Y - ContentRect.Y + Position;
            if (top < Position)
                Position = top;
            else if (top + e.Rect.Height > Position + _view)
                Position = top + e.Rect.Height - _view;
            Position = MathHelper.Clamp(Position, 0f, ScrollMax);
            Invalidate();
        }

        public override bool SetFloat(int prop, float v)
        {
            if (prop == GuiProp.ScrollPosition)
            {
                Position = Math.Max(0f, v * K);
                Invalidate();
                return true;
            }
            return base.SetFloat(prop, v);
        }

        public override bool GetFloat(int prop, out float v)
        {
            if (prop == GuiProp.ScrollPosition) { v = Position / K; return true; }
            if (prop == GuiProp.ScrollMax) { v = ScrollMax / K; return true; }
            return base.GetFloat(prop, out v);
        }

        public override bool SetBool(int prop, bool v)
        {
            if (prop == GuiProp.ScrollToEnd) { ToEnd = v; return true; }
            return base.SetBool(prop, v);
        }

        public override bool GetVector(int prop, out Vector2 v)
        {
            if (prop == GuiProp.ContentSize)
            {
                v = new Vector2(ContentRect.Width, _content) / K;
                return true;
            }
            return base.GetVector(prop, out v);
        }
    }
}
