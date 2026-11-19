// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRageMath;

namespace Syntrix.Gui
{
    /// <summary>
    /// Window service (API 3): windows and overlays in the Vanilla style for docked mods, drawn by Syntrix with the
    /// game's screen API (MyModScreen, MyScreenSpace…). Mods address elements by handle. Inactive on dedicated servers
    /// (State -1, Create returns 0). Events are queued and handed out after the input pass.
    /// </summary>
    public sealed class GuiService
    {
        public static GuiService Instance { get; private set; }

        private readonly SyntrixCore _core;
        private readonly Dictionary<string, GuiClient> _clients = new Dictionary<string, GuiClient>();
        private readonly List<GuiWindow> _windows = new List<GuiWindow>();
        private readonly List<GuiOverlay> _overlays = new List<GuiOverlay>();

        private struct Pending
        {
            public GuiElement Element;
            public int Event, Index;
            public string Text;
        }

        private List<Pending> _queue = new List<Pending>();
        private List<Pending> _work = new List<Pending>();
        private bool _flushing;
        private readonly GuiRichText _tip = new GuiRichText();

        public GuiService(SyntrixCore core)
        {
            _core = core;
            Instance = this;
        }

        /// <summary>1 ready, -1 not available (dedicated server).</summary>
        public int State => _core.IsPlayer ? 1 : -1;

        public bool Usable => _core.IsPlayer;

        /// <summary>Window state of a mod, made on first use.</summary>
        public GuiClient Client(string modName)
        {
            GuiClient c;
            if (!_clients.TryGetValue(modName, out c))
                _clients[modName] = c = new GuiClient(this, modName);
            return c;
        }

        public void Stop()
        {
            foreach (var c in _clients.Values)
                c.Release();
            _clients.Clear();
            foreach (var w in _windows.ToArray())
                w.CloseNow();
            _windows.Clear();
            _overlays.Clear();
            _queue.Clear();
            if (Instance == this)
                Instance = null;
        }

        // ---------- keys ----------

        private readonly List<Action> _keyHandlers = new List<Action>();

        /// <summary>Handler run every frame from the session's HandleInput (key binds of Syntrix' own windows).</summary>
        public void OnKeys(Action handler)
        {
            if (handler != null)
                _keyHandlers.Add(handler);
        }

        public void HandleKeys()
        {
            if (!Usable)
                return;
            foreach (var a in _keyHandlers)
            {
                try { a(); }
                catch (Exception e) { Log.Error("GUI keys", e); }
            }
        }

        // ---------- elements ----------

        /// <summary>New element of a kind, null for unknown kinds.</summary>
        public static GuiElement Make(int kind)
        {
            switch (kind)
            {
                case GuiKind.Window: return new GuiWindow();
                case GuiKind.Overlay: return new GuiOverlay();
                case GuiKind.Panel: return new GuiElement(GuiKind.Panel);
                case GuiKind.Stack: return new GuiStack();
                case GuiKind.Scroll: return new GuiScroll();
                case GuiKind.Label: return new GuiLabel();
                case GuiKind.Button: return new GuiButton();
                case GuiKind.Image: return new GuiImage();
                case GuiKind.TextField: return new GuiTextField();
                case GuiKind.List: return new GuiList();
                case GuiKind.Tree: return new GuiTree();
                case GuiKind.Table: return new GuiTable();
                case GuiKind.CheckBox: return new GuiCheckBox();
                case GuiKind.Slider: return new GuiSlider();
                case GuiKind.ProgressBar: return new GuiProgressBar();
                case GuiKind.Tabs: return new GuiTabs();
                case GuiKind.Canvas: return new GuiCanvas();
                case GuiKind.Dropdown: return new GuiDropdown();
                default: return null;
            }
        }

        public void AddOverlay(GuiOverlay o)
        {
            if (!_overlays.Contains(o))
                _overlays.Add(o);
        }

        public void RemoveOverlay(GuiOverlay o)
        {
            _overlays.Remove(o);
        }

