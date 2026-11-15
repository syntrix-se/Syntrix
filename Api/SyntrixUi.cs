// Syntrix UI dock file (API version 2, Syntrix 1.1.0). Optional second dock file for the UI service:
// copy it next to SyntrixApi.cs only if your mod shows windows or HUD elements. Do not edit it.
// Needs Rich HUD Master (Workshop 1965654081) in the world, which is a dependency of Syntrix.
//
// MIT License - Copyright (c) 2026 Syntrix contributors
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files, to deal in the Software without restriction,
// subject to including this notice. Full text: LICENSE in the Syntrix repository.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND.
using System;
using System.Collections.Generic;
using VRage.Utils;
using VRageMath;

namespace Syntrix.Api
{
    /// <summary>Element kinds (API 2). Values are fixed.</summary>
    public static class UiKind
    {
        public const int Empty = 1, Box = 2, Border = 3, Label = 4, LabelBox = 5, Panel = 6, Button = 7, TextField = 8,
            ScrollBox = 9, ScrollBoxHorizontal = 10, Chain = 11, ChainHorizontal = 12, ClickArea = 13, Window = 14,
            CheckBox = 15, Slider = 16;
    }

    /// <summary>Element properties (API 2), by value type. Values are fixed. Unknown ones are ignored.</summary>
    public static class UiProp
    {
        // Vector2
        public const int Offset = 1, Size = 2, Padding = 3, MinSize = 4, TextPadding = 5, TextureSize = 6, TextureOffset = 7,
            TextureRegion = 8, Position = 9, TextSize = 10, MemberMaxSize = 11, MemberMinSize = 12;
        // float
        public const int Width = 20, Height = 21, Spacing = 22, BorderThickness = 23, FontSize = 24, WrapWidth = 25,
            WrapToParent = 26, WrapMax = 27, Value = 28, Min = 29, Max = 30, HeaderHeight = 31;
        // bool
        public const int Visible = 40, AutoResize = 41, Wrap = 42, VertCenter = 43, Draggable = 44, Resizable = 45, HideHud = 46,
            LockView = 47, CloseOnEscape = 48, SmoothScrolling = 49, Scrolling = 50, HighlightEnabled = 51, Checked = 52,
            MousedOver = 53, InputOpen = 54, InputEnabled = 55, Masking = 56, Focused = 57, Modal = 58;
        // int
        public const int ParentAlign = 60, DimAlign = 61, TextAlign = 62, FontStyle = 63, Sizing = 64, Start = 65, ZOffset = 66,
            MaxLength = 67, CharFilter = 68, Count = 69, TextureAlign = 70, End = 71, EnabledCount = 72;
        // Color
        public const int Color = 80, HighlightColor = 81, BorderColor = 82, TextColor = 83, LinkColor = 84, FocusColor = 85,
            BarColor = 86, BarHighlight = 87, SliderColor = 88, SliderHighlight = 89, DividerColor = 90, HeaderColor = 91,
            BodyColor = 92, TickColor = 93;
        // string
        public const int Text = 100, PlainText = 101, Font = 102, Texture = 103, Placeholder = 104, Tooltip = 105, Title = 106;
    }

    /// <summary>Events (API 2). Values are fixed. 1-10 belong to an element, 20-23 to the service.</summary>
    public static class UiEvent
    {
        public const int Click = 1, RightClick = 2, CursorEnter = 3, CursorExit = 4, TextChanged = 5, Submit = 6,
            ValueChanged = 7, Link = 8, Resized = 9, Closed = 10, BindPressed = 20, Ready = 21, Lost = 22, Unavailable = 23;
    }

    /// <summary>Position relative to the parent (Rich HUD ParentAlignments, flags). Inner = inside the parent's edge.</summary>
    public static class UiAlign
    {
        public const int Center = 0, Left = 1, Bottom = 2, Right = 4, Top = 8, InnerH = 16, InnerV = 32, UsePadding = 64,
            Inner = InnerH | InnerV, InnerLeft = Inner | Left, InnerRight = Inner | Right, InnerTop = Inner | Top,
            InnerBottom = Inner | Bottom, InnerTopLeft = Inner | Top | Left, InnerTopRight = Inner | Top | Right,
            InnerBottomLeft = Inner | Bottom | Left, InnerBottomRight = Inner | Bottom | Right;
    }

    /// <summary>Size taken from the parent (Rich HUD DimAlignments).</summary>
    public static class UiDim
    {
        public const int None = 0, Width = 1, Height = 2, Size = 3, IgnorePadding = 4, UnpaddedWidth = 5, UnpaddedHeight = 6, UnpaddedSize = 7;
    }

    /// <summary>Sizing of chains and scroll boxes (Rich HUD HudChainSizingModes, flags).</summary>
    public static class UiSizing
    {
        public const int None = 0, ClampChainOffAxis = 4, ClampChainAlignAxis = 8, FitChainOffAxis = 16, FitChainAlignAxis = 32,
            FitChainBoth = 48, ClampMembersOffAxis = 64, ClampMembersAlignAxis = 128, FitMembersOffAxis = 256,
            FitMembersAlignAxis = 512, FitMembersBoth = 768;
    }

