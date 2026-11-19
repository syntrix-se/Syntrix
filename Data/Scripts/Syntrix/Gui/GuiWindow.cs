// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using Sandbox.ModAPI;
using VRage.Input;
using VRageMath;

namespace Syntrix.Gui
{
    /// <summary>
    /// Window in the Vanilla style: a MyModScreen of the game (background, close button, Esc, mouse cursor, fade)
    /// holding a tree of elements. Title centred at the top with a line below, content below the title.
    /// Size and position are normalized GUI coordinates (Vanilla terminal size 1.0157 x 0.9172).
    /// </summary>
    public class GuiWindow : GuiElement
    {
        public string Title;
        public bool Pause;
        public bool EscapeCloses = true;
        public bool CloseButton = true;
        public bool BackgroundFade = true;
        public Vector2 WindowSize = GuiStyle.WindowSize;
        public Vector2 Position = new Vector2(0.5f, 0.5f);

        /// <summary>Element with keyboard focus (text field), null if none.</summary>
        public GuiElement Focus;

        /// <summary>Open drop-down list, drawn above everything and served first.</summary>
        public GuiDropdown Popup;

        private Screen _screen;
        private bool _layoutValid;
        private Vector2 _laidFor;
        private GuiElement _hover;
        private GuiElement _pressed;
        private int _pressedButton;
        private int _frame;
        private int _lastClickFrame = -100;
        private GuiElement _lastClickTarget;
        private GuiElement _tipTarget;
        private int _tipFrames;
        private string _tip;
        private Vector2 _cursor;
        private bool _hasCursor;

        private static int _counter;

        public GuiWindow() : base(GuiKind.Window)
        {
            Padding = new Vector2(40f, 30f);
        }

        /// <summary>The game screen is open.</summary>
        public bool IsOpen => _screen != null && _screen.IsOpen;

        /// <summary>The window has the input focus of the game.</summary>
        public bool HasFocus => _screen != null && _screen.HasFocus;

        private float TitleHeight => string.IsNullOrEmpty(Title) ? 0f : 70f * K;

        // ---------- open and close ----------

        /// <summary>Opens the window; false if the game refused (no session, four mod windows already open).</summary>
        public bool Open()
        {
            if (IsOpen)
                return true;
            if (MyAPIGateway.Utilities == null || MyAPIGateway.Utilities.IsDedicated)
                return false;
            _screen = new Screen(this, "Syntrix-" + (Owner != null ? Owner.ModName : "Syntrix") + "-" + (++_counter));
            Apply();
            _screen.Open();
            if (!_screen.IsOpen)
            {
                Log.Warn("window " + (Title ?? "") + " of " + (Owner != null ? Owner.ModName : "Syntrix") + " not opened (no session or four mod windows already open)");
                _screen = null;
                return false;
            }
            _layoutValid = false;
            GuiService.WindowOpened(this);
            return true;
        }

        public void Close()
        {
            if (_screen != null && _screen.IsOpen)
                _screen.Close();
        }

        public void CloseNow()
        {
            if (_screen != null && _screen.IsOpen)
                _screen.CloseNow();
        }

        private void Apply()
        {
            if (_screen == null)
                return;
            _screen.PausesGameInSinglePlayer = Pause;
            _screen.CloseButtonEnabled = CloseButton;
            _screen.EnabledBackgroundFade = BackgroundFade;
            _screen.CloseOnEscape = true;
            _screen.Size = WindowSize;
            _screen.Position = Position;
            _layoutValid = false;
        }

        public override void Invalidate()
        {
            _layoutValid = false;
        }

        /// <summary>Gives the keyboard focus to an element (null = nobody).</summary>
        public void SetFocus(GuiElement e)
        {
            if (e != null && !e.Focusable)
                e = null;
            if (Focus == e)
                return;
            var old = Focus;
            Focus = e;
            var tf = old as GuiTextField;
            if (tf != null)
                tf.Blur();
            else if (old != null)
                old.Blur();
            var nf = e as GuiTextField;
            if (nf != null)
                nf.Focused = true;
        }

