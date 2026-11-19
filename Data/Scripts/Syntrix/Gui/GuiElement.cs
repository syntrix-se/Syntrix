// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using VRageMath;

namespace Syntrix.Gui
{
    /// <summary>
    /// Base of all elements: tree, layout (anchor, offset, size, fill, padding), background, border, tooltip,
    /// hit testing and events. Positions after layout are screen pixels (<see cref="Rect"/>).
    /// </summary>
    public class GuiElement
    {
        /// <summary>Kind number (GuiKind).</summary>
        public readonly int Kind;

        /// <summary>Handle of the owning mod (0 for Syntrix' own elements).</summary>
        public int Handle;

        /// <summary>Mod that made the element; null for Syntrix' own elements.</summary>
        public GuiClient Owner;

        /// <summary>Parent element.</summary>
        public GuiElement Parent;

        /// <summary>Children in drawing order.</summary>
        public readonly List<GuiElement> Children = new List<GuiElement>();

        /// <summary>Handler for Syntrix' own elements: (element, event, index, text).</summary>
        public Action<GuiElement, int, int, string> Handler;

        // ---------- properties ----------

        public bool Visible = true;
        public bool Enabled = true;
        public bool FillWidth;
        public bool FillHeight;
        public bool Clip;
        public Vector2 Offset;
        public Vector2 Size;
        public Vector2 Padding;
        public int Anchor;
        public Color Color = Color.Transparent;
        public Color BorderColor = Color.Transparent;
        public float BorderWidth = 1f;
        public string Tooltip;
        public string Font;
        public Color TextColor = GuiStyle.Text;
        public float TextScale = GuiStyle.TextScale;
        public int Align;
        public int Style;

        // ---------- state after layout ----------

        /// <summary>Screen rectangle after layout.</summary>
        public RectangleF Rect;

        /// <summary>Visible part (clip of the parents), screen pixels.</summary>
        public RectangleF ClipArea;

        /// <summary>Mouse is over the element (set by the window each frame).</summary>
        public bool Hovered;

        /// <summary>Mouse button is held on the element.</summary>
        public bool Pressed;

        public GuiElement(int kind)
        {
            Kind = kind;
        }

        /// <summary>Screen pixels per reference pixel.</summary>
        protected static float K => GuiDraw.K;

        /// <summary>Font of this element, inherited from the parents, Vanilla "Blue" at the top.</summary>
        public string EffectiveFont
        {
            get
            {
                for (var e = this; e != null; e = e.Parent)
                    if (!string.IsNullOrEmpty(e.Font))
                        return e.Font;
                return GuiStyle.Font;
            }
        }

        /// <summary>Enabled including all parents.</summary>
        public bool IsEnabled
        {
            get
            {
                for (var e = this; e != null; e = e.Parent)
                    if (!e.Enabled)
                        return false;
                return true;
            }
        }

        /// <summary>The window or overlay at the top of the tree.</summary>
        public GuiElement Root
        {
            get
            {
                var e = this;
                while (e.Parent != null)
                    e = e.Parent;
                return e;
            }
        }

        /// <summary>Takes clicks (otherwise the parent gets them).</summary>
        public virtual bool Interactive => false;

        /// <summary>Takes keyboard input when focused.</summary>
        public virtual bool Focusable => false;

        // ---------- tree ----------

        public void Add(GuiElement child)
        {
            if (child == null || child == this)
                return;
            if (child.Parent != null)
                child.Parent.Children.Remove(child);
            child.Parent = this;
            Children.Add(child);
            Invalidate();
        }

        public void Remove(GuiElement child)
        {
            if (child != null && Children.Remove(child))
            {
                child.Parent = null;
                Invalidate();
            }
        }

        /// <summary>Marks the layout of the tree as outdated.</summary>
        public virtual void Invalidate()
        {
            if (Parent != null)
                Parent.Invalidate();
        }

        // ---------- layout ----------