    /// <summary>Text alignment and font style flags.</summary>
    public static class UiText
    {
        public const int Left = 0, Center = 1, Right = 2;
        public const int Regular = 0, Bold = 1, Italic = 2, Underline = 4;
        /// <summary>Text field filters: one line, digits, number (digits, sign, point), anything.</summary>
        public const int FilterLine = 0, FilterDigits = 1, FilterNumber = 2, FilterAny = 3;
    }

    /// <summary>
    /// API 2. Windows, HUD elements, formatted text and key binds through Syntrix (Rich HUD inside).
    /// Create one per mod after the SyntrixApi, call Load in LoadData and Unload in UnloadData.
    /// Build elements in onReady (it runs again after a Rich HUD restart, when all elements were lost).
    /// Every element is a handle; the wrappers never throw and do nothing without the framework.
    /// Texts use markup: [b] [i] [u] [color=#RRGGBB] [size=1.2] [font=Name] [align=center] [link=target] ... [/tag], "[[" = "[".
    /// </summary>
    public class SyntrixUi
    {
        private readonly SyntrixApi _api;
        private readonly Dictionary<long, Action<string>> _handlers = new Dictionary<long, Action<string>>();
        private readonly Dictionary<string, BindEntry> _binds = new Dictionary<string, BindEntry>();
        private string _bindPage;
        private Action _onReady, _onLost, _onUnavailable;
        private bool _loaded;

        private struct BindEntry
        {
            public string Keys;
            public Action Pressed;
        }

        private Func<int> _state;
        private Action<Action<int, int, string>> _events;
        private Action _release;
        private Func<int, int, int> _create, _part;
        private Action<int> _remove, _clear, _focus;
        private Action<int, int> _setParent;
        private Action<int, int, bool> _listen, _setBool;
        private Action<int, int, int> _setInt;
        private Action<int, int, float> _setFloat;
        private Action<int, int, Vector2> _setVector;
        private Action<int, int, Color> _setColor;
        private Action<int, int, string> _setText;
        private Func<int, int, bool> _getBool;
        private Func<int, int, int> _getInt;
        private Func<int, int, float> _getFloat;
        private Func<int, int, Vector2> _getVector;
        private Func<int, int, Color> _getColor;
        private Func<int, int, string> _getText;
        private Func<int, string, float, Vector2> _measure;
        private Func<Vector2> _screen, _cursorPos;
        private Func<float> _resScale;
        private Action<bool> _cursor;
        private Action<string, string> _bind;
        private Func<string, string> _bindKeys;
        private Action<string> _bindPageFn;

        public SyntrixUi(SyntrixApi api) { _api = api; }

        /// <summary>
        /// Starts the UI service for this mod. onReady: build your elements; onLost: all elements are gone
        /// (onReady follows when Rich HUD is back); onUnavailable: no UI this session (Rich HUD Master missing).
        /// </summary>
        public void Load(Action onReady = null, Action onLost = null, Action onUnavailable = null)
        {
            if (_loaded || _api == null)
                return;
            _loaded = true;
            _onReady = onReady;
            _onLost = onLost;
            _onUnavailable = onUnavailable;
            _api.EndpointsChanged += Rebind;
            Rebind();
        }

        /// <summary>Removes all elements of this mod and saves its key binds.</summary>
        public void Unload()
        {
            if (!_loaded)
                return;
            _loaded = false;
            if (_release != null)
                _release();
            _api.EndpointsChanged -= Rebind;
            _handlers.Clear();
            _binds.Clear();
            _onReady = _onLost = _onUnavailable = null;
            _events = null;
            _create = null;
        }

        private T Get<T>(string key) where T : class { return _api.Endpoint<T>(key); }

        private void Rebind()
        {
            try
            {
                _state = Get<Func<int>>("Ui.State");
                _events = Get<Action<Action<int, int, string>>>("Ui.Events");
                _release = Get<Action>("Ui.Release");
                _create = Get<Func<int, int, int>>("Ui.Create");
                _remove = Get<Action<int>>("Ui.Remove");
                _clear = Get<Action<int>>("Ui.Clear");
                _setParent = Get<Action<int, int>>("Ui.SetParent");
                _part = Get<Func<int, int, int>>("Ui.Part");
                _listen = Get<Action<int, int, bool>>("Ui.Listen");
                _setBool = Get<Action<int, int, bool>>("Ui.SetBool");
                _setInt = Get<Action<int, int, int>>("Ui.SetInt");
                _setFloat = Get<Action<int, int, float>>("Ui.SetFloat");
                _setVector = Get<Action<int, int, Vector2>>("Ui.SetVector");
                _setColor = Get<Action<int, int, Color>>("Ui.SetColor");
                _setText = Get<Action<int, int, string>>("Ui.SetText");
                _getBool = Get<Func<int, int, bool>>("Ui.GetBool");
                _getInt = Get<Func<int, int, int>>("Ui.GetInt");
                _getFloat = Get<Func<int, int, float>>("Ui.GetFloat");
                _getVector = Get<Func<int, int, Vector2>>("Ui.GetVector");
                _getColor = Get<Func<int, int, Color>>("Ui.GetColor");
                _getText = Get<Func<int, int, string>>("Ui.GetText");
                _focus = Get<Action<int>>("Ui.Focus");
                _measure = Get<Func<int, string, float, Vector2>>("Ui.Measure");
                _screen = Get<Func<Vector2>>("Ui.Screen");
                _resScale = Get<Func<float>>("Ui.ResScale");
                _cursorPos = Get<Func<Vector2>>("Ui.CursorPos");
                _cursor = Get<Action<bool>>("Ui.Cursor");
                _bind = Get<Action<string, string>>("Ui.Bind");
                _bindKeys = Get<Func<string, string>>("Ui.BindKeys");
                _bindPageFn = Get<Action<string>>("Ui.BindPage");
                if (_events == null)
                    return;
                // Registrations made before the framework answered
                if (_bind != null)
                    foreach (var b in _binds)
                        _bind(b.Key, b.Value.Keys);
                if (_bindPage != null && _bindPageFn != null)
                    _bindPageFn(_bindPage);
                _events(Dispatch);
            }
            catch (Exception e) { MyLog.Default.WriteLineAndConsole(_api.ModName + ": ERROR binding Syntrix UI: " + e); }
        }