        /// <summary>Called when an element leaves the tree.</summary>
        public void Forget(GuiElement e)
        {
            for (var x = Focus; x != null; x = x.Parent)
                if (x == e) { Focus = null; break; }
            for (var x = _hover; x != null; x = x.Parent)
                if (x == e) { _hover = null; break; }
            for (var x = _pressed; x != null; x = x.Parent)
                if (x == e) { _pressed = null; break; }
            for (var x = (GuiElement)Popup; x != null; x = x.Parent)
                if (x == e) { Popup = null; break; }
            for (var x = _tipTarget; x != null; x = x.Parent)
                if (x == e) { _tipTarget = null; break; }
            _lastClickTarget = null;
        }

        // ---------- layout ----------

        private void Layout()
        {
            GuiDraw.UpdateScale();
            var screen = MyScreenSpace.NormalizedSizeToPixels(Vector2.One);
            if (_layoutValid && screen == _laidFor)
                return;
            _layoutValid = true;
            _laidFor = screen;
            var size = MyScreenSpace.NormalizedSizeToPixels(WindowSize);
            var centre = MyScreenSpace.NormalizedToPixels(Position);
            var rect = new RectangleF(centre.X - size.X / 2f, centre.Y - size.Y / 2f, size.X, size.Y);
            Arrange(rect, rect);
        }

        public override void Arrange(RectangleF rect, RectangleF clip)
        {
            Rect = rect;
            ClipArea = clip;
            var c = ContentRect;
            float t = TitleHeight;
            var content = new RectangleF(c.X, c.Y + t, c.Width, Math.Max(0f, c.Height - t));
            ArrangeChildren(content, GuiDraw.Intersect(clip, content));
        }

        /// <summary>Area of the content in reference pixels (for mods that lay out themselves).</summary>
        public Vector2 ContentSize
        {
            get
            {
                Layout();
                var c = ContentRect;
                return new Vector2(c.Width, Math.Max(0f, c.Height - TitleHeight)) / K;
            }
        }

        // ---------- drawing ----------

        private void Draw(float alpha)
        {
            Layout();
            GuiDraw.Alpha = alpha;
            GuiDraw.Layer = MyScreenSpaceLayer.BelowHud;
            GuiDraw.Clip = new RectangleF();
            try
            {
                if (TitleHeight > 0f)
                {
                    var c = ContentRect;
                    GuiDraw.TextIn(GuiStyle.FontBright, Title, new RectangleF(c.X + 40f * K, Rect.Y + 22f * K, c.Width - 80f * K, 40f * K), 1.0f, Color.White, 1);
                    GuiDraw.Rect(new RectangleF(c.X, c.Y + TitleHeight - 12f * K, c.Width, Math.Max(1f, K)), GuiStyle.Line);
                }
                if (Color.A > 0)
                    GuiDraw.Rect(Rect, Color);
                DrawContent();
                foreach (var child in Children)
                    child.DrawTree();
                if (Popup != null)
                    Popup.DrawPopup();
                if (_tip != null && _hasCursor)
                    GuiService.DrawTooltip(_tip, _cursor);
            }
            catch (Exception e)
            {
                Log.Error("window draw " + (Title ?? ""), e);
            }
            finally
            {
                GuiDraw.Alpha = 1f;
            }
        }

        // ---------- input ----------

        private static GuiElement InteractiveOf(GuiElement e)
        {
            for (; e != null; e = e.Parent)
                if (e.Interactive && e.IsEnabled)
                    return e;
            return null;
        }

        private static bool Contains(GuiElement outer, GuiElement inner)
        {
            for (var x = inner; x != null; x = x.Parent)
                if (x == outer)
                    return true;
            return false;
        }

