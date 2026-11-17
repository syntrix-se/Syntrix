// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using System.Text;
using RichHudFramework.Client;
using RichHudFramework.UI;
using RichHudFramework.UI.Client;
using RichHudFramework.UI.Rendering;
using RichHudFramework.UI.Rendering.Client;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Input;
using VRageMath;

namespace Syntrix.Ui
{
    /// <summary>
    /// UI service (API 2): windows, HUD elements, formatted text and key binds for docked mods, built on the
    /// Rich HUD Framework client (Rich HUD Master, Workshop 1965654081, must be loaded).
    /// Mods address elements by handle; Syntrix creates and holds the real Rich HUD elements.
    /// The Rich HUD client starts when the first mod uses the service. Inactive on dedicated servers.
    /// Events are queued and handed to the mods outside of Rich HUD's own update, so a handler may freely
    /// remove or rebuild elements.
    /// </summary>
    public sealed class UiService
    {
        private const ulong RichHudMasterId = 1965654081;
        private const int WaitTicks = 15 * 60;

        /// <summary>The running UI service.</summary>
        public static UiService Instance { get; private set; }

        private struct Pending
        {
            /// <summary>Receiving mod.</summary>
            public UiClient Client;
            /// <summary>Element handle; 0 = service event.</summary>
            public int Handle;
            /// <summary>UiEvent number.</summary>
            public int Event;
            /// <summary>Argument or null.</summary>
            public string Arg;
        }

        private readonly SyntrixCore _core;
        private readonly Dictionary<string, UiClient> _clients = new Dictionary<string, UiClient>();
        private readonly List<UiNode> _windows = new List<UiNode>();
        private List<Pending> _queue = new List<Pending>();
        private List<Pending> _work = new List<Pending>();

        private bool _started, _ready, _unavailable, _hinted, _wantPages;
        private bool _cursorOn, _hudHidden, _hudWasFull, _terminalOpen;
        private int _ticks;
        private TextBoard _measure;

        private static int _frame, _typingFrame = -100;

        /// <summary>Makes the UI service.</summary>
        public UiService(SyntrixCore core)
        {
            _core = core;
            Instance = this;
        }

        /// <summary>Rich HUD is registered: elements can be made.</summary>
        public bool Ready { get { return _ready; } }

        /// <summary>1 ready, 0 starting, -1 not available (dedicated server or Rich HUD Master missing).</summary>
        public int State { get { return !_core.IsPlayer || _unavailable ? -1 : _ready ? 1 : 0; } }

        /// <summary>True while a text field took typing in this or the last two frames.</summary>
        public static bool TypedRecently { get { return _frame - _typingFrame <= 2; } }

        /// <summary>Notes that a text field took a key this frame.</summary>
        public static void MarkTyping() { _typingFrame = _frame; }

        /// <summary>UI state of a mod, made on first use.</summary>
        public UiClient Client(string modName)
        {
            UiClient c;
            if (!_clients.TryGetValue(modName, out c))
                _clients[modName] = c = new UiClient(this, modName);
            return c;
        }

        /// <summary>Saves the binds, gives back HUD and cursor and forgets all mods.</summary>
        public void Stop()
        {
            try
            {
                if (_ready)
                {
                    foreach (var c in _clients.Values)
                        c.SaveBinds();
                    if (_hudHidden && _hudWasFull)
                        MyVisualScriptLogicProvider.ShowHud(true, 0L);
                    if (_cursorOn)
                        HudMain.EnableCursor = false;
                }
            }
            catch (Exception e)
            {
                Log.Error("UI stop", e);
            }
            _clients.Clear();
            _windows.Clear();
            _queue.Clear();
            _ready = false;
            if (Instance == this)
                Instance = null;
        }

        // ---------------------------------------------------------------- Rich HUD client