        private void Dispatch(int handle, int ev, string arg)
        {
            Action a = null;
            if (handle == 0)
            {
                switch (ev)
                {
                    case UiEvent.BindPressed:
                        BindEntry b;
                        if (arg != null && _binds.TryGetValue(arg, out b))
                            a = b.Pressed;
                        break;
                    case UiEvent.Ready: a = _onReady; break;
                    case UiEvent.Lost: _handlers.Clear(); a = _onLost; break;
                    case UiEvent.Unavailable: a = _onUnavailable; break;
                }
                if (a != null)
                    a();
                return;
            }
            Action<string> h;
            if (_handlers.TryGetValue(Key(handle, ev), out h))
                h(arg);
        }

        private static long Key(int handle, int ev) { return ((long)handle << 8) | (uint)ev; }

        // ---------------------------------------------------------------- service

        /// <summary>1 ready, 0 starting (or framework missing), -1 not available this session.</summary>
        public int State { get { return _state != null ? _state() : 0; } }

        /// <summary>True when elements can be made now.</summary>
        public bool IsAvailable { get { return State == 1; } }

        /// <summary>Screen size in pixels (zero before ready).</summary>
        public Vector2 ScreenSize { get { return _screen != null ? _screen() : Vector2.Zero; } }

        /// <summary>Scale of the high DPI root (pixels per UI unit of Root).</summary>
        public float ResScale { get { return _resScale != null ? _resScale() : 1f; } }

        /// <summary>Cursor position on the screen.</summary>
        public Vector2 CursorPosition { get { return _cursorPos != null ? _cursorPos() : Vector2.Zero; } }

        /// <summary>Shows the cursor outside of windows (open modal windows show it anyway).</summary>
        public void ShowCursor(bool on) { if (_cursor != null) _cursor(on); }

        /// <summary>Size of a markup text; styleFrom gives the base format (font size, font), wrapWidth > 0 wraps.</summary>
        public Vector2 MeasureText(string markup, float wrapWidth = 0f, UiElement styleFrom = null)
        {
            return _measure != null ? _measure(styleFrom != null ? styleFrom.Handle : 0, markup, wrapWidth) : Vector2.Zero;
        }

        /// <summary>Makes text safe for markup ("[" becomes "[[").</summary>
        public static string Escape(string text) { return text == null ? null : text.Replace("[", "[["); }

        /// <summary>
        /// Key bind of this mod, e.g. Bind("OpenWiki", "F11", Toggle) or "Control+F1". Players change it on the rebind
        /// page (BindPage); Syntrix saves the choice. Not raised while typing or in the chat. May be called before ready.
        /// </summary>
        public void Bind(string name, string keys, Action pressed)
        {
            if (string.IsNullOrEmpty(name))
                return;
            _binds[name] = new BindEntry { Keys = keys, Pressed = pressed };
            if (_bind != null)
                _bind(name, keys);
        }

        /// <summary>Current keys of a bind, e.g. "F11" (null if unknown).</summary>
        public string BindKeys(string name) { return _bindKeys != null ? _bindKeys(name) : null; }

        /// <summary>Page with this mod's binds in the Rich HUD terminal (F2). Call once, after the Bind calls.</summary>
        public void BindPage(string title)
        {
            _bindPage = title;
            if (_bindPageFn != null)
                _bindPageFn(title);
        }

        // ---------------------------------------------------------------- elements

        /// <summary>Parent for elements in scaled UI units (null as parent means the same).</summary>
        public UiElement Root { get { return new UiElement(this, 0); } }

        /// <summary>Parent in screen pixels, e.g. for HUD overlays positioned with ScreenSize.</summary>
        public UiElement PixelRoot { get { return new UiElement(this, -1); } }

        internal int Create(int kind, UiElement parent) { return _create != null ? _create(kind, parent != null ? parent.Handle : 0) : 0; }