        private void HandleInput()
        {
            _frame++;
            Layout();
            var input = MyAPIGateway.Input;
            _hasCursor = HasFocus && MyScreenSpace.TryGetCursorPositionPixels(out _cursor);
            try
            {
                if (Focus != null)
                    Focus.Keyboard();
                if (!_hasCursor)
                {
                    SetHover(null);
                    return;
                }
                var p = _cursor;

                // Open drop-down first
                if (Popup != null)
                {
                    if (!Popup.Open || !Contains(this, Popup))
                        Popup = null;
                    else
                    {
                        Popup.PopupHover(p);
                        int wheelP = input.DeltaMouseScrollWheelValue();
                        if (wheelP != 0 && Popup.PopupContains(p))
                            Popup.PopupWheel(wheelP);
                        if (input.IsNewLeftMousePressed() || input.IsNewRightMousePressed())
                        {
                            if (Popup.PopupContains(p))
                                Popup.PopupClick(p);
                            else
                                Popup.Open = false;
                            Popup = null;
                        }
                        UpdateTooltip(null);
                        return;
                    }
                }

                var hit = Hit(p);
                var target = InteractiveOf(hit);
                if (_pressed == null)
                    SetHover(target);
                if (target != null && _pressed == null)
                    target.MouseMove(p);

                // Pressing
                int button = input.IsNewLeftMousePressed() ? 0 : input.IsNewRightMousePressed() ? 1 : -1;
                if (button >= 0 && _pressed == null)
                {
                    if (Focus != null && !Contains(Focus, target))
                        SetFocus(null);
                    if (target != null)
                    {
                        if (target.Focusable)
                            SetFocus(target);
                        if (target.MouseDown(p, button))
                        {
                            _pressed = target;
                            _pressedButton = button;
                            target.Pressed = true;
                        }
                        if (button == 0)
                        {
                            if (_lastClickTarget == target && _frame - _lastClickFrame <= 25)
                            {
                                target.DoubleClick(p);
                                _lastClickFrame = -100;
                            }
                            else
                            {
                                _lastClickFrame = _frame;
                                _lastClickTarget = target;
                            }
                        }
                    }
                }
                else if (_pressed != null)
                {
                    bool held = _pressedButton == 0 ? input.IsLeftMousePressed() : input.IsRightMousePressed();
                    if (held)
                        _pressed.MouseDrag(p);
                    else
                    {
                        var e = _pressed;
                        _pressed = null;
                        e.Pressed = false;
                        e.MouseUp(p, _pressedButton, Contains(e, hit));
                    }
                }

                // Mouse wheel: innermost element that uses it
                int wheel = input.DeltaMouseScrollWheelValue();
                if (wheel != 0)
                {
                    for (var e = hit; e != null; e = e.Parent)
                        if (e.IsEnabled && e.Wheel(p, wheel))
                            break;
                }

                UpdateTooltip(hit);
            }
            catch (Exception e)
            {
                Log.Error("window input " + (Title ?? ""), e);
                _pressed = null;
            }
        }

        private void SetHover(GuiElement e)
        {
            if (_hover == e)
                return;
            if (_hover != null)
            {
                _hover.Hovered = false;
                _hover.MouseLeave();
            }
            _hover = e;
            if (e != null)
                e.Hovered = true;
        }

        private void UpdateTooltip(GuiElement hit)
        {
            GuiElement owner = null;
            string text = null;
            for (var e = hit; e != null; e = e.Parent)
            {
                text = e.TooltipAt(_cursor);
                if (text != null)
                {
                    owner = e;
                    break;
                }
            }
            if (owner == null || owner != _tipTarget)
            {
                _tipTarget = owner;
                _tipFrames = 0;
                _tip = null;
                return;
            }
            if (++_tipFrames >= 30)
                _tip = text;
        }