        public static void WindowOpened(GuiWindow w)
        {
            if (Instance != null && !Instance._windows.Contains(w))
                Instance._windows.Add(w);
        }

        public static void WindowClosed(GuiWindow w)
        {
            if (Instance != null)
                Instance._windows.Remove(w);
        }

        /// <summary>Open windows of all mods (the game allows four mod windows at once).</summary>
        public int WindowCount => _windows.Count;

        /// <summary>A Syntrix window has the input focus.</summary>
        public bool AnyWindowFocused
        {
            get
            {
                foreach (var w in _windows)
                    if (w.HasFocus)
                        return true;
                return false;
            }
        }

        // ---------- events ----------

        public static void Post(GuiElement e, int ev, int index, string text)
        {
            if (Instance == null)
                return;
            Instance._queue.Add(new Pending { Element = e, Event = ev, Index = index, Text = text });
        }

        /// <summary>Hands out queued events (from the window hooks and the session update).</summary>
        public static void Flush()
        {
            var s = Instance;
            if (s == null || s._flushing)
                return;
            s._flushing = true;
            try
            {
                int guard = 0;
                while (s._queue.Count > 0 && guard++ < 8)
                {
                    var t = s._work;
                    s._work = s._queue;
                    s._queue = t;
                    foreach (var p in s._work)
                        p.Element.Deliver(p.Event, p.Index, p.Text);
                    s._work.Clear();
                }
            }
            finally
            {
                s._flushing = false;
            }
        }

        // ---------- drawing ----------

        /// <summary>Overlays, from the session's Draw().</summary>
        public void Draw()
        {
            if (!Usable || _overlays.Count == 0)
                return;
            bool menus = MyAPIGateway.Gui != null && MyAPIGateway.Gui.IsCursorVisible;
            bool hud = MyAPIGateway.Session != null && MyAPIGateway.Session.Config != null && MyAPIGateway.Session.Config.HudState != 0;
            GuiDraw.UpdateScale();
            foreach (var o in _overlays.ToArray())
            {
                if (!o.Visible || !(o.ShowInMenus || hud && !menus))
                    continue;
                o.DrawOverlay();
            }
        }

        /// <summary>Tooltip box at the mouse cursor (Vanilla tooltip background, wraps at 420 reference pixels).</summary>
        public static void DrawTooltip(string text, Vector2 cursor)
        {
            var s = Instance;
            if (s == null || string.IsNullOrEmpty(text))
                return;
            float k = GuiDraw.K;
            s._tip.Set(text, true, GuiStyle.Font, GuiStyle.TextScale * 0.9f, GuiStyle.Text, true);
            s._tip.Layout(420f * k);
            float pad = 10f * k;
            var size = new Vector2(Math.Min(420f * k, s._tip.Width) + 2f * pad, s._tip.Height + 2f * pad);
            var screen = ScreenPixels;
            float x = cursor.X + 20f * k, y = cursor.Y + 24f * k;
            if (x + size.X > screen.X)
                x = Math.Max(0f, cursor.X - size.X - 8f * k);
            if (y + size.Y > screen.Y)
                y = Math.Max(0f, cursor.Y - size.Y - 8f * k);
            var r = new RectangleF(x, y, size.X, size.Y);
            var layer = GuiDraw.Layer;
            GuiDraw.Layer = MyScreenSpaceLayer.AboveEverything;
            GuiDraw.Rect(r, new Color(10, 14, 18, 235));
            GuiDraw.Texture(GuiStyle.Tooltip, r, Color.White);
            GuiDraw.Border(r, GuiStyle.Line, Math.Max(1f, k));
            s._tip.Draw(new Vector2(x + pad, y + pad), size.X - 2f * pad);
            GuiDraw.Layer = layer;
        }

        /// <summary>Screen size in screen pixels.</summary>
        public static Vector2 ScreenPixels
        {
            get
            {
                if (MyAPIGateway.Session != null && MyAPIGateway.Session.Camera != null)
                    return MyAPIGateway.Session.Camera.ViewportSize;
                return MyScreenSpace.NormalizedSizeToPixels(Vector2.One);
            }
        }

