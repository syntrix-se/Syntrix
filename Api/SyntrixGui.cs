// Syntrix window dock file (API version 3, Syntrix 1.2.0). Optional: copy it next to SyntrixApi.cs only if your mod
// shows windows or HUD overlays. Do not edit it; take a newer copy from the Syntrix repository instead.
// Syntrix draws everything in the Vanilla style with the game's own screen API; no other mod is needed.
// Deliberate exception from the one-type-per-file rule: a dock file is copied as a whole, so it holds
// several small types.
//
// MIT License - Copyright (c) 2026 Syntrix contributors
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files, to deal in the Software without restriction,
// subject to including this notice. Full text: LICENSE in the Syntrix repository.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND.
using System;
using System.Collections.Generic;
using System.Globalization;
using VRage.Utils;
using VRageMath;

namespace Syntrix.Api
{
    /// <summary>Element kinds (API 3). Values are fixed.</summary>
    public static class GuiKind
    {
        /// <summary>Window in the game's screen stack, with mouse cursor, Esc and close button.</summary>
        public const int Window = 1;
        /// <summary>Layer on the screen without a window (no mouse); hides itself with the HUD and in menus.</summary>
        public const int Overlay = 2;
        /// <summary>Area: colour or Vanilla background, optional border, holds other elements.</summary>
        public const int Panel = 3;
        /// <summary>Lays its children out one below the other (or side by side).</summary>
        public const int Stack = 4;
        /// <summary>Scroll area with scroll bar; its children are laid out one below the other.</summary>
        public const int Scroll = 5;
        /// <summary>Text with wrapping and markup.</summary>
        public const int Label = 6;
        /// <summary>Button.</summary>
        public const int Button = 7;
        /// <summary>Image or icon.</summary>
        public const int Image = 8;
        /// <summary>Text field with keyboard input.</summary>
        public const int TextField = 9;
        /// <summary>List of items with icons and selection.</summary>
        public const int List = 10;
        /// <summary>Tree of items that can be expanded.</summary>
        public const int Tree = 11;
        /// <summary>Table with header and rows.</summary>
        public const int Table = 12;
        /// <summary>Check box with text.</summary>
        public const int CheckBox = 13;
        /// <summary>Slider.</summary>
        public const int Slider = 14;
        /// <summary>Progress bar.</summary>
        public const int ProgressBar = 15;
        /// <summary>Row of tabs.</summary>
        public const int Tabs = 16;
        /// <summary>Drawing area: the mod draws into it from a callback (for special cases only).</summary>
        public const int Canvas = 17;
        /// <summary>Drop-down selection.</summary>
        public const int Dropdown = 18;
    }

    /// <summary>Properties (API 3). Values are fixed. Sizes and offsets are reference pixels (screen 1080 high).</summary>
    public static class GuiProp
    {
        public const int Visible = 1, Enabled = 2, Offset = 3, Size = 4, Anchor = 5, FillWidth = 6, FillHeight = 7, Padding = 8;
        public const int Color = 9, BorderColor = 10, BorderWidth = 11, Tooltip = 12, Text = 13, TextColor = 14, TextScale = 15;
        public const int Font = 16, Align = 17, Wrap = 18, Markup = 19, Value = 20, Min = 21, Max = 22, Step = 23, Checked = 24;
        public const int Selected = 25, Material = 26, Spacing = 27, Horizontal = 28, ScrollPosition = 29, Columns = 30;
        public const int RowHeight = 31, Title = 32, Pause = 33, EscapeCloses = 34, CloseButton = 35, BackgroundFade = 36;
        public const int MaxLength = 37, Placeholder = 38, Focused = 39, Style = 40, IconSize = 41, Widths = 42, Header = 43;
        public const int ScrollMax = 44, ContentSize = 45, ScreenPosition = 46, ScreenSize = 47, IsOpen = 48, Position = 49;
        public const int Clip = 50, Hover = 51, Format = 52, MouseMove = 53, SelectedKey = 54, Layer = 55, WindowSize = 56;
        public const int Multiline = 57, ShowInMenus = 58, TextHeight = 59, ScrollToEnd = 60;
    }

    /// <summary>Events (API 3). Values are fixed.</summary>
    public static class GuiEvent
    {
        /// <summary>index = mouse button (0 left, 1 right); lists: text = item key.</summary>
        public const int Click = 1;
        /// <summary>Changed by the player; text = new value (slider "R" format, check box "1"/"0").</summary>
        public const int Changed = 2;
        /// <summary>Enter in a text field; text = text.</summary>
        public const int Submit = 3;
        /// <summary>Item chosen; index = item, text = key.</summary>
        public const int Select = 4;
        /// <summary>Tree item expanded or collapsed; index = item, text "1" or "0".</summary>
        public const int Expand = 5;
        /// <summary>Link clicked; text = target.</summary>
        public const int Link = 6;
        public const int Opened = 7;
        /// <summary>Window closed; index 1 = session unloading.</summary>
        public const int Closed = 8;
        /// <summary>Esc on a window with EscapeCloses off.</summary>
        public const int Cancel = 9;
        /// <summary>index = item (lists) or -1, text = key.</summary>
        public const int DoubleClick = 10;
        /// <summary>Resolution or UI scale changed.</summary>
        public const int Resized = 11;
        /// <summary>Canvas: index 0 down, 1 up, 2 move, 3 wheel; text "x;y;button|delta" in reference pixels.</summary>
        public const int Mouse = 12;
        /// <summary>Text field lost the keyboard; text = text.</summary>
        public const int FocusLost = 13;
    }