        /// <summary>Sets the event receiver of a mod, starts the Rich HUD client on first use and queues Ready or Unavailable.</summary>
        public void SetDispatcher(UiClient c, Action<int, int, string> dispatcher)
        {
            // The same dispatcher again (the mod docked again): no second Ready
            bool again = c.Dispatcher != null && dispatcher != null && c.Dispatcher.Equals(dispatcher);
            c.Dispatcher = dispatcher;
            if (dispatcher == null || !_core.IsPlayer || again)
                return;
            if (!_started)
            {
                _started = true;
                RichHudClient.Init("Syntrix", HudReady, HudReset);
            }
            if (_ready)
                Queue(c, 0, (int)UiEvent.Ready, null);
            else if (_unavailable)
                Queue(c, 0, (int)UiEvent.Unavailable, null);
        }

        private void HudReady()
        {
            try
            {
                _ready = true;
                _unavailable = false;
                _measure = null;
                _cursorOn = false;
                _hudHidden = false;
                UiMarkup.ResetFonts();
                foreach (var c in _clients.Values)
                {
                    c.ApplyAllBinds();
                    if (c.Dispatcher != null)
                        Queue(c, 0, (int)UiEvent.Ready, null);
                }
                _wantPages = true;
                Log.Info("UI ready (Rich HUD)");
            }
            catch (Exception e)
            {
                Log.Error("UI ready", e);
            }
        }

        private void HudReset()
        {
            if (!_ready)
                return;
            _ready = false;
            _windows.Clear();
            _measure = null;
            foreach (var c in _clients.Values)
            {
                c.Forget();
                if (c.Dispatcher != null)
                    Queue(c, 0, (int)UiEvent.Lost, null);
            }
        }

        // ---------------------------------------------------------------- events

        /// <summary>Queues an event; sent after the Rich HUD update.</summary>
        public void Queue(UiClient c, int handle, int ev, string arg)
        {
            _queue.Add(new Pending { Client = c, Handle = handle, Event = ev, Arg = arg });
        }

        /// <summary>Hands queued events to the mods; events raised meanwhile wait for the next call.</summary>
        private void Flush()
        {
            if (_queue.Count == 0)
                return;
            var list = _queue;
            _queue = _work;
            _work = list;
            for (int i = 0; i < list.Count; i++)
            {
                var p = list[i];
                var d = p.Client.Dispatcher;
                if (d == null)
                    continue;
                try { d(p.Handle, p.Event, p.Arg); }
                catch (Exception e) { Log.Error("UI event " + p.Event + " of " + p.Client.ModName, e); }
            }
            list.Clear();
        }

        /// <summary>Queues a bind event unless the player types.</summary>
        public void BindPressed(UiClient c, string name)
        {
            if (TypedRecently || MyAPIGateway.Gui.ChatEntryVisible)
                return;
            Queue(c, 0, (int)UiEvent.BindPressed, name);
        }

        /// <summary>Asks for the rebind pages once Rich HUD is ready.</summary>
        public void WantPages() { _wantPages = true; }

        /// <summary>Tracks a window (cursor, Escape, focus).</summary>
        public void AddWindow(UiNode n) { _windows.Add(n); }

        /// <summary>Stops tracking a window.</summary>
        public void RemoveWindow(UiNode n)
        {
            _windows.Remove(n);
            if (n.Window != null && n.Window.WasVisible)
                n.Window.WasVisible = false;
        }

        // ---------------------------------------------------------------- per frame

        /// <summary>After simulation: notices a missing Rich HUD Master.</summary>
        public void Update()
        {
            if (!_started || _ready || _hinted || ++_ticks < WaitTicks)
                return;
            _hinted = true;
            _unavailable = true;
            bool listed = false;
            var mods = MyAPIGateway.Session.Mods;
            if (mods != null)
                foreach (var m in mods)
                    if (m.PublishedFileId == RichHudMasterId)
                        listed = true;
            Log.Warn("UI not available: Rich HUD Master (Workshop " + RichHudMasterId + ") " + (listed ? "did not answer" : "is not loaded")
                + ". Windows and key binds of docked mods are off.");
            if (MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Admin)
                MyAPIGateway.Utilities.ShowMessage("Syntrix", "Rich HUD Master (Workshop " + RichHudMasterId + ") is missing, mod windows are off. Add it to the world's mods.");
            foreach (var c in _clients.Values)
                if (c.Dispatcher != null)
                    Queue(c, 0, (int)UiEvent.Unavailable, null);
        }