        /// <summary>Hidden window; children made with it as parent go into its body.</summary>
        public UiWindow Window(string title = null) { var w = new UiWindow(this, Create(UiKind.Window, null)); if (title != null) w.Title = title; return w; }
        public UiPanel Panel(UiElement parent) { return new UiPanel(this, Create(UiKind.Panel, parent)); }
        public UiLabel Label(UiElement parent, string text = null) { var l = new UiLabel(this, Create(UiKind.Label, parent)); if (text != null) l.Text = text; return l; }
        public UiLabelBox LabelBox(UiElement parent, string text = null) { var l = new UiLabelBox(this, Create(UiKind.LabelBox, parent)); if (text != null) l.Text = text; return l; }
        public UiButton Button(UiElement parent, string text = null) { var b = new UiButton(this, Create(UiKind.Button, parent)); if (text != null) b.Text = text; return b; }
        public UiTextField TextField(UiElement parent) { return new UiTextField(this, Create(UiKind.TextField, parent)); }
        /// <summary>Coloured rectangle or image (texture = transparent material subtype).</summary>
        public UiBox Box(UiElement parent, string texture = null) { var b = new UiBox(this, Create(UiKind.Box, parent)); if (texture != null) b.Texture = texture; return b; }
        public UiBorder Border(UiElement parent) { return new UiBorder(this, Create(UiKind.Border, parent)); }
        public UiScrollBox ScrollBox(UiElement parent, bool vertical = true) { return new UiScrollBox(this, Create(vertical ? UiKind.ScrollBox : UiKind.ScrollBoxHorizontal, parent)); }
        public UiChain Chain(UiElement parent, bool vertical = true) { return new UiChain(this, Create(vertical ? UiKind.Chain : UiKind.ChainHorizontal, parent)); }
        /// <summary>Invisible element: anchor, spacer or group.</summary>
        public UiElement Empty(UiElement parent) { return new UiElement(this, Create(UiKind.Empty, parent)); }
        public UiElement ClickArea(UiElement parent) { return new UiElement(this, Create(UiKind.ClickArea, parent)); }
        public UiCheckBox CheckBox(UiElement parent) { return new UiCheckBox(this, Create(UiKind.CheckBox, parent)); }
        public UiSlider Slider(UiElement parent) { return new UiSlider(this, Create(UiKind.Slider, parent)); }

        // ---------------------------------------------------------------- plumbing for the wrappers

        internal void Listen(int h, int ev, Action<string> handler)
        {
            if (h == 0)
                return;
            if (handler != null)
                _handlers[Key(h, ev)] = handler;
            else
                _handlers.Remove(Key(h, ev));
            if (_listen != null)
                _listen(h, ev, handler != null);
        }

        internal void Removed(int h)
        {
            for (int ev = 1; ev <= UiEvent.Closed; ev++)
                _handlers.Remove(Key(h, ev));
        }

        internal void Remove(int h) { if (_remove != null) _remove(h); Removed(h); }
        internal void Clear(int h) { if (_clear != null) _clear(h); }
        internal void SetParent(int h, int p) { if (_setParent != null) _setParent(h, p); }
        internal int Part(int h, int part) { return _part != null ? _part(h, part) : 0; }
        internal void Focus(int h) { if (_focus != null) _focus(h); }
        internal void Set(int h, int p, bool v) { if (_setBool != null) _setBool(h, p, v); }
        internal void Set(int h, int p, int v) { if (_setInt != null) _setInt(h, p, v); }
        internal void Set(int h, int p, float v) { if (_setFloat != null) _setFloat(h, p, v); }
        internal void Set(int h, int p, Vector2 v) { if (_setVector != null) _setVector(h, p, v); }
        internal void Set(int h, int p, Color v) { if (_setColor != null) _setColor(h, p, v); }
        internal void Set(int h, int p, string v) { if (_setText != null) _setText(h, p, v); }
        internal bool GetBool(int h, int p) { return _getBool != null && _getBool(h, p); }
        internal int GetInt(int h, int p) { return _getInt != null ? _getInt(h, p) : 0; }
        internal float GetFloat(int h, int p) { return _getFloat != null ? _getFloat(h, p) : 0f; }
        internal Vector2 GetVector(int h, int p) { return _getVector != null ? _getVector(h, p) : Vector2.Zero; }
        internal Color GetColor(int h, int p) { return _getColor != null ? _getColor(h, p) : Color.Transparent; }
        internal string GetText(int h, int p) { return _getText != null ? _getText(h, p) : null; }
    }

    /// <summary>
    /// API 2. Any UI element. Handle 0 means "not made" (framework or Rich HUD missing); every call is then ignored.
    /// Use Set/Get with UiProp for properties the typed wrappers do not list.
    /// </summary>
    public class UiElement
    {
        public readonly SyntrixUi Ui;
        public readonly int Handle;

        internal UiElement(SyntrixUi ui, int handle) { Ui = ui; Handle = handle; }

        public bool IsValid { get { return Handle != 0; } }