        /// <summary>Content rectangle (inside the padding).</summary>
        public RectangleF ContentRect
        {
            get
            {
                float px = Padding.X * K, py = Padding.Y * K;
                return new RectangleF(Rect.X + px, Rect.Y + py, Math.Max(0f, Rect.Width - 2f * px), Math.Max(0f, Rect.Height - 2f * py));
            }
        }

        /// <summary>Wanted size in screen pixels for the given available space (used for automatic sizes).</summary>
        public virtual Vector2 Measure(float width, float height)
        {
            // Containers: big enough for their children
            float pw = Padding.X * K * 2f, ph = Padding.Y * K * 2f;
            float w = 0f, h = 0f;
            foreach (var c in Children)
            {
                if (!c.Visible)
                    continue;
                var s = c.ResolveSize(Math.Max(0f, width - pw), Math.Max(0f, height - ph));
                w = Math.Max(w, s.X + Math.Abs(c.Offset.X) * K);
                h = Math.Max(h, s.Y + Math.Abs(c.Offset.Y) * K);
            }
            return new Vector2(w + pw, h + ph);
        }

        /// <summary>Final size inside an area of the given size: fill, fixed or measured.</summary>
        public Vector2 ResolveSize(float width, float height)
        {
            float w = FillWidth ? width : Size.X > 0f ? Size.X * K : -1f;
            float h = FillHeight ? height : Size.Y > 0f ? Size.Y * K : -1f;
            if (w < 0f || h < 0f)
            {
                var m = Measure(w < 0f ? width : w, h < 0f ? height : h);
                if (w < 0f)
                    w = Math.Min(m.X, Math.Max(width, m.X));
                if (h < 0f)
                    h = m.Y;
            }
            return new Vector2(w, h);
        }

        /// <summary>Places the element inside an area by anchor and offset.</summary>
        public void Place(RectangleF area, RectangleF clip)
        {
            var s = ResolveSize(area.Width, area.Height);
            int hx = Anchor % 3, vy = Anchor / 3;
            float x = area.X + hx * (area.Width - s.X) / 2f + Offset.X * K;
            float y = area.Y + vy * (area.Height - s.Y) / 2f + Offset.Y * K;
            Arrange(new RectangleF(x, y, s.X, s.Y), clip);
        }

        /// <summary>Takes the final rectangle and lays out the children.</summary>
        public virtual void Arrange(RectangleF rect, RectangleF clip)
        {
            Rect = rect;
            ClipArea = clip;
            ArrangeChildren(ContentRect, Clip ? GuiDraw.Intersect(clip, ContentRect) : clip);
        }

        /// <summary>Default: every child placed freely by its anchor inside the content rectangle.</summary>
        protected virtual void ArrangeChildren(RectangleF content, RectangleF clip)
        {
            foreach (var c in Children)
                if (c.Visible)
                    c.Place(content, clip);
        }

        // ---------- drawing ----------

        /// <summary>Draws the element and its children.</summary>
        public void DrawTree()
        {
            if (!Visible || Rect.Width <= 0f && Rect.Height <= 0f && Children.Count == 0)
                return;
            if (ClipArea.Width <= 0f || ClipArea.Height <= 0f)
                return;
            DrawBackground();
            DrawContent();
            if (Clip)
            {
                using (GuiDraw.PushClip(ContentRect))
                    DrawChildren();
            }
            else
                DrawChildren();
            DrawOver();
        }

        protected virtual void DrawChildren()
        {
            foreach (var c in Children)
                c.DrawTree();
        }

        /// <summary>Background colour or Vanilla panel and border.</summary>
        protected virtual void DrawBackground()
        {
            if (Style == GuiStyleId.Dark || Style == GuiStyleId.Border || Style == GuiStyleId.List)
                GuiStyle.Panel(Style, Rect, Color.A > 0 ? Color : Color.White);
            else
                GuiDraw.Rect(Rect, Color);
            if (BorderColor.A > 0)
                GuiDraw.Border(Rect, BorderColor, Math.Max(1f, BorderWidth * K));
        }