        private bool Cancelling()
        {
            if (Popup != null)
            {
                Popup.Open = false;
                Popup = null;
                return false;
            }
            if (Focus != null)
            {
                SetFocus(null);
                return false;
            }
            if (!EscapeCloses)
            {
                Fire(GuiEvent.Cancel);
                GuiService.Flush();
                return false;
            }
            return true;
        }

        private void Closed(bool unloading)
        {
            SetHover(null);
            SetFocus(null);
            _pressed = null;
            Popup = null;
            _screen = null;
            GuiService.WindowClosed(this);
            Fire(GuiEvent.Closed, unloading ? 1 : 0);
            if (!unloading)
                GuiService.Flush();
        }

        // ---------- properties ----------

        public override bool SetText(int prop, string v)
        {
            if (prop == GuiProp.Title) { Title = v; _layoutValid = false; return true; }
            return base.SetText(prop, v);
        }

        public override bool GetText(int prop, out string v)
        {
            if (prop == GuiProp.Title) { v = Title; return true; }
            return base.GetText(prop, out v);
        }

        public override bool SetBool(int prop, bool v)
        {
            switch (prop)
            {
                case GuiProp.Pause: Pause = v; break;
                case GuiProp.EscapeCloses: EscapeCloses = v; break;
                case GuiProp.CloseButton: CloseButton = v; break;
                case GuiProp.BackgroundFade: BackgroundFade = v; break;
                case GuiProp.IsOpen:
                    if (v) Open(); else Close();
                    return true;
                default: return base.SetBool(prop, v);
            }
            Apply();
            return true;
        }

        public override bool GetBool(int prop, out bool v)
        {
            switch (prop)
            {
                case GuiProp.Pause: v = Pause; return true;
                case GuiProp.EscapeCloses: v = EscapeCloses; return true;
                case GuiProp.CloseButton: v = CloseButton; return true;
                case GuiProp.BackgroundFade: v = BackgroundFade; return true;
                case GuiProp.IsOpen: v = IsOpen; return true;
                case GuiProp.Focused: v = HasFocus; return true;
            }
            return base.GetBool(prop, out v);
        }

        public override bool SetVector(int prop, Vector2 v)
        {
            if (prop == GuiProp.WindowSize) { WindowSize = v.X > 0f && v.Y > 0f ? v : GuiStyle.WindowSize; Apply(); return true; }
            if (prop == GuiProp.Position) { Position = v; Apply(); return true; }
            return base.SetVector(prop, v);
        }

        public override bool GetVector(int prop, out Vector2 v)
        {
            if (prop == GuiProp.WindowSize) { v = WindowSize; return true; }
            if (prop == GuiProp.Position) { v = Position; return true; }
            if (prop == GuiProp.ContentSize) { v = ContentSize; return true; }
            return base.GetVector(prop, out v);
        }

        public override void Destroyed()
        {
            CloseNow();
        }

        // ---------- the game screen ----------

        private sealed class Screen : MyModScreen
        {
            private readonly GuiWindow _w;

            public Screen(GuiWindow w, string name) : base(name, w.Position, w.WindowSize, Vector4.One)
            {
                _w = w;
            }

            protected override void OnOpened()
            {
                _w._layoutValid = false;
                _w.Fire(GuiEvent.Opened);
                GuiService.Flush();
            }

            protected override void OnLayoutChanged()
            {
                _w._layoutValid = false;
                _w.Fire(GuiEvent.Resized);
            }

            protected override void OnUpdate(bool hasFocus)
            {
                GuiService.Flush();
            }

            protected override void OnHandleInput()
            {
                _w.HandleInput();
                GuiService.Flush();
            }

            protected override void OnDraw(float alpha)
            {
                _w.Draw(alpha);
            }

            protected override bool OnCancelling()
            {
                return _w.Cancelling();
            }

            protected override void OnClosed(bool isUnloading)
            {
                if (_w._screen == this)
                    _w.Closed(isUnloading);
            }
        }
    }
}