        public void Set(int prop, bool v) { Ui.Set(Handle, prop, v); }
        public void Set(int prop, int v) { Ui.Set(Handle, prop, v); }
        public void Set(int prop, float v) { Ui.Set(Handle, prop, v); }
        public void Set(int prop, Vector2 v) { Ui.Set(Handle, prop, v); }
        public void Set(int prop, Color v) { Ui.Set(Handle, prop, v); }
        public void Set(int prop, string v) { Ui.Set(Handle, prop, v); }
        public bool GetBool(int prop) { return Ui.GetBool(Handle, prop); }
        public int GetInt(int prop) { return Ui.GetInt(Handle, prop); }
        public float GetFloat(int prop) { return Ui.GetFloat(Handle, prop); }
        public Vector2 GetVector(int prop) { return Ui.GetVector(Handle, prop); }
        public Color GetColor(int prop) { return Ui.GetColor(Handle, prop); }
        public string GetText(int prop) { return Ui.GetText(Handle, prop); }

        /// <summary>Offset from the aligned position (cheap, fine every frame).</summary>
        public Vector2 Offset { get { return GetVector(UiProp.Offset); } set { Set(UiProp.Offset, value); } }
        public Vector2 Size { get { return GetVector(UiProp.Size); } set { Set(UiProp.Size, value); } }
        public float Width { get { return GetFloat(UiProp.Width); } set { Set(UiProp.Width, value); } }
        public float Height { get { return GetFloat(UiProp.Height); } set { Set(UiProp.Height, value); } }
        public Vector2 Padding { get { return GetVector(UiProp.Padding); } set { Set(UiProp.Padding, value); } }
        /// <summary>In a scroll box this also takes the entry out of the list.</summary>
        public bool Visible { get { return GetBool(UiProp.Visible); } set { Set(UiProp.Visible, value); } }
        /// <summary>UiAlign flags.</summary>
        public int ParentAlign { get { return GetInt(UiProp.ParentAlign); } set { Set(UiProp.ParentAlign, value); } }
        /// <summary>UiDim value.</summary>
        public int DimAlign { get { return GetInt(UiProp.DimAlign); } set { Set(UiProp.DimAlign, value); } }
        public int ZOffset { get { return GetInt(UiProp.ZOffset); } set { Set(UiProp.ZOffset, value); } }
        public bool InputEnabled { get { return GetBool(UiProp.InputEnabled); } set { Set(UiProp.InputEnabled, value); } }
        /// <summary>Clip children to this element.</summary>
        public bool Masking { get { return GetBool(UiProp.Masking); } set { Set(UiProp.Masking, value); } }
        public bool IsMousedOver { get { return GetBool(UiProp.MousedOver); } }
        /// <summary>Centre on the screen (read only).</summary>
        public Vector2 Position { get { return GetVector(UiProp.Position); } }
        /// <summary>Number of children.</summary>
        public int Count { get { return GetInt(UiProp.Count); } }
        /// <summary>Tooltip (markup) for elements with mouse input.</summary>
        public string Tooltip { set { Set(UiProp.Tooltip, value); } }

        /// <summary>Removes the element and its children.</summary>
        public void Remove() { Ui.Remove(Handle); }
        /// <summary>Removes all children (e.g. the entries of a scroll box).</summary>
        public void Clear() { Ui.Clear(Handle); }
        /// <summary>Moves the element to another parent (null = root).</summary>
        public void MoveTo(UiElement parent) { Ui.SetParent(Handle, parent != null ? parent.Handle : 0); }

        /// <summary>Handler for a UiEvent of this element (one per event; null removes it). Runs outside Rich HUD's update.</summary>
        public void On(int uiEvent, Action<string> handler) { Ui.Listen(Handle, uiEvent, handler); }
        public void OnClick(Action a) { On(UiEvent.Click, a == null ? (Action<string>)null : s => a()); }
        public void OnRightClick(Action a) { On(UiEvent.RightClick, a == null ? (Action<string>)null : s => a()); }
        public void OnCursorEnter(Action a) { On(UiEvent.CursorEnter, a == null ? (Action<string>)null : s => a()); }
        public void OnCursorExit(Action a) { On(UiEvent.CursorExit, a == null ? (Action<string>)null : s => a()); }
    }

    /// <summary>API 2. Element with text and a base text format (the markup may change it per run).</summary>
    public class UiTextElement : UiElement
    {
        internal UiTextElement(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Text with markup.</summary>
        public string Text { get { return GetText(UiProp.Text); } set { Set(UiProp.Text, value); } }
        /// <summary>Text without markup (faster, "[" stays).</summary>
        public string PlainText { set { Set(UiProp.PlainText, value); } }
        public Color TextColor { get { return GetColor(UiProp.TextColor); } set { Set(UiProp.TextColor, value); } }
        public float FontSize { get { return GetFloat(UiProp.FontSize); } set { Set(UiProp.FontSize, value); } }
        /// <summary>Rich HUD font name, e.g. "BitstreamVeraSans"; null = default.</summary>
        public string Font { get { return GetText(UiProp.Font); } set { Set(UiProp.Font, value); } }
        /// <summary>UiText.Bold | Italic | Underline.</summary>
        public int FontStyle { get { return GetInt(UiProp.FontStyle); } set { Set(UiProp.FontStyle, value); } }
        /// <summary>UiText.Left, Center or Right.</summary>
        public int TextAlign { get { return GetInt(UiProp.TextAlign); } set { Set(UiProp.TextAlign, value); } }
        /// <summary>Size of the shown text (read only).</summary>
        public Vector2 TextSize { get { return GetVector(UiProp.TextSize); } }
    }