    /// <summary>Values of <see cref="GuiProp.Style"/>.</summary>
    public static class GuiStyleId
    {
        public const int Default = 0;
        /// <summary>Panel: Vanilla dark area; button: red (danger).</summary>
        public const int Dark = 1;
        /// <summary>Panel: dark area with border; button: without outline.</summary>
        public const int Border = 2;
        /// <summary>Panel: list background; button: flat text button (link look).</summary>
        public const int List = 3;
        /// <summary>Panel: no background.</summary>
        public const int None = 4;
        /// <summary>Button: toggle.</summary>
        public const int Toggle = 5;
    }

    /// <summary>Item properties for <see cref="GuiNode.ItemSet"/>.</summary>
    public static class GuiItemProp
    {
        public const int Text = 1, Icons = 2, Enabled = 3, Expanded = 4, Extra = 5, Color = 6, Tooltip = 7, Badge = 8;
    }

    /// <summary>Anchor values (position inside the parent).</summary>
    public static class GuiAnchor
    {
        public const int TopLeft = 0, Top = 1, TopRight = 2, Left = 3, Centre = 4, Right = 5, BottomLeft = 6, Bottom = 7, BottomRight = 8;
    }

    /// <summary>
    /// Window service of Syntrix (API 3). Make one per mod after <see cref="SyntrixApi.Load"/>; build windows when
    /// <see cref="IsAvailable"/> is true (in onReady or later). Everything is safe to call without the framework.
    /// Markup in texts: [b] [i] [u] [color=#RRGGBB] [size=1.2] [font=White] [align=center] [link=target] … [/…];
    /// "[[" is a literal "[".
    /// </summary>
    public sealed class SyntrixGui
    {
        /// <summary>Separator of table cells.</summary>
        public const char CellSeparator = '\u001f';

        private readonly SyntrixApi _api;
        private readonly Dictionary<int, GuiNode> _nodes = new Dictionary<int, GuiNode>();

        internal Func<int> State_;
        internal Action<Action<int, int, int, string>> Events_;
        internal Action Release_;
        internal Func<int, int, int> Create_;
        internal Action<int> Remove_, Clear_, Close_, Focus_, ItemClear_;
        internal Action<int, int> SetParent_, ItemRemove_, Expand_, ScrollTo_;
        internal Action<int, int, bool> SetBool_;
        internal Action<int, int, int> SetInt_;
        internal Action<int, int, float> SetFloat_;
        internal Action<int, int, Vector2> SetVector_;
        internal Action<int, int, Color> SetColor_;
        internal Action<int, int, string> SetText_;
        internal Func<int, int, bool> GetBool_;
        internal Func<int, int, int> GetInt_;
        internal Func<int, int, float> GetFloat_;
        internal Func<int, int, Vector2> GetVector_;
        internal Func<int, int, Color> GetColor_;
        internal Func<int, int, string> GetText_;
        internal Func<int, string, string, string, string, int> ItemAdd_;
        internal Action<int, int, int, string> ItemSet_;
        internal Func<int, int> ItemCount_;
        internal Func<int, string, int> ItemFind_;
        internal Func<int, bool> Open_;
        internal Action<int, Action<Vector4, float>> SetDraw_;
        private Func<string, string, float, Vector2> _measure;
        private Func<Vector2> _screen, _cursor;
        private Func<float> _scale;
        private Func<string, string, string, string, Action<bool>, bool> _confirm;
        private Func<int> _windowCount;

        /// <summary>Runs when the service became available or went away (Syntrix loaded or unloaded).</summary>
        public event Action AvailabilityChanged;

        public SyntrixGui(SyntrixApi api)
        {
            _api = api;
            api.EndpointsChanged += Bind;
            Bind();
        }