        /// <summary>Own content (text, image …), drawn before the children.</summary>
        protected virtual void DrawContent()
        {
        }

        /// <summary>Drawn after the children (scroll bar, drop-down list …).</summary>
        protected virtual void DrawOver()
        {
        }

        // ---------- input ----------

        /// <summary>Deepest visible element under the point (clip respected), null if none.</summary>
        public virtual GuiElement Hit(Vector2 p)
        {
            if (!Visible || !GuiDraw.Inside(ClipArea, p))
                return null;
            for (int i = Children.Count - 1; i >= 0; i--)
            {
                var h = Children[i].Hit(p);
                if (h != null)
                    return h;
            }
            return GuiDraw.Inside(Rect, p) ? this : null;
        }

        /// <summary>Mouse button pressed on the element (0 left, 1 right). True = takes the mouse until release.</summary>
        public virtual bool MouseDown(Vector2 p, int button)
        {
            return false;
        }

        /// <summary>Mouse moved while pressed.</summary>
        public virtual void MouseDrag(Vector2 p)
        {
        }

        /// <summary>Mouse button released; inside = still over the element.</summary>
        public virtual void MouseUp(Vector2 p, int button, bool inside)
        {
        }

        /// <summary>Mouse wheel over the element; true = used.</summary>
        public virtual bool Wheel(Vector2 p, int delta)
        {
            return false;
        }

        /// <summary>Double click (left).</summary>
        public virtual void DoubleClick(Vector2 p)
        {
        }

        /// <summary>Mouse moved over the element (no button).</summary>
        public virtual void MouseMove(Vector2 p)
        {
        }

        /// <summary>Mouse left the element.</summary>
        public virtual void MouseLeave()
        {
        }

        /// <summary>Focused: one frame of keyboard input.</summary>
        public virtual void Keyboard()
        {
        }

        /// <summary>Focus lost.</summary>
        public virtual void Blur()
        {
        }

        /// <summary>Text for the tooltip at a point (lists override it per item).</summary>
        public virtual string TooltipAt(Vector2 p)
        {
            return Tooltip;
        }

        // ---------- events ----------

        /// <summary>Raises an event to the owner (mod or Syntrix handler). Queued: handlers run after the input pass,
        /// so they may freely rebuild the tree.</summary>
        public void Fire(int ev, int index = 0, string text = null)
        {
            GuiService.Post(this, ev, index, text);
        }

        /// <summary>Delivers a queued event (called by the service).</summary>
        public void Deliver(int ev, int index, string text)
        {
            try
            {
                if (Handler != null)
                    Handler(this, ev, index, text);
            }
            catch (Exception e)
            {
                Log.Error("GUI handler", e);
            }
            if (Owner != null)
                Owner.Dispatch(Handle, ev, index, text);
        }

        // ---------- properties by number ----------

        public virtual bool SetBool(int prop, bool v)
        {
            switch (prop)
            {
                case GuiProp.Visible: Visible = v; break;
                case GuiProp.Enabled: Enabled = v; break;
                case GuiProp.FillWidth: FillWidth = v; break;
                case GuiProp.FillHeight: FillHeight = v; break;
                case GuiProp.Clip: Clip = v; break;
                default: return false;
            }
            Invalidate();
            return true;
        }

        public virtual bool GetBool(int prop, out bool v)
        {
            switch (prop)
            {
                case GuiProp.Visible: v = Visible; return true;
                case GuiProp.Enabled: v = Enabled; return true;
                case GuiProp.FillWidth: v = FillWidth; return true;
                case GuiProp.FillHeight: v = FillHeight; return true;
                case GuiProp.Clip: v = Clip; return true;
                case GuiProp.Hover: v = Hovered; return true;
                default: v = false; return false;
            }
        }