    /// <summary>API 2. Text with markup and clickable [link=target] runs.</summary>
    public class UiLabel : UiTextElement
    {
        internal UiLabel(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Size follows the text (default true).</summary>
        public bool AutoResize { get { return GetBool(UiProp.AutoResize); } set { Set(UiProp.AutoResize, value); } }
        public bool Wrap { get { return GetBool(UiProp.Wrap); } set { Set(UiProp.Wrap, value); } }
        public float WrapWidth { get { return GetFloat(UiProp.WrapWidth); } set { Set(UiProp.WrapWidth, value); } }
        public bool VertCenter { get { return GetBool(UiProp.VertCenter); } set { Set(UiProp.VertCenter, value); } }
        public Color LinkColor { get { return GetColor(UiProp.LinkColor); } set { Set(UiProp.LinkColor, value); } }

        /// <summary>Wraps at the parent's inner width minus margin, at most max (0 = no limit); follows resizing.</summary>
        public void WrapToParent(float margin, float max = 0f)
        {
            Set(UiProp.WrapMax, max);
            Set(UiProp.WrapToParent, margin);
        }

        /// <summary>Click on a link; the handler gets the target.</summary>
        public void OnLink(Action<string> handler) { On(UiEvent.Link, handler); }
    }

    /// <summary>API 2. Text on a coloured background (badges, logos).</summary>
    public class UiLabelBox : UiTextElement
    {
        internal UiLabelBox(SyntrixUi ui, int handle) : base(ui, handle) { }

        public Color Color { get { return GetColor(UiProp.Color); } set { Set(UiProp.Color, value); } }
        public Vector2 TextPadding { get { return GetVector(UiProp.TextPadding); } set { Set(UiProp.TextPadding, value); } }
        public bool AutoResize { get { return GetBool(UiProp.AutoResize); } set { Set(UiProp.AutoResize, value); } }
        public bool VertCenter { get { return GetBool(UiProp.VertCenter); } set { Set(UiProp.VertCenter, value); } }
    }

    /// <summary>API 2. Text button with highlight colour and optional border.</summary>
    public class UiButton : UiTextElement
    {
        internal UiButton(SyntrixUi ui, int handle) : base(ui, handle) { }

        public Color Color { get { return GetColor(UiProp.Color); } set { Set(UiProp.Color, value); } }
        public Color HighlightColor { get { return GetColor(UiProp.HighlightColor); } set { Set(UiProp.HighlightColor, value); } }
        public bool HighlightEnabled { get { return GetBool(UiProp.HighlightEnabled); } set { Set(UiProp.HighlightEnabled, value); } }
        public Color BorderColor { set { Set(UiProp.BorderColor, value); } }
        public float BorderThickness { get { return GetFloat(UiProp.BorderThickness); } set { Set(UiProp.BorderThickness, value); } }
        public Vector2 TextPadding { get { return GetVector(UiProp.TextPadding); } set { Set(UiProp.TextPadding, value); } }
        public bool AutoResize { get { return GetBool(UiProp.AutoResize); } set { Set(UiProp.AutoResize, value); } }
    }

    /// <summary>API 2. Surface with background, optional border, highlight colour and mouse events: rows, cards, tabs.</summary>
    public class UiPanel : UiElement
    {
        internal UiPanel(SyntrixUi ui, int handle) : base(ui, handle) { }

        public Color Color { get { return GetColor(UiProp.Color); } set { Set(UiProp.Color, value); } }
        /// <summary>Background under the cursor; also turns on mouse input.</summary>
        public Color HighlightColor { get { return GetColor(UiProp.HighlightColor); } set { Set(UiProp.HighlightColor, value); } }
        public Color BorderColor { set { Set(UiProp.BorderColor, value); } }
        public float BorderThickness { get { return GetFloat(UiProp.BorderThickness); } set { Set(UiProp.BorderThickness, value); } }
        /// <summary>Background texture (transparent material subtype).</summary>
        public string Texture { get { return GetText(UiProp.Texture); } set { Set(UiProp.Texture, value); } }
        /// <summary>Background area as its own element (children drawn behind the panel's content).</summary>
        public UiElement Background { get { return new UiElement(Ui, Ui.Part(Handle, 1)); } }
        /// <summary>Raised when the size changed (also once after creation).</summary>
        public void OnResized(Action a) { On(UiEvent.Resized, a == null ? (Action<string>)null : s => a()); }
    }