        /// <summary>Draw: events, rebind pages, saving binds after the Rich HUD terminal closed.</summary>
        public void Frame()
        {
            _frame++;
            Flush();
            if (!_ready)
                return;
            if (_wantPages)
            {
                _wantPages = false;
                foreach (var c in _clients.Values)
                    c.MakePage();
            }
            if (_frame % 30 == 0)
            {
                bool open = RichHudTerminal.Open;
                if (_terminalOpen && !open)
                    foreach (var c in _clients.Values)
                        c.SaveBinds();
                _terminalOpen = open;
            }
        }

        /// <summary>Input: open windows keep cursor, HUD and view as asked, Closed events.</summary>
        public void Input()
        {
            Flush();
            if (!_ready)
                return;
            bool modal = false, hide = false, lockView = false;
            for (int i = 0; i < _windows.Count; i++)
            {
                var n = _windows[i];
                var w = n.Window;
                bool visible = !n.Dead && w.Visible;
                if (w.WasVisible && !visible)
                    n.Fire(UiEvent.Closed, null);
                w.WasVisible = visible;
                if (!visible || !w.Modal)
                    continue;
                modal = true;
                hide |= w.HideHud;
                lockView |= w.LockView;
            }

            bool cursor = modal;
            if (!cursor)
                foreach (var c in _clients.Values)
                    cursor |= c.WantsCursor;
            if (cursor != _cursorOn)
            {
                _cursorOn = cursor;
                HudMain.EnableCursor = cursor;
            }

            if (hide && !_hudHidden)
            {
                _hudHidden = true;
                _hudWasFull = !(MyAPIGateway.Session.Config != null && MyAPIGateway.Session.Config.MinimalHud);
            }
            if (_hudHidden)
            {
                if (!hide)
                {
                    _hudHidden = false;
                    if (_hudWasFull)
                        MyVisualScriptLogicProvider.ShowHud(true, 0L);
                }
                else if (_hudWasFull)
                    // The game turns the HUD back on after loading the world; keep it off while open
                    MyVisualScriptLogicProvider.ShowHud(false, 0L);
            }

            if (!modal)
                return;
            BindManager.RequestTempBlacklist(SeBlacklistModes.MouseAndCam);
            if (lockView)
                HoldView();
        }

        /// <summary>
        /// In seats the game hands mouse look to the controls before the mods, so the Rich HUD blacklist does not
        /// hold it: rotation is set to zero here (moving stays). Looking around in a passenger seat or with Alt is
        /// applied after the mods; it is undone in advance by rotating back by the same amount.
        /// </summary>
        private static void HoldView()
        {
            var controlled = MyAPIGateway.Session.ControlledObject;
            if (controlled == null)
                return;
            controlled.MoveAndRotate(controlled.LastMotionIndicator, Vector2.Zero, 0f);
            if (MyAPIGateway.Gui.IsCursorVisible || MyAPIGateway.Gui.ChatEntryVisible)
                return;
            var seat = controlled as IMyShipController;
            if (!MyAPIGateway.Input.IsAnyAltKeyPressed() && (seat == null || seat.CanControlShip))
                return;
            var camera = MyAPIGateway.Session.CameraController;
            var rotation = MyAPIGateway.Input.GetRotation();
            float roll = MyAPIGateway.Input.GetRoll();
            if (camera != null && (rotation != Vector2.Zero || roll != 0f))
                camera.Rotate(-rotation, -roll);
        }

        // ---------------------------------------------------------------- helpers

        /// <summary>Size of a markup text in a format.</summary>
        public Vector2 Measure(string markup, UiMarkup.Style style, float wrapWidth)
        {
            if (!_ready)
                return Vector2.Zero;
            if (_measure == null)
                _measure = new TextBoard();
            _measure.BuilderMode = wrapWidth > 0f ? TextBuilderModes.Wrapped : TextBuilderModes.Lined;
            if (wrapWidth > 0f)
                _measure.LineWrapWidth = wrapWidth;
            _measure.SetText(UiMarkup.Parse(markup, style, Color.White, null));
            return _measure.TextSize;
        }

