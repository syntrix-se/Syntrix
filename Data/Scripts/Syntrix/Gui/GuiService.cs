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
        /// <summary>The running service; null before start and after Stop.</summary>
        public static GuiService Instance { get; private set; }

        private readonly SyntrixCore _core;
        private readonly Dictionary<string, GuiClient> _clients = new Dictionary<string, GuiClient>();
        private readonly List<GuiWindow> _windows = new List<GuiWindow>();
        private readonly List<GuiOverlay> _overlays = new List<GuiOverlay>();

        private struct Pending
        {
            /// <summary>Element that raised the event.</summary>
            public GuiElement Element;
            /// <summary>Event number (GuiEvent) and index.</summary>
            public int Event, Index;
            /// <summary>Event text.</summary>
            public string Text;
        }

        private List<Pending> _queue = new List<Pending>();
        private List<Pending> _work = new List<Pending>();
        private bool _flushing;
        private readonly GuiRichText _tip = new GuiRichText();

        /// <summary>Creates the service and makes it the instance.</summary>
        public GuiService(SyntrixCore core)
        {
            _core = core;
            Instance = this;
        }

        /// <summary>1 ready, -1 not available (dedicated server).</summary>
        public int State => _core.IsPlayer ? 1 : -1;

        /// <summary>Windows can be shown (local player, not a dedicated server).</summary>
        public bool Usable => _core.IsPlayer;

        /// <summary>Window state of a mod, made on first use.</summary>
        public GuiClient Client(string modName)
        {
            GuiClient c;
            if (!_clients.TryGetValue(modName, out c))
                _clients[modName] = c = new GuiClient(this, modName);
            return c;
        }

        /// <summary>Releases all clients, closes all windows and clears the queue.</summary>
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

        /// <summary>Runs the key handlers; called every frame from HandleInput.</summary>
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

        /// <summary>Registers an overlay for drawing.</summary>
        public void AddOverlay(GuiOverlay o)
        {
            if (!_overlays.Contains(o))
                _overlays.Add(o);
        }

        /// <summary>Unregisters an overlay.</summary>
        public void RemoveOverlay(GuiOverlay o)
        {
            _overlays.Remove(o);
        }

        /// <summary>Registers a window as open.</summary>
        public static void WindowOpened(GuiWindow w)
        {
            if (Instance != null && !Instance._windows.Contains(w))
                Instance._windows.Add(w);
        }

        /// <summary>Unregisters a closed window.</summary>
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

        /// <summary>Queues an event for delivery after the input pass.</summary>
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
                Color = new Color(0, 0, 0, 70),   // a little darker than the plain window (1.2.2)
            };
            var stack = new GuiStack { FillWidth = true, FillHeight = true, Spacing = 16f };
            w.Add(stack);
            // text and buttons centred (1.2.2); the row takes the full width, so the buttons sit between two fill spacers
            var label = new GuiLabel { Text = "[align=center]" + (text ?? "") + "[/align]", FillWidth = true };
            stack.Add(label);
            var row = new GuiStack { Horizontal = true, Spacing = 12f, FillWidth = true };
            stack.Add(row);
            row.Add(new GuiLabel { Text = "", FillWidth = true });
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
            row.Add(new GuiLabel { Text = "", FillWidth = true });
            return w.Open();
        }
    }
}