    /// <summary>API 2. Coloured rectangle or image.</summary>
    public class UiBox : UiElement
    {
        internal UiBox(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Tint (white shows the texture as it is).</summary>
        public Color Color { get { return GetColor(UiProp.Color); } set { Set(UiProp.Color, value); } }
        /// <summary>Transparent material subtype; null = plain colour.</summary>
        public string Texture { get { return GetText(UiProp.Texture); } set { Set(UiProp.Texture, value); } }
        /// <summary>Pixel size of the whole texture (for the aspect ratio and regions).</summary>
        public Vector2 TextureSize { get { return GetVector(UiProp.TextureSize); } set { Set(UiProp.TextureSize, value); } }
        /// <summary>Rich HUD MaterialAlignment: 0 stretch, 1 fit vertical, 2 fit horizontal, 3 fit auto.</summary>
        public int TextureAlign { get { return GetInt(UiProp.TextureAlign); } set { Set(UiProp.TextureAlign, value); } }

        /// <summary>Shows only a part of the texture (atlas); size zero = whole texture.</summary>
        public void TextureRegion(Vector2 offset, Vector2 size)
        {
            Set(UiProp.TextureOffset, offset);
            Set(UiProp.TextureRegion, size);
        }
    }

    /// <summary>API 2. Frame line.</summary>
    public class UiBorder : UiElement
    {
        internal UiBorder(SyntrixUi ui, int handle) : base(ui, handle) { }

        public Color Color { get { return GetColor(UiProp.Color); } set { Set(UiProp.Color, value); } }
        public float Thickness { get { return GetFloat(UiProp.BorderThickness); } set { Set(UiProp.BorderThickness, value); } }
    }

    /// <summary>API 2. One-line text entry: click to type, Enter/Escape/click elsewhere stops.</summary>
    public class UiTextField : UiTextElement
    {
        internal UiTextField(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Shown while empty (markup).</summary>
        public string Placeholder { get { return GetText(UiProp.Placeholder); } set { Set(UiProp.Placeholder, value); } }
        public Color Color { get { return GetColor(UiProp.Color); } set { Set(UiProp.Color, value); } }
        public Color BorderColor { set { Set(UiProp.BorderColor, value); } }
        public Color FocusColor { set { Set(UiProp.FocusColor, value); } }
        public Color HighlightColor { set { Set(UiProp.HighlightColor, value); } }
        public Vector2 TextPadding { get { return GetVector(UiProp.TextPadding); } set { Set(UiProp.TextPadding, value); } }
        /// <summary>Longest text, 0 = no limit.</summary>
        public int MaxLength { get { return GetInt(UiProp.MaxLength); } set { Set(UiProp.MaxLength, value); } }
        /// <summary>UiText.FilterLine, FilterDigits, FilterNumber or FilterAny.</summary>
        public int CharFilter { get { return GetInt(UiProp.CharFilter); } set { Set(UiProp.CharFilter, value); } }
        /// <summary>True while the player types; set to start or stop typing.</summary>
        public bool Typing { get { return GetBool(UiProp.InputOpen); } set { Set(UiProp.InputOpen, value); } }

        /// <summary>Every change while typing; the handler gets the text.</summary>
        public void OnTextChanged(Action<string> handler) { On(UiEvent.TextChanged, handler); }
        /// <summary>Enter pressed; the handler gets the text.</summary>
        public void OnSubmit(Action<string> handler) { On(UiEvent.Submit, handler); }
    }

    /// <summary>API 2. Scrolling list; children become entries. Hidden children leave the list.</summary>
    public class UiScrollBox : UiElement
    {
        internal UiScrollBox(SyntrixUi ui, int handle) : base(ui, handle) { }

        public Color Color { get { return GetColor(UiProp.Color); } set { Set(UiProp.Color, value); } }
        public float Spacing { get { return GetFloat(UiProp.Spacing); } set { Set(UiProp.Spacing, value); } }
        /// <summary>UiSizing flags (default FitMembersOffAxis: entries take the full width).</summary>
        public int Sizing { get { return GetInt(UiProp.Sizing); } set { Set(UiProp.Sizing, value); } }
        /// <summary>First shown entry; 0 scrolls to the top.</summary>
        public int Start { get { return GetInt(UiProp.Start); } set { Set(UiProp.Start, value); } }
        public int End { get { return GetInt(UiProp.End); } }
        public int EnabledCount { get { return GetInt(UiProp.EnabledCount); } }
        public bool SmoothScrolling { set { Set(UiProp.SmoothScrolling, value); } }
        public bool Scrolling { set { Set(UiProp.Scrolling, value); } }
        public Color BarColor { set { Set(UiProp.BarColor, value); } }
        public Color BarHighlight { set { Set(UiProp.BarHighlight, value); } }
        public Color SliderColor { set { Set(UiProp.SliderColor, value); } }
        public Color SliderHighlight { set { Set(UiProp.SliderHighlight, value); } }
        public Color DividerColor { set { Set(UiProp.DividerColor, value); } }
        /// <summary>Background part: parent for overlays that are no entries (e.g. an empty-list hint).</summary>
        public UiElement Background { get { return new UiElement(Ui, Ui.Part(Handle, 1)); } }
    }

    /// <summary>API 2. Stack of members, vertical or horizontal.</summary>
    public class UiChain : UiElement
    {
        internal UiChain(SyntrixUi ui, int handle) : base(ui, handle) { }