        private void Bind()
        {
            bool before = IsAvailable;
            State_ = _api.Endpoint<Func<int>>("Gui.State");
            Events_ = _api.Endpoint<Action<Action<int, int, int, string>>>("Gui.Events");
            Release_ = _api.Endpoint<Action>("Gui.Release");
            Create_ = _api.Endpoint<Func<int, int, int>>("Gui.Create");
            Remove_ = _api.Endpoint<Action<int>>("Gui.Remove");
            Clear_ = _api.Endpoint<Action<int>>("Gui.Clear");
            SetParent_ = _api.Endpoint<Action<int, int>>("Gui.SetParent");
            SetBool_ = _api.Endpoint<Action<int, int, bool>>("Gui.SetBool");
            SetInt_ = _api.Endpoint<Action<int, int, int>>("Gui.SetInt");
            SetFloat_ = _api.Endpoint<Action<int, int, float>>("Gui.SetFloat");
            SetVector_ = _api.Endpoint<Action<int, int, Vector2>>("Gui.SetVector");
            SetColor_ = _api.Endpoint<Action<int, int, Color>>("Gui.SetColor");
            SetText_ = _api.Endpoint<Action<int, int, string>>("Gui.SetText");
            GetBool_ = _api.Endpoint<Func<int, int, bool>>("Gui.GetBool");
            GetInt_ = _api.Endpoint<Func<int, int, int>>("Gui.GetInt");
            GetFloat_ = _api.Endpoint<Func<int, int, float>>("Gui.GetFloat");
            GetVector_ = _api.Endpoint<Func<int, int, Vector2>>("Gui.GetVector");
            GetColor_ = _api.Endpoint<Func<int, int, Color>>("Gui.GetColor");
            GetText_ = _api.Endpoint<Func<int, int, string>>("Gui.GetText");
            ItemAdd_ = _api.Endpoint<Func<int, string, string, string, string, int>>("Gui.ItemAdd");
            ItemSet_ = _api.Endpoint<Action<int, int, int, string>>("Gui.ItemSet");
            ItemRemove_ = _api.Endpoint<Action<int, int>>("Gui.ItemRemove");
            ItemClear_ = _api.Endpoint<Action<int>>("Gui.ItemClear");
            ItemCount_ = _api.Endpoint<Func<int, int>>("Gui.ItemCount");
            ItemFind_ = _api.Endpoint<Func<int, string, int>>("Gui.ItemFind");
            Expand_ = _api.Endpoint<Action<int, int>>("Gui.Expand");
            ScrollTo_ = _api.Endpoint<Action<int, int>>("Gui.ScrollTo");
            Open_ = _api.Endpoint<Func<int, bool>>("Gui.Open");
            Close_ = _api.Endpoint<Action<int>>("Gui.Close");
            Focus_ = _api.Endpoint<Action<int>>("Gui.Focus");
            SetDraw_ = _api.Endpoint<Action<int, Action<Vector4, float>>>("Gui.SetDraw");
            _measure = _api.Endpoint<Func<string, string, float, Vector2>>("Gui.Measure");
            _screen = _api.Endpoint<Func<Vector2>>("Gui.Screen");
            _scale = _api.Endpoint<Func<float>>("Gui.Scale");
            _cursor = _api.Endpoint<Func<Vector2>>("Gui.Cursor");
            _confirm = _api.Endpoint<Func<string, string, string, string, Action<bool>, bool>>("Gui.Confirm");
            _windowCount = _api.Endpoint<Func<int>>("Gui.WindowCount");
            if (Events_ != null)
                Events_(OnEvent);
            else
                _nodes.Clear();
            if (before != IsAvailable && AvailabilityChanged != null)
            {
                try { AvailabilityChanged(); }
                catch (Exception e) { MyLog.Default.WriteLineAndConsole(_api.ModName + ": ERROR in Syntrix GUI availability handler: " + e); }
            }
        }

        /// <summary>1 ready, -1 not available (dedicated server or Syntrix older than 1.2), 0 not docked yet.</summary>
        public int State { get { return State_ != null ? State_() : _api.IsReady ? -1 : 0; } }

        /// <summary>Windows can be made (player with Syntrix 1.2 or newer).</summary>
        public bool IsAvailable { get { return State == 1; } }

        private void OnEvent(int handle, int ev, int index, string text)
        {
            GuiNode n;
            if (_nodes.TryGetValue(handle, out n))
                n.Raise(ev, index, text);
        }

        internal T Make<T>(T node, int kind, GuiNode parent) where T : GuiNode
        {
            node.Gui = this;
            node.Handle = Create_ != null ? Create_(kind, parent != null ? parent.Handle : 0) : 0;
            if (node.Handle > 0)
                _nodes[node.Handle] = node;
            return node;
        }

        internal void Forget(GuiNode n)
        {
            _nodes.Remove(n.Handle);
            foreach (var c in n.ChildList)
                Forget(c);
        }

        /// <summary>New window (closed; call Open). Default size is the Vanilla terminal size.</summary>
        public GuiWindow Window(string title = null)
        {
            var w = Make(new GuiWindow(), GuiKind.Window, null);
            if (title != null)
                w.Title = title;
            return w;
        }

        /// <summary>New overlay on the whole screen (visible at once; children by anchor).</summary>
        public GuiOverlay Overlay()
        {
            return Make(new GuiOverlay(), GuiKind.Overlay, null);
        }

        /// <summary>Removes all windows and overlays of this mod.</summary>
        public void Release()
        {
            if (Release_ != null)
                Release_();
            _nodes.Clear();
        }

        /// <summary>Small window with a question; answer(true) for yes. Returns false if it could not open.</summary>
        public bool Confirm(string title, string text, string yes, string no, Action<bool> answer)
        {
            return _confirm != null && _confirm(title, text, yes, no, answer);
        }

        /// <summary>Text size in reference pixels.</summary>
        public Vector2 Measure(string text, float scale = 0.8f, string font = null)
        {
            return _measure != null ? _measure(font, text, scale) : Vector2.Zero;
        }

        /// <summary>Screen size in reference pixels (height 1080).</summary>
        public Vector2 ScreenSize { get { return _screen != null ? _screen() : new Vector2(1920f, 1080f); } }

        /// <summary>Screen pixels per reference pixel.</summary>
        public float Scale { get { return _scale != null ? _scale() : 1f; } }

        /// <summary>Mouse cursor in reference pixels, (-1, -1) without cursor.</summary>
        public Vector2 Cursor { get { return _cursor != null ? _cursor() : new Vector2(-1f, -1f); } }