        /// <summary>Screen size in pixels; zero before the UI is ready.</summary>
        public Vector2 ScreenSize { get { return _ready ? new Vector2(HudMain.ScreenWidth, HudMain.ScreenHeight) : Vector2.Zero; } }

        /// <summary>Resolution scale of the high-DPI root.</summary>
        public float ResScale { get { return _ready ? HudMain.ResScale : 1f; } }

        /// <summary>Cursor position on the screen (centre origin).</summary>
        public Vector2 CursorPosition { get { return _ready && HudMain.Cursor != null ? HudMain.Cursor.ScreenPos : Vector2.Zero; } }

        /// <summary>Cursor position in the world.</summary>
        public Vector3D CursorWorld { get { return _ready && HudMain.Cursor != null ? HudMain.Cursor.WorldPos : Vector3D.Zero; } }

        /// <summary>Rich HUD clipboard as plain text.</summary>
        public string Clipboard
        {
            get { return _ready ? HudMain.ClipBoard.ToString() : null; }
            set { if (_ready) HudMain.ClipBoard = new RichText(value ?? ""); }
        }

        /// <summary>True while the Rich HUD terminal is open.</summary>
        public bool TerminalIsOpen { get { return _ready && RichHudTerminal.Open; } }

        /// <summary>
        /// Opens or closes the Rich HUD terminal: -1 close, -2 toggle, otherwise open.
        /// </summary>
        /// <param name="mode">-1 close, -2 toggle, otherwise open.</param>
        public void Terminal(int mode)
        {
            if (!_ready)
                return;
            if (mode == -1) RichHudTerminal.CloseMenu();
            else if (mode == -2) RichHudTerminal.ToggleMenu();
            else RichHudTerminal.OpenMenu();
        }

        /// <summary>
        /// Screen values: 0 width, 1 height, 2 aspect ratio, 3 field of view (radians), 4 FOV scale,
        /// 5 UI background opacity, 6 resolution scale, 7 input mode (0 default, 1 cursor, 2 text input),
        /// 8 cursor visible (1/0), 9 cursor captured (1/0).
        /// </summary>
        /// <param name="what">Number of the value.</param>
        public float ScreenInfo(int what)
        {
            if (!_ready)
                return 0f;
            switch (what)
            {
                case 0: return HudMain.ScreenWidth;
                case 1: return HudMain.ScreenHeight;
                case 2: return HudMain.AspectRatio;
                case 3: return HudMain.Fov;
                case 4: return HudMain.FovScale;
                case 5: return HudMain.UiBkOpacity;
                case 6: return HudMain.ResScale;
                case 7: return (float)(int)HudMain.InputMode;
                case 8: return HudMain.Cursor != null && HudMain.Cursor.Visible ? 1f : 0f;
                case 9: return HudMain.Cursor != null && HudMain.Cursor.IsCaptured ? 1f : 0f;
            }
            return 0f;
        }

        private static string BindFile(string modName)
        {
            var sb = new StringBuilder("Binds_");
            foreach (char ch in modName)
                sb.Append(char.IsLetterOrDigit(ch) || ch == '-' || ch == '_' ? ch : '_');
            return sb.Append(".xml").ToString();
        }

        /// <summary>Saved key binds of a mod from Syntrix's local storage, null if none.</summary>
        public BindDefinition[] LoadBinds(string modName)
        {
            try
            {
                string file = BindFile(modName);
                if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(file, typeof(UiService)))
                    return null;
                using (var r = MyAPIGateway.Utilities.ReadFileInLocalStorage(file, typeof(UiService)))
                    return MyAPIGateway.Utilities.SerializeFromXML<BindDefinition[]>(r.ReadToEnd());
            }
            catch (Exception e)
            {
                Log.Warn("UI: saved key binds of " + modName + " unreadable, defaults stay: " + e.Message);
                return null;
            }
        }

        /// <summary>Saves the binds of a mod in the local storage.</summary>
        public void SaveBinds(string modName, BindDefinition[] binds)
        {
            using (var w = MyAPIGateway.Utilities.WriteFileInLocalStorage(BindFile(modName), typeof(UiService)))
                w.Write(MyAPIGateway.Utilities.SerializeToXML(binds));
        }
    }
}