        public float Spacing { get { return GetFloat(UiProp.Spacing); } set { Set(UiProp.Spacing, value); } }
        /// <summary>UiSizing flags (default FitChainBoth: the chain takes the size of its members).</summary>
        public int Sizing { get { return GetInt(UiProp.Sizing); } set { Set(UiProp.Sizing, value); } }
        public Vector2 MemberMinSize { set { Set(UiProp.MemberMinSize, value); } }
        public Vector2 MemberMaxSize { set { Set(UiProp.MemberMaxSize, value); } }
    }

    /// <summary>API 2. Window: header (Part Header) and body (children go there). Starts hidden.</summary>
    public class UiWindow : UiTextElement
    {
        internal UiWindow(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Header text (markup).</summary>
        public string Title { get { return GetText(UiProp.Title); } set { Set(UiProp.Title, value); } }
        public Color HeaderColor { get { return GetColor(UiProp.HeaderColor); } set { Set(UiProp.HeaderColor, value); } }
        public Color BodyColor { get { return GetColor(UiProp.BodyColor); } set { Set(UiProp.BodyColor, value); } }
        public Color BorderColor { set { Set(UiProp.BorderColor, value); } }
        public float HeaderHeight { get { return GetFloat(UiProp.HeaderHeight); } set { Set(UiProp.HeaderHeight, value); } }
        public Vector2 MinSize { get { return GetVector(UiProp.MinSize); } set { Set(UiProp.MinSize, value); } }
        public bool Draggable { get { return GetBool(UiProp.Draggable); } set { Set(UiProp.Draggable, value); } }
        public bool Resizable { get { return GetBool(UiProp.Resizable); } set { Set(UiProp.Resizable, value); } }
        /// <summary>Hide the game HUD while open (default false).</summary>
        public bool HideHud { get { return GetBool(UiProp.HideHud); } set { Set(UiProp.HideHud, value); } }
        /// <summary>Keep the view still while open (default true).</summary>
        public bool LockView { get { return GetBool(UiProp.LockView); } set { Set(UiProp.LockView, value); } }
        /// <summary>Escape closes the focused window (default true).</summary>
        public bool CloseOnEscape { get { return GetBool(UiProp.CloseOnEscape); } set { Set(UiProp.CloseOnEscape, value); } }
        /// <summary>Cursor on, mouse and camera kept from the game while open (default true).</summary>
        public bool Modal { get { return GetBool(UiProp.Modal); } set { Set(UiProp.Modal, value); } }
        public bool Focused { get { return GetBool(UiProp.Focused); } }
        public bool IsOpen { get { return Visible; } }

        /// <summary>Header bar (a button-like label box: Color, Height, children for your own header content).</summary>
        public UiElement Header { get { return new UiElement(Ui, Ui.Part(Handle, 1)); } }
        /// <summary>Body area below the header.</summary>
        public UiElement Body { get { return new UiElement(Ui, Ui.Part(Handle, 2)); } }

        public void Open() { Visible = true; }
        public void Close() { Visible = false; }
        public void Toggle() { Visible = !Visible; }
        /// <summary>Brings the open window to the front.</summary>
        public void Focus() { Ui.Focus(Handle); }

        /// <summary>The window was hidden (Escape, Close, Visible = false).</summary>
        public void OnClosed(Action a) { On(UiEvent.Closed, a == null ? (Action<string>)null : s => a()); }
        /// <summary>The body size changed (player resized the window, or first layout).</summary>
        public void OnResized(Action a) { On(UiEvent.Resized, a == null ? (Action<string>)null : s => a()); }
    }

    /// <summary>API 2. Check box.</summary>
    public class UiCheckBox : UiElement
    {
        internal UiCheckBox(SyntrixUi ui, int handle) : base(ui, handle) { }

        public bool Checked { get { return GetBool(UiProp.Checked); } set { Set(UiProp.Checked, value); } }
        public Color Color { set { Set(UiProp.Color, value); } }
        public Color BorderColor { set { Set(UiProp.BorderColor, value); } }
        public Color TickColor { set { Set(UiProp.TickColor, value); } }
        public void OnChanged(Action<bool> handler) { On(UiEvent.ValueChanged, handler == null ? (Action<string>)null : s => handler(s == "1")); }
    }

    /// <summary>API 2. Slider between Min and Max.</summary>
    public class UiSlider : UiElement
    {
        internal UiSlider(SyntrixUi ui, int handle) : base(ui, handle) { }

        public float Min { get { return GetFloat(UiProp.Min); } set { Set(UiProp.Min, value); } }
        public float Max { get { return GetFloat(UiProp.Max); } set { Set(UiProp.Max, value); } }
        public float Value { get { return GetFloat(UiProp.Value); } set { Set(UiProp.Value, value); } }
        public Color Color { set { Set(UiProp.Color, value); } }
        public Color BarColor { set { Set(UiProp.BarColor, value); } }
        public Color SliderColor { set { Set(UiProp.SliderColor, value); } }
        public void OnChanged(Action<float> handler) { On(UiEvent.ValueChanged, handler == null ? (Action<string>)null : s => handler(Value)); }
    }
}