        /// <summary>Open Syntrix windows of all mods (the game allows four mod windows at once).</summary>
        public int WindowCount { get { return _windowCount != null ? _windowCount() : 0; } }

        /// <summary>Escapes a text for markup.</summary>
        public static string Escape(string text) { return string.IsNullOrEmpty(text) ? text : text.Replace("[", "[["); }

        /// <summary>Colour as "#RRGGBBAA" for item colours.</summary>
        public static string Hex(Color c) { return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2") + c.A.ToString("X2"); }
    }

    /// <summary>Base of all elements. Calls do nothing once the element was removed or without the framework.</summary>
    public class GuiNode
    {
        internal SyntrixGui Gui;
        internal int Handle;
        internal GuiNode ParentNode;
        internal readonly List<GuiNode> ChildList = new List<GuiNode>();

        /// <summary>Every event: (event, index, text). The typed events below are raised as well.</summary>
        public event Action<int, int, string> Event;
        /// <summary>Click with the mouse button (0 left, 1 right).</summary>
        public event Action<int> Click;
        /// <summary>Changed by the player; new value as text.</summary>
        public event Action<string> Changed;
        /// <summary>Link clicked; target.</summary>
        public event Action<string> Link;

        /// <summary>The element exists in Syntrix.</summary>
        public bool IsValid { get { return Handle > 0 && Gui != null; } }

        public GuiNode Parent { get { return ParentNode; } }

        internal virtual void Raise(int ev, int index, string text)
        {
            try
            {
                if (Event != null) Event(ev, index, text);
                if (ev == GuiEvent.Click && Click != null) Click(index);
                else if (ev == GuiEvent.Changed && Changed != null) Changed(text);
                else if (ev == GuiEvent.Link && Link != null) Link(text);
                RaiseTyped(ev, index, text);
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole("Syntrix GUI handler: " + e);
            }
        }

        internal virtual void RaiseTyped(int ev, int index, string text) { }

        // ---------- tree ----------

        private T Child<T>(T node, int kind) where T : GuiNode
        {
            if (Gui == null)
                return node;
            Gui.Make(node, kind, this);
            node.ParentNode = this;
            ChildList.Add(node);
            return node;
        }

        public GuiPanel AddPanel(int style = GuiStyleId.Default) { var n = Child(new GuiPanel(), GuiKind.Panel); if (style != 0) n.Style = style; return n; }
        public GuiStack AddStack(bool horizontal = false, float spacing = 6f) { var n = Child(new GuiStack(), GuiKind.Stack); if (horizontal) n.Horizontal = true; if (spacing != 6f) n.Spacing = spacing; return n; }
        public GuiScroll AddScroll() { return Child(new GuiScroll(), GuiKind.Scroll); }
        public GuiLabel AddLabel(string text = null) { var n = Child(new GuiLabel(), GuiKind.Label); if (text != null) n.Text = text; return n; }
        public GuiButton AddButton(string text = null, Action onClick = null)
        {
            var n = Child(new GuiButton(), GuiKind.Button);
            if (text != null) n.Text = text;
            if (onClick != null) n.Click += b => { if (b == 0) onClick(); };
            return n;
        }
        public GuiImage AddImage(string material = null) { var n = Child(new GuiImage(), GuiKind.Image); if (material != null) n.Material = material; return n; }
        public GuiTextField AddTextField(string text = null) { var n = Child(new GuiTextField(), GuiKind.TextField); if (text != null) n.Text = text; return n; }
        public GuiList AddList() { return Child(new GuiList(), GuiKind.List); }
        public GuiTree AddTree() { return Child(new GuiTree(), GuiKind.Tree); }
        public GuiTable AddTable() { return Child(new GuiTable(), GuiKind.Table); }
        public GuiCheckBox AddCheckBox(string text = null) { var n = Child(new GuiCheckBox(), GuiKind.CheckBox); if (text != null) n.Text = text; return n; }
        public GuiSlider AddSlider(float min = 0f, float max = 1f, float value = 0f) { var n = Child(new GuiSlider(), GuiKind.Slider); n.Min = min; n.Max = max; n.Value = value; return n; }
        public GuiProgressBar AddProgressBar() { return Child(new GuiProgressBar(), GuiKind.ProgressBar); }
        public GuiTabs AddTabs() { return Child(new GuiTabs(), GuiKind.Tabs); }
        public GuiDropdown AddDropdown() { return Child(new GuiDropdown(), GuiKind.Dropdown); }
        /// <summary>Drawing area for special cases: draw(rect in screen pixels x, y, w, h; alpha) with MyScreenSpaceSprite.DrawPixels and MyScreenSpaceText.</summary>
        public GuiCanvas AddCanvas(Action<Vector4, float> draw = null) { var n = Child(new GuiCanvas(), GuiKind.Canvas); if (draw != null) n.SetDraw(draw); return n; }

        /// <summary>Removes the element and its children.</summary>
        public void Remove()
        {
            if (!IsValid)
                return;
            if (Gui.Remove_ != null) Gui.Remove_(Handle);
            Gui.Forget(this);
            if (ParentNode != null) ParentNode.ChildList.Remove(this);
            Handle = 0;
        }

        /// <summary>Removes all children.</summary>
        public void Clear()
        {
            if (!IsValid)
                return;
            if (Gui.Clear_ != null) Gui.Clear_(Handle);
            foreach (var c in ChildList)
            {
                Gui.Forget(c);
                c.Handle = 0;
            }
            ChildList.Clear();
        }