        /// <summary>Screen size in reference pixels (1080 high).</summary>
        public static Vector2 ScreenReference
        {
            get
            {
                GuiDraw.UpdateScale();
                return ScreenPixels / GuiDraw.K;
            }
        }

        /// <summary>Text size in reference pixels.</summary>
        public static Vector2 Measure(string font, string text, float scale)
        {
            GuiDraw.UpdateScale();
            if (string.IsNullOrEmpty(font) || !MyScreenSpaceText.IsFontAvailable(font))
                font = GuiStyle.Font;
            return GuiDraw.Measure(font, text ?? "", scale > 0f ? scale : GuiStyle.TextScale) / GuiDraw.K;
        }

        // ---------- confirmation ----------

        /// <summary>Small window with a text and two buttons; answer true for yes. No answer when the window is closed with Esc.</summary>
        public bool Confirm(string title, string text, string yes, string no, Action<bool> answer)
        {
            if (!Usable)
                return false;
            var w = new GuiWindow
            {
                Title = title,
                WindowSize = new Vector2(0.42f, 0.30f),
                Pause = false,
            };
            var stack = new GuiStack { FillWidth = true, FillHeight = true, Spacing = 16f };
            w.Add(stack);
            var label = new GuiLabel { Text = text ?? "", FillWidth = true };
            stack.Add(label);
            var row = new GuiStack { Horizontal = true, Spacing = 12f, Anchor = 1 };
            stack.Add(row);
            bool answered = false;
            Action<bool> reply = ok =>
            {
                if (answered)
                    return;
                answered = true;
                w.Close();
                if (answer != null)
                {
                    try { answer(ok); }
                    catch (Exception e) { Log.Error("confirm answer", e); }
                }
            };
            var yesButton = new GuiButton { Text = string.IsNullOrEmpty(yes) ? "OK" : yes };
            yesButton.Handler = (e, ev, i, t) => { if (ev == GuiEvent.Click && i == 0) reply(true); };
            row.Add(yesButton);
            if (!string.IsNullOrEmpty(no))
            {
                var noButton = new GuiButton { Text = no };
                noButton.Handler = (e, ev, i, t) => { if (ev == GuiEvent.Click && i == 0) reply(false); };
                row.Add(noButton);
            }
            return w.Open();
        }
    }

    /// <summary>Elements of one docked mod, addressed by handle.</summary>
    public sealed class GuiClient
    {
        public readonly string ModName;
        private readonly GuiService _service;
        private readonly Dictionary<int, GuiElement> _elements = new Dictionary<int, GuiElement>();
        private int _next = 1;
        private Action<int, int, int, string> _handler;

        public GuiClient(GuiService service, string modName)
        {
            _service = service;
            ModName = modName;
        }

        public void SetHandler(Action<int, int, int, string> handler)
        {
            _handler = handler;
        }

        public void Dispatch(int handle, int ev, int index, string text)
        {
            if (_handler == null || !_elements.ContainsKey(handle))
                return;
            try
            {
                _handler(handle, ev, index, text);
            }
            catch (Exception e)
            {
                Log.Error("GUI event handler of " + ModName, e);
            }
        }

        public GuiElement Get(int handle)
        {
            GuiElement e;
            return _elements.TryGetValue(handle, out e) ? e : null;
        }

        public int Create(int kind, int parent)
        {
            if (!_service.Usable)
                return 0;
            var e = GuiService.Make(kind);
            if (e == null)
            {
                Log.Warn(ModName + ": unknown element kind " + kind);
                return 0;
            }
            e.Owner = this;
            e.Handle = _next++;
            _elements[e.Handle] = e;
            var overlay = e as GuiOverlay;
            if (overlay != null)
                _service.AddOverlay(overlay);
            else if (!(e is GuiWindow) && parent > 0)
            {
                var p = Get(parent);
                if (p != null)
                    p.Add(e);
            }
            return e.Handle;
        }