        public virtual bool SetInt(int prop, int v)
        {
            switch (prop)
            {
                case GuiProp.Anchor: Anchor = MathHelper.Clamp(v, 0, 8); break;
                case GuiProp.Align: Align = MathHelper.Clamp(v, 0, 2); break;
                case GuiProp.Style: Style = v; break;
                default: return false;
            }
            Invalidate();
            return true;
        }

        public virtual bool GetInt(int prop, out int v)
        {
            switch (prop)
            {
                case GuiProp.Anchor: v = Anchor; return true;
                case GuiProp.Align: v = Align; return true;
                case GuiProp.Style: v = Style; return true;
                default: v = 0; return false;
            }
        }

        public virtual bool SetFloat(int prop, float v)
        {
            switch (prop)
            {
                case GuiProp.BorderWidth: BorderWidth = v; break;
                case GuiProp.TextScale: TextScale = v > 0f ? v : GuiStyle.TextScale; break;
                default: return false;
            }
            Invalidate();
            return true;
        }

        public virtual bool GetFloat(int prop, out float v)
        {
            switch (prop)
            {
                case GuiProp.BorderWidth: v = BorderWidth; return true;
                case GuiProp.TextScale: v = TextScale; return true;
                default: v = 0f; return false;
            }
        }

        public virtual bool SetVector(int prop, Vector2 v)
        {
            switch (prop)
            {
                case GuiProp.Offset: Offset = v; break;
                case GuiProp.Size: Size = v; break;
                case GuiProp.Padding: Padding = v; break;
                default: return false;
            }
            Invalidate();
            return true;
        }

        public virtual bool GetVector(int prop, out Vector2 v)
        {
            switch (prop)
            {
                case GuiProp.Offset: v = Offset; return true;
                case GuiProp.Size: v = Size; return true;
                case GuiProp.Padding: v = Padding; return true;
                case GuiProp.ScreenPosition: v = new Vector2(Rect.X, Rect.Y) / K; return true;
                case GuiProp.ScreenSize: v = new Vector2(Rect.Width, Rect.Height) / K; return true;
                default: v = Vector2.Zero; return false;
            }
        }

        public virtual bool SetColor(int prop, Color v)
        {
            switch (prop)
            {
                case GuiProp.Color: Color = v; return true;
                case GuiProp.BorderColor: BorderColor = v; return true;
                case GuiProp.TextColor: TextColor = v; Invalidate(); return true;
                default: return false;
            }
        }

        public virtual bool GetColor(int prop, out Color v)
        {
            switch (prop)
            {
                case GuiProp.Color: v = Color; return true;
                case GuiProp.BorderColor: v = BorderColor; return true;
                case GuiProp.TextColor: v = TextColor; return true;
                default: v = Color.Transparent; return false;
            }
        }

        public virtual bool SetText(int prop, string v)
        {
            switch (prop)
            {
                case GuiProp.Tooltip: Tooltip = string.IsNullOrEmpty(v) ? null : v; return true;
                case GuiProp.Font: Font = string.IsNullOrEmpty(v) ? null : v; Invalidate(); return true;
                default: return false;
            }
        }

        public virtual bool GetText(int prop, out string v)
        {
            switch (prop)
            {
                case GuiProp.Tooltip: v = Tooltip; return true;
                case GuiProp.Font: v = Font; return true;
                default: v = null; return false;
            }
        }

        // ---------- items (lists, trees, tabs …) ----------

        public virtual int ItemAdd(string key, string parentKey, string text, string icons) { return -1; }
        public virtual void ItemSet(int index, int prop, string value) { }
        public virtual void ItemRemove(int index) { }
        public virtual void ItemClear() { }
        public virtual int ItemCount => 0;
        public virtual int ItemFind(string key) { return -1; }

        /// <summary>Removed from its client: release resources.</summary>
        public virtual void Destroyed()
        {
        }
    }
}