        /// <summary>Moves the element to another parent (null = detach, kept for later).</summary>
        public void MoveTo(GuiNode parent)
        {
            if (!IsValid || Gui.SetParent_ == null)
                return;
            Gui.SetParent_(Handle, parent != null ? parent.Handle : 0);
            if (ParentNode != null) ParentNode.ChildList.Remove(this);
            ParentNode = parent;
            if (parent != null) parent.ChildList.Add(this);
        }

        // ---------- raw properties ----------

        public void SetBool(int prop, bool v) { if (IsValid && Gui.SetBool_ != null) Gui.SetBool_(Handle, prop, v); }
        public void SetInt(int prop, int v) { if (IsValid && Gui.SetInt_ != null) Gui.SetInt_(Handle, prop, v); }
        public void SetFloat(int prop, float v) { if (IsValid && Gui.SetFloat_ != null) Gui.SetFloat_(Handle, prop, v); }
        public void SetVector(int prop, Vector2 v) { if (IsValid && Gui.SetVector_ != null) Gui.SetVector_(Handle, prop, v); }
        public void SetColor(int prop, Color v) { if (IsValid && Gui.SetColor_ != null) Gui.SetColor_(Handle, prop, v); }
        public void SetText(int prop, string v) { if (IsValid && Gui.SetText_ != null) Gui.SetText_(Handle, prop, v); }
        public bool GetBool(int prop) { return IsValid && Gui.GetBool_ != null && Gui.GetBool_(Handle, prop); }
        public int GetInt(int prop) { return IsValid && Gui.GetInt_ != null ? Gui.GetInt_(Handle, prop) : 0; }
        public float GetFloat(int prop) { return IsValid && Gui.GetFloat_ != null ? Gui.GetFloat_(Handle, prop) : 0f; }
        public Vector2 GetVector(int prop) { return IsValid && Gui.GetVector_ != null ? Gui.GetVector_(Handle, prop) : Vector2.Zero; }
        public Color GetColor(int prop) { return IsValid && Gui.GetColor_ != null ? Gui.GetColor_(Handle, prop) : Color.Transparent; }
        public string GetText(int prop) { return IsValid && Gui.GetText_ != null ? Gui.GetText_(Handle, prop) : null; }

        // ---------- common properties ----------

        public bool Visible { get { return GetBool(GuiProp.Visible); } set { SetBool(GuiProp.Visible, value); } }
        public bool Enabled { get { return GetBool(GuiProp.Enabled); } set { SetBool(GuiProp.Enabled, value); } }
        /// <summary>Offset from the anchored position, reference pixels.</summary>
        public Vector2 Offset { get { return GetVector(GuiProp.Offset); } set { SetVector(GuiProp.Offset, value); } }
        /// <summary>Size in reference pixels; 0 in a direction = automatic.</summary>
        public Vector2 Size { get { return GetVector(GuiProp.Size); } set { SetVector(GuiProp.Size, value); } }
        /// <summary>Position inside the parent, see <see cref="GuiAnchor"/>.</summary>
        public int Anchor { get { return GetInt(GuiProp.Anchor); } set { SetInt(GuiProp.Anchor, value); } }
        public bool FillWidth { get { return GetBool(GuiProp.FillWidth); } set { SetBool(GuiProp.FillWidth, value); } }
        public bool FillHeight { get { return GetBool(GuiProp.FillHeight); } set { SetBool(GuiProp.FillHeight, value); } }
        public Vector2 Padding { get { return GetVector(GuiProp.Padding); } set { SetVector(GuiProp.Padding, value); } }
        /// <summary>Background colour (panels) or tint.</summary>
        public Color Color { get { return GetColor(GuiProp.Color); } set { SetColor(GuiProp.Color, value); } }
        public Color BorderColor { get { return GetColor(GuiProp.BorderColor); } set { SetColor(GuiProp.BorderColor, value); } }
        public float BorderWidth { get { return GetFloat(GuiProp.BorderWidth); } set { SetFloat(GuiProp.BorderWidth, value); } }
        /// <summary>Tooltip with markup, null = none.</summary>
        public string Tooltip { get { return GetText(GuiProp.Tooltip); } set { SetText(GuiProp.Tooltip, value); } }
        /// <summary>Font name, inherited by the children; null = Vanilla.</summary>
        public string Font { get { return GetText(GuiProp.Font); } set { SetText(GuiProp.Font, value); } }
        public Color TextColor { get { return GetColor(GuiProp.TextColor); } set { SetColor(GuiProp.TextColor, value); } }
        /// <summary>Text size, 0.8 = Vanilla.</summary>
        public float TextScale { get { return GetFloat(GuiProp.TextScale); } set { SetFloat(GuiProp.TextScale, value); } }
        /// <summary>0 left, 1 centre, 2 right.</summary>
        public int Align { get { return GetInt(GuiProp.Align); } set { SetInt(GuiProp.Align, value); } }
        /// <summary>See <see cref="GuiStyleId"/>.</summary>
        public int Style { get { return GetInt(GuiProp.Style); } set { SetInt(GuiProp.Style, value); } }
        public bool Clip { get { return GetBool(GuiProp.Clip); } set { SetBool(GuiProp.Clip, value); } }
        /// <summary>Mouse is over the element.</summary>
        public bool Hover { get { return GetBool(GuiProp.Hover); } }
        /// <summary>Top left on the screen, reference pixels (after layout).</summary>
        public Vector2 ScreenPosition { get { return GetVector(GuiProp.ScreenPosition); } }
        /// <summary>Size on the screen, reference pixels (after layout).</summary>
        public Vector2 ScreenSize { get { return GetVector(GuiProp.ScreenSize); } }