        public void Remove(int handle)
        {
            var e = Get(handle);
            if (e == null)
                return;
            var w = e.Root as GuiWindow;
            if (w != null && w != e)
                w.Forget(e);
            if (e.Parent != null)
                e.Parent.Remove(e);
            Drop(e);
        }

        private void Drop(GuiElement e)
        {
            foreach (var c in e.Children.ToArray())
                Drop(c);
            if (e.Owner == this)
                _elements.Remove(e.Handle);
            var overlay = e as GuiOverlay;
            if (overlay != null)
                _service.RemoveOverlay(overlay);
            try { e.Destroyed(); }
            catch (Exception x) { Log.Error("GUI remove", x); }
            e.Owner = null;
            e.Handler = null;
        }

        public void Clear(int handle)
        {
            var e = Get(handle);
            if (e == null)
                return;
            foreach (var c in e.Children.ToArray())
                Remove(c.Handle);
        }

        public void SetParent(int handle, int parent)
        {
            var e = Get(handle);
            var p = Get(parent);
            if (e == null || e is GuiWindow || e is GuiOverlay)
                return;
            var w = e.Root as GuiWindow;
            if (w != null && w != e)
                w.Forget(e);
            if (p == null)
            {
                if (e.Parent != null)
                    e.Parent.Remove(e);
                return;
            }
            for (var x = p; x != null; x = x.Parent)
                if (x == e)
                    return;
            p.Add(e);
        }

        public void Release()
        {
            foreach (var e in new List<GuiElement>(_elements.Values))
                if (e.Parent == null && e.Owner == this)
                    Drop(e);
            _elements.Clear();
            _handler = null;
        }

        // ---------- properties ----------

        public void SetBool(int h, int prop, bool v) { var e = Get(h); if (e != null) e.SetBool(prop, v); }
        public void SetInt(int h, int prop, int v) { var e = Get(h); if (e != null) e.SetInt(prop, v); }
        public void SetFloat(int h, int prop, float v) { var e = Get(h); if (e != null) e.SetFloat(prop, v); }
        public void SetVector(int h, int prop, Vector2 v) { var e = Get(h); if (e != null) e.SetVector(prop, v); }
        public void SetColor(int h, int prop, Color v) { var e = Get(h); if (e != null) e.SetColor(prop, v); }
        public void SetText(int h, int prop, string v) { var e = Get(h); if (e != null) e.SetText(prop, v); }

        public bool GetBool(int h, int prop) { var e = Get(h); bool v; return e != null && e.GetBool(prop, out v) && v; }
        public int GetInt(int h, int prop) { var e = Get(h); int v; return e != null && e.GetInt(prop, out v) ? v : 0; }
        public float GetFloat(int h, int prop) { var e = Get(h); float v; return e != null && e.GetFloat(prop, out v) ? v : 0f; }
        public Vector2 GetVector(int h, int prop) { var e = Get(h); Vector2 v; return e != null && e.GetVector(prop, out v) ? v : Vector2.Zero; }
        public Color GetColor(int h, int prop) { var e = Get(h); Color v; return e != null && e.GetColor(prop, out v) ? v : Color.Transparent; }
        public string GetText(int h, int prop) { var e = Get(h); string v; return e != null && e.GetText(prop, out v) ? v : null; }

        // ---------- items ----------

        public int ItemAdd(int h, string key, string parent, string text, string icons) { var e = Get(h); return e != null ? e.ItemAdd(key, parent, text, icons) : -1; }
        public void ItemSet(int h, int index, int prop, string value) { var e = Get(h); if (e != null) e.ItemSet(index, prop, value); }
        public void ItemRemove(int h, int index) { var e = Get(h); if (e != null) e.ItemRemove(index); }
        public void ItemClear(int h) { var e = Get(h); if (e != null) e.ItemClear(); }
        public int ItemCount(int h) { var e = Get(h); return e != null ? e.ItemCount : 0; }
        public int ItemFind(int h, string key) { var e = Get(h); return e != null ? e.ItemFind(key) : -1; }

        // ---------- windows ----------

        public bool Open(int h)
        {
            var w = Get(h) as GuiWindow;
            return w != null && w.Open();
        }