        /// <summary>Gives this element the keyboard (text fields).</summary>
        public void Focus() { if (IsValid && Gui.Focus_ != null) Gui.Focus_(Handle); }

        // ---------- items (list, tree, table, tabs, drop-down) ----------

        /// <summary>Adds an item; icons = materials separated by ';'. Returns its index.</summary>
        public int ItemAdd(string key, string text, string icons = null, string parentKey = null)
        {
            return IsValid && Gui.ItemAdd_ != null ? Gui.ItemAdd_(Handle, key, parentKey, text, icons) : -1;
        }
        /// <summary>Changes an item property (<see cref="GuiItemProp"/>).</summary>
        public void ItemSet(int index, int prop, string value) { if (IsValid && Gui.ItemSet_ != null) Gui.ItemSet_(Handle, index, prop, value); }
        public void ItemRemove(int index) { if (IsValid && Gui.ItemRemove_ != null) Gui.ItemRemove_(Handle, index); }
        public void ItemClear() { if (IsValid && Gui.ItemClear_ != null) Gui.ItemClear_(Handle); }
        public int ItemCount { get { return IsValid && Gui.ItemCount_ != null ? Gui.ItemCount_(Handle) : 0; } }
        public int ItemFind(string key) { return IsValid && Gui.ItemFind_ != null ? Gui.ItemFind_(Handle, key) : -1; }
    }

    /// <summary>Elements with a text.</summary>
    public class GuiTextNode : GuiNode
    {
        public string Text { get { return GetText(GuiProp.Text); } set { SetText(GuiProp.Text, value); } }
    }

    /// <summary>Elements with items and a selection.</summary>
    public class GuiItemsNode : GuiNode
    {
        /// <summary>Item chosen by the player: (index, key).</summary>
        public event Action<int, string> Selected;
        /// <summary>Double click on an item: (index, key).</summary>
        public event Action<int, string> DoubleClicked;

        internal override void RaiseTyped(int ev, int index, string text)
        {
            if (ev == GuiEvent.Select && Selected != null) Selected(index, text);
            else if (ev == GuiEvent.DoubleClick && DoubleClicked != null) DoubleClicked(index, text);
        }

        /// <summary>Selected item, -1 = none (setting it raises no event).</summary>
        public int Selection { get { return GetInt(GuiProp.Selected); } set { SetInt(GuiProp.Selected, value); } }
        public string SelectedKey { get { return GetText(GuiProp.SelectedKey); } set { SetText(GuiProp.SelectedKey, value); } }
        /// <summary>Row height in reference pixels, 0 = automatic.</summary>
        public float RowHeight { get { return GetFloat(GuiProp.RowHeight); } set { SetFloat(GuiProp.RowHeight, value); } }
        public Vector2 IconSize { get { return GetVector(GuiProp.IconSize); } set { SetVector(GuiProp.IconSize, value); } }
        public float ScrollPosition { get { return GetFloat(GuiProp.ScrollPosition); } set { SetFloat(GuiProp.ScrollPosition, value); } }
    }

    /// <summary>Window. Size and Position are normalized GUI coordinates (Vanilla terminal 1.0157 x 0.9172).</summary>
    public sealed class GuiWindow : GuiNode
    {
        public event Action Opened;
        /// <summary>Closed; true while the session unloads.</summary>
        public event Action<bool> Closed;
        /// <summary>Esc while <see cref="EscapeCloses"/> is off.</summary>
        public event Action Cancelled;
        public event Action Resized;

        internal override void RaiseTyped(int ev, int index, string text)
        {
            if (ev == GuiEvent.Opened && Opened != null) Opened();
            else if (ev == GuiEvent.Closed && Closed != null) Closed(index == 1);
            else if (ev == GuiEvent.Cancel && Cancelled != null) Cancelled();
            else if (ev == GuiEvent.Resized && Resized != null) Resized();
        }

        public string Title { get { return GetText(GuiProp.Title); } set { SetText(GuiProp.Title, value); } }
        /// <summary>Pauses the game in single player while open (set before Open).</summary>
        public bool Pause { get { return GetBool(GuiProp.Pause); } set { SetBool(GuiProp.Pause, value); } }
        public bool EscapeCloses { get { return GetBool(GuiProp.EscapeCloses); } set { SetBool(GuiProp.EscapeCloses, value); } }
        public bool CloseButton { get { return GetBool(GuiProp.CloseButton); } set { SetBool(GuiProp.CloseButton, value); } }
        public bool BackgroundFade { get { return GetBool(GuiProp.BackgroundFade); } set { SetBool(GuiProp.BackgroundFade, value); } }
        /// <summary>Normalized size; Vector2.Zero = Vanilla terminal size.</summary>
        public Vector2 WindowSize { get { return GetVector(GuiProp.WindowSize); } set { SetVector(GuiProp.WindowSize, value); } }
        /// <summary>Normalized centre, (0.5, 0.5) = middle.</summary>
        public Vector2 Position { get { return GetVector(GuiProp.Position); } set { SetVector(GuiProp.Position, value); } }
        /// <summary>Content area below the title, reference pixels.</summary>
        public Vector2 ContentSize { get { return GetVector(GuiProp.ContentSize); } }
        public bool IsOpen { get { return GetBool(GuiProp.IsOpen); } }
        /// <summary>The window has the input focus.</summary>
        public bool HasFocus { get { return GetBool(GuiProp.Focused); } }

        /// <summary>Opens the window; false if the game refused (four mod windows open, no session).</summary>
        public bool Open() { return IsValid && Gui.Open_ != null && Gui.Open_(Handle); }
        public void Close() { if (IsValid && Gui.Close_ != null) Gui.Close_(Handle); }
    }

    /// <summary>Layer on the whole screen without mouse. Hidden with the HUD and in menus unless ShowInMenus.</summary>
    public sealed class GuiOverlay : GuiNode
    {
        public bool ShowInMenus { get { return GetBool(GuiProp.ShowInMenus); } set { SetBool(GuiProp.ShowInMenus, value); } }
        /// <summary>0 below the HUD, 1 above the HUD, 2 above everything.</summary>
        public int Layer { get { return GetInt(GuiProp.Layer); } set { SetInt(GuiProp.Layer, value); } }
    }

    /// <summary>Area with colour or Vanilla background (Style), children placed by anchor.</summary>
    public sealed class GuiPanel : GuiNode { }

    /// <summary>Children one below the other (or side by side).</summary>
    public class GuiStack : GuiNode
    {
        public bool Horizontal { get { return GetBool(GuiProp.Horizontal); } set { SetBool(GuiProp.Horizontal, value); } }
        /// <summary>Space between children, reference pixels.</summary>
        public float Spacing { get { return GetFloat(GuiProp.Spacing); } set { SetFloat(GuiProp.Spacing, value); } }
    }

    /// <summary>Scroll area: children one below the other, Vanilla scroll bar.</summary>
    public sealed class GuiScroll : GuiStack
    {
        public float ScrollPosition { get { return GetFloat(GuiProp.ScrollPosition); } set { SetFloat(GuiProp.ScrollPosition, value); } }
        public float ScrollMax { get { return GetFloat(GuiProp.ScrollMax); } }
        /// <summary>Stays at the end when the content grows (logs, chat).</summary>
        public bool ScrollToEnd { set { SetBool(GuiProp.ScrollToEnd, value); } }
        /// <summary>Scrolls so that the element is visible.</summary>
        public void ScrollTo(GuiNode element) { if (IsValid && element != null && Gui.ScrollTo_ != null) Gui.ScrollTo_(Handle, element.Handle); }
    }

    /// <summary>Text with wrapping and markup; links raise <see cref="GuiNode.Link"/>.</summary>
    public sealed class GuiLabel : GuiTextNode
    {
        public bool Wrap { get { return GetBool(GuiProp.Wrap); } set { SetBool(GuiProp.Wrap, value); } }
        public bool Markup { get { return GetBool(GuiProp.Markup); } set { SetBool(GuiProp.Markup, value); } }
        public float TextHeight { get { return GetFloat(GuiProp.TextHeight); } }
    }

    /// <summary>Vanilla button; Style Dark = red, List = flat link, Toggle = stays pressed (Checked).</summary>
    public sealed class GuiButton : GuiTextNode
    {
        /// <summary>Icon material shown left of the text (or alone).</summary>
        public string Material { get { return GetText(GuiProp.Material); } set { SetText(GuiProp.Material, value); } }
        public Vector2 IconSize { get { return GetVector(GuiProp.IconSize); } set { SetVector(GuiProp.IconSize, value); } }
        public bool Checked { get { return GetBool(GuiProp.Checked); } set { SetBool(GuiProp.Checked, value); } }
    }

    /// <summary>Image: material name, texture path (.dds) or definition id ("MyObjectBuilder_Ore/Iron"); several separated by ';' are layered.</summary>
    public sealed class GuiImage : GuiNode
    {
        public string Material { get { return GetText(GuiProp.Material); } set { SetText(GuiProp.Material, value); } }
    }

    /// <summary>Text field; Changed on every key, Submit on Enter.</summary>
    public sealed class GuiTextField : GuiTextNode
    {
        public event Action<string> Submitted;
        public event Action<string> FocusLost;

        internal override void RaiseTyped(int ev, int index, string text)
        {
            if (ev == GuiEvent.Submit && Submitted != null) Submitted(text);
            else if (ev == GuiEvent.FocusLost && FocusLost != null) FocusLost(text);
        }

        public string Placeholder { get { return GetText(GuiProp.Placeholder); } set { SetText(GuiProp.Placeholder, value); } }
        public int MaxLength { get { return GetInt(GuiProp.MaxLength); } set { SetInt(GuiProp.MaxLength, value); } }
        public bool Multiline { get { return GetBool(GuiProp.Multiline); } set { SetBool(GuiProp.Multiline, value); } }
        public bool Focused { get { return GetBool(GuiProp.Focused); } set { SetBool(GuiProp.Focused, value); } }
    }