        public void Close(int h)
        {
            var w = Get(h) as GuiWindow;
            if (w != null)
                w.Close();
        }

        public void Focus(int h)
        {
            var e = Get(h);
            if (e == null)
                return;
            var w = e.Root as GuiWindow;
            if (w != null)
                w.SetFocus(e);
        }

        public void SetDraw(int h, Action<Vector4, float> draw)
        {
            var c = Get(h) as GuiCanvas;
            if (c != null)
                c.DrawCallback = draw;
        }

        public void Expand(int h, int index)
        {
            var t = Get(h) as GuiTree;
            if (t != null)
                t.Reveal(index);
        }

        public void ScrollTo(int h, int target)
        {
            var e = Get(h);
            var s = e as GuiScroll;
            var t = Get(target);
            if (s != null && t != null)
                s.ScrollTo(t);
        }
    }

    /// <summary>
    /// Layer on the screen without a window: no mouse, children placed by anchor on the whole screen.
    /// Hidden with the HUD and while a menu or window shows the mouse cursor, unless ShowInMenus.
    /// </summary>
    public class GuiOverlay : GuiElement
    {
        public bool ShowInMenus;
        public int Layer;

        public GuiOverlay() : base(GuiKind.Overlay)
        {
        }

        public void DrawOverlay()
        {
            var screen = GuiService.ScreenPixels;
            var rect = new RectangleF(0f, 0f, screen.X, screen.Y);
            GuiDraw.Layer = Layer == 1 ? MyScreenSpaceLayer.AboveHud : Layer == 2 ? MyScreenSpaceLayer.AboveEverything : MyScreenSpaceLayer.BelowHud;
            GuiDraw.Alpha = 1f;
            GuiDraw.Clip = new RectangleF();
            try
            {
                Arrange(rect, rect);
                DrawTree();
            }
            catch (Exception e)
            {
                Log.Error("overlay draw of " + (Owner != null ? Owner.ModName : "Syntrix"), e);
                Visible = false;
            }
            finally
            {
                GuiDraw.Layer = MyScreenSpaceLayer.BelowHud;
            }
        }

        public override bool SetBool(int prop, bool v)
        {
            if (prop == GuiProp.ShowInMenus) { ShowInMenus = v; return true; }
            return base.SetBool(prop, v);
        }

        public override bool GetBool(int prop, out bool v)
        {
            if (prop == GuiProp.ShowInMenus) { v = ShowInMenus; return true; }
            return base.GetBool(prop, out v);
        }

        public override bool SetInt(int prop, int v)
        {
            if (prop == GuiProp.Layer) { Layer = MathHelper.Clamp(v, 0, 2); return true; }
            return base.SetInt(prop, v);
        }

        public override bool GetInt(int prop, out int v)
        {
            if (prop == GuiProp.Layer) { v = Layer; return true; }
            return base.GetInt(prop, out v);
        }
    }

    /// <summary>Frame hooks for the window service: overlays in Draw(), queued events in the update.</summary>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class GuiSession : MySessionComponentBase
    {
        /// <summary>Actions for the next update (things that must not run inside chat or input handlers).</summary>
        private static readonly List<Action> _later = new List<Action>();

        public static void Later(Action a)
        {
            if (a != null)
                _later.Add(a);
        }

        public override void UpdateAfterSimulation()
        {
            RunLater();
            GuiService.Flush();
        }

        public override void HandleInput()
        {
            RunLater();
            var s = GuiService.Instance;
            if (s != null)
                s.HandleKeys();
        }

        private static void RunLater()
        {
            if (_later.Count == 0)
                return;
            var list = _later.ToArray();
            _later.Clear();
            foreach (var a in list)
            {
                try { a(); }
                catch (Exception e) { Log.Error("GUI deferred action", e); }
            }
        }

        public override void Draw()
        {
            var s = GuiService.Instance;
            if (s != null)
                s.Draw();
        }

        protected override void UnloadData()
        {
            _later.Clear();
        }
    }
}