    /// <summary>List with icons, second text (Extra), badge and selection; Columns &gt; 1 gives tiles.</summary>
    public sealed class GuiList : GuiItemsNode
    {
        public int Columns { get { return GetInt(GuiProp.Columns); } set { SetInt(GuiProp.Columns, value); } }
    }

    /// <summary>Tree: items with a parent key appear below their parent.</summary>
    public sealed class GuiTree : GuiItemsNode
    {
        /// <summary>Item expanded or collapsed by the player: (index, expanded).</summary>
        public event Action<int, bool> Expanded;

        internal override void RaiseTyped(int ev, int index, string text)
        {
            base.RaiseTyped(ev, index, text);
            if (ev == GuiEvent.Expand && Expanded != null) Expanded(index, text == "1");
        }

        /// <summary>Expands all parents of the item so it is visible.</summary>
        public void Reveal(int index) { if (IsValid && Gui.Expand_ != null) Gui.Expand_(Handle, index); }
    }

    /// <summary>Table: rows are items whose text holds the cells separated by <see cref="SyntrixGui.CellSeparator"/>.</summary>
    public sealed class GuiTable : GuiItemsNode
    {
        /// <summary>Header cells, null = no header.</summary>
        public void SetHeader(params string[] cells) { SetText(GuiProp.Header, cells == null || cells.Length == 0 ? null : string.Join(SyntrixGui.CellSeparator.ToString(), cells)); }
        /// <summary>Relative column widths, e.g. 2, 1, 1.</summary>
        public void SetWidths(params float[] widths)
        {
            if (widths == null || widths.Length == 0) { SetText(GuiProp.Widths, null); return; }
            var parts = new string[widths.Length];
            for (int i = 0; i < widths.Length; i++)
                parts[i] = widths[i].ToString(CultureInfo.InvariantCulture);
            SetText(GuiProp.Widths, string.Join("|", parts));
        }
        /// <summary>Adds a row; cells may hold markup and links.</summary>
        public int AddRow(string key, params string[] cells) { return ItemAdd(key, string.Join(SyntrixGui.CellSeparator.ToString(), cells ?? new string[0])); }
    }

    /// <summary>Vanilla check box with text; Changed gives "1" or "0".</summary>
    public sealed class GuiCheckBox : GuiTextNode
    {
        public bool Checked { get { return GetBool(GuiProp.Checked); } set { SetBool(GuiProp.Checked, value); } }
    }

    /// <summary>Vanilla slider; Changed gives the value.</summary>
    public sealed class GuiSlider : GuiTextNode
    {
        public float Value { get { return GetFloat(GuiProp.Value); } set { SetFloat(GuiProp.Value, value); } }
        public float Min { get { return GetFloat(GuiProp.Min); } set { SetFloat(GuiProp.Min, value); } }
        public float Max { get { return GetFloat(GuiProp.Max); } set { SetFloat(GuiProp.Max, value); } }
        /// <summary>Raster, 0 = free.</summary>
        public float Step { get { return GetFloat(GuiProp.Step); } set { SetFloat(GuiProp.Step, value); } }
        /// <summary>Format of the value text, e.g. "0.0" or "0 %"; null = no value text.</summary>
        public string Format { get { return GetText(GuiProp.Format); } set { SetText(GuiProp.Format, value); } }
    }

    /// <summary>Progress bar 0..1; Color is the fill.</summary>
    public sealed class GuiProgressBar : GuiTextNode
    {
        public float Value { get { return GetFloat(GuiProp.Value); } set { SetFloat(GuiProp.Value, value); } }
    }

    /// <summary>Row of tabs; Selected when the player picks one.</summary>
    public sealed class GuiTabs : GuiItemsNode { }

    /// <summary>Drop-down selection; Selected when the player picks an entry.</summary>
    public sealed class GuiDropdown : GuiItemsNode { }

    /// <summary>
    /// Drawing area for special cases (things Syntrix has no element for). The callback gets the area in screen pixels
    /// (x, y, width, height) and the window alpha; draw with MyScreenSpaceSprite.DrawPixels / MyScreenSpaceText.
    /// Syntrix clips to the area. Mouse: event Mouse.
    /// </summary>
    public sealed class GuiCanvas : GuiNode
    {
        /// <summary>Mouse on the area: (kind 0 down, 1 up, 2 move, 3 wheel; position in reference pixels; button or wheel delta).</summary>
        public event Action<int, Vector2, int> Mouse;

        internal override void RaiseTyped(int ev, int index, string text)
        {
            if (ev != GuiEvent.Mouse || Mouse == null || text == null)
                return;
            var p = text.Split(';');
            float x, y;
            int extra;
            if (p.Length >= 3 && float.TryParse(p[0], NumberStyles.Float, CultureInfo.InvariantCulture, out x)
                && float.TryParse(p[1], NumberStyles.Float, CultureInfo.InvariantCulture, out y)
                && int.TryParse(p[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out extra))
                Mouse(index, new Vector2(x, y), extra);
        }

        /// <summary>Also report mouse moves (event Mouse with kind 2).</summary>
        public bool ReportMoves { set { SetBool(GuiProp.MouseMove, value); } }

        public void SetDraw(Action<Vector4, float> draw) { if (IsValid && Gui.SetDraw_ != null) Gui.SetDraw_(Handle, draw); }
    }
}
