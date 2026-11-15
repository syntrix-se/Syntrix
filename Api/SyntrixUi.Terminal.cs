// Syntrix UI dock file, terminal part (API version 2, Syntrix 1.1.0). Copy it together with SyntrixUi.cs.
// Do not edit it. Holds the pages and controls of the Rich HUD terminal (F2 menu, "Syntrix" entry).
// Deliberate exception from the one-type-per-file rule: a dock file is copied as a whole.
//
// MIT License - Copyright (c) 2026 Syntrix contributors
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files, to deal in the Software without restriction,
// subject to including this notice. Full text: LICENSE in the Syntrix repository.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND.
using System;
using VRageMath;

namespace Syntrix.Api
{
    public partial class SyntrixUi
    {
        /// <summary>
        /// Page category in the Rich HUD terminal: groups the pages of a mod. All Syntrix pages are under the
        /// terminal entry of Syntrix; a category per mod keeps them apart. Rich HUD cannot remove terminal
        /// members, Remove and Visible = false disable them.
        /// </summary>
        /// <param name="name">Category name.</param>
        public UiTerminalCategory TerminalCategory(string name) { var c = new UiTerminalCategory(this, Create(UiKind.TerminalCategory, null)); if (name != null) c.Text = name; return c; }

        /// <summary>Control page at the top level of the terminal (prefer a page in a TerminalCategory).</summary>
        /// <param name="name">Page name.</param>
        public UiControlPage ControlPage(string name) { var p = new UiControlPage(this, Create(UiKind.ControlPage, null)); if (name != null) p.Text = name; return p; }

        /// <summary>Text page at the top level of the terminal (prefer a page in a TerminalCategory).</summary>
        /// <param name="name">Page name.</param>
        public UiTextPage TextPage(string name) { var p = new UiTextPage(this, Create(UiKind.TextPage, null)); if (name != null) p.Text = name; return p; }

        /// <summary>Opens the Rich HUD terminal.</summary>
        public void OpenTerminal() { Terminal(0); }

        /// <summary>Opens the Rich HUD terminal on a page.</summary>
        /// <param name="page">A control page or text page of this mod.</param>
        public void OpenTerminal(UiElement page) { if (page != null && page.Handle > 0) Terminal(page.Handle); }

        /// <summary>Closes the Rich HUD terminal.</summary>
        public void CloseTerminal() { Terminal(-1); }

        /// <summary>Opens or closes the Rich HUD terminal.</summary>
        public void ToggleTerminal() { Terminal(-2); }

        /// <summary>True while the Rich HUD terminal is open.</summary>
        public bool IsTerminalOpen { get { return TerminalOpen; } }
    }

    /// <summary>API 2. Base of terminal members: Text is the name, Visible enables or disables it.</summary>
    public class UiTerminalElement : UiTextElement
    {
        internal UiTerminalElement(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Shown in the terminal (same as Visible).</summary>
        public bool Enabled { get { return GetBool(UiProp.Enabled); } set { Set(UiProp.Enabled, value); } }
    }

    /// <summary>API 2. Page category of the Rich HUD terminal.</summary>
    public class UiTerminalCategory : UiTerminalElement
    {
        internal UiTerminalCategory(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Adds a control page.</summary>
        /// <param name="name">Page name.</param>
        public UiControlPage ControlPage(string name) { var p = new UiControlPage(Ui, Ui.Create(UiKind.ControlPage, this)); if (name != null) p.Text = name; return p; }

        /// <summary>Adds a text page.</summary>
        /// <param name="name">Page name.</param>
        public UiTextPage TextPage(string name) { var p = new UiTextPage(Ui, Ui.Create(UiKind.TextPage, this)); if (name != null) p.Text = name; return p; }
    }

    /// <summary>API 2. Terminal page with control categories.</summary>
    public class UiControlPage : UiTerminalElement
    {
        internal UiControlPage(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Adds a control category (a row with header and tiles).</summary>
        /// <param name="header">Header text.</param>
        /// <param name="subHeader">Text below the header.</param>
        public UiControlCategory Category(string header, string subHeader = null)
        {
            var c = new UiControlCategory(Ui, Ui.Create(UiKind.ControlCategory, this));
            if (header != null) c.Text = header;
            if (subHeader != null) c.SubHeader = subHeader;
            return c;
        }

        /// <summary>Opens the terminal on this page.</summary>
        public void Open() { Ui.OpenTerminal(this); }
    }

    /// <summary>API 2. Terminal page with a header, a sub-header and a text.</summary>
    public class UiTextPage : UiTerminalElement
    {
        internal UiTextPage(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Text below the header (markup).</summary>
        public string SubHeader { get { return GetText(UiProp.SubHeader); } set { Set(UiProp.SubHeader, value); } }

        /// <summary>Page text (markup).</summary>
        public string Body { get { return GetText(UiProp.BodyText); } set { Set(UiProp.BodyText, value); } }

        /// <summary>Opens the terminal on this page.</summary>
        public void Open() { Ui.OpenTerminal(this); }
    }

    /// <summary>API 2. Row of a control page: header, sub-header and tiles.</summary>
    public class UiControlCategory : UiTerminalElement
    {
        internal UiControlCategory(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Text below the header.</summary>
        public string SubHeader { get { return GetText(UiProp.SubHeader); } set { Set(UiProp.SubHeader, value); } }

        /// <summary>Adds a tile (a column of up to three controls).</summary>
        public UiControlTile Tile() { return new UiControlTile(Ui, Ui.Create(UiKind.ControlTile, this)); }
    }

    /// <summary>API 2. Column of terminal controls in a control category.</summary>
    public class UiControlTile : UiTerminalElement
    {
        internal UiControlTile(SyntrixUi ui, int handle) : base(ui, handle) { }

        private T Make<T>(int kind, string name, Func<SyntrixUi, int, T> make) where T : UiTerminalControl
        {
            var c = make(Ui, Ui.Create(kind, this));
            if (name != null) c.Text = name;
            return c;
        }

        /// <summary>Button.</summary>
        /// <param name="name">Button text.</param>
        public UiTerminalButton Button(string name) { return Make(UiKind.TerminalButton, name, (u, h) => new UiTerminalButton(u, h)); }

        /// <summary>Check box.</summary>
        /// <param name="name">Name.</param>
        public UiTerminalCheckBox CheckBox(string name) { return Make(UiKind.TerminalCheckBox, name, (u, h) => new UiTerminalCheckBox(u, h)); }

        /// <summary>On/off buttons.</summary>
        /// <param name="name">Name.</param>
        public UiTerminalCheckBox OnOffButton(string name) { return Make(UiKind.TerminalOnOffButton, name, (u, h) => new UiTerminalCheckBox(u, h)); }

        /// <summary>RGB colour picker.</summary>
        /// <param name="name">Name.</param>
        public UiTerminalColorPicker ColorPicker(string name) { return Make(UiKind.TerminalColorPicker, name, (u, h) => new UiTerminalColorPicker(u, h)); }

        /// <summary>Drag box: the player places a box on the screen (e.g. a HUD position).</summary>
        /// <param name="name">Name.</param>
        public UiTerminalDragBox DragBox(string name) { return Make(UiKind.TerminalDragBox, name, (u, h) => new UiTerminalDragBox(u, h)); }

        /// <summary>Drop-down; fill it with Add.</summary>
        /// <param name="name">Name.</param>
        public UiTerminalList Dropdown(string name) { return Make(UiKind.TerminalDropdown, name, (u, h) => new UiTerminalList(u, h)); }

        /// <summary>List; fill it with Add.</summary>
        /// <param name="name">Name.</param>
        public UiTerminalList List(string name) { return Make(UiKind.TerminalList, name, (u, h) => new UiTerminalList(u, h)); }

        /// <summary>Text line.</summary>
        /// <param name="name">Text.</param>
        public UiTerminalControl Label(string name) { return Make(UiKind.TerminalLabel, name, (u, h) => new UiTerminalControl(u, h)); }

        /// <summary>Slider; set ValueText in OnChanged to show the value.</summary>
        /// <param name="name">Name.</param>
        /// <param name="min">Smallest value.</param>
        /// <param name="max">Largest value.</param>
        public UiTerminalSlider Slider(string name, float min = 0f, float max = 1f)
        {
            var s = Make(UiKind.TerminalSlider, name, (u, h) => new UiTerminalSlider(u, h));
            s.Min = min;
            s.Max = max;
            return s;
        }

        /// <summary>Text field.</summary>
        /// <param name="name">Name.</param>
        public UiTerminalTextField TextField(string name) { return Make(UiKind.TerminalTextField, name, (u, h) => new UiTerminalTextField(u, h)); }
    }

    /// <summary>API 2. Terminal control: Text is the name, Tooltip the hover text.</summary>
    public class UiTerminalControl : UiTerminalElement
    {
        internal UiTerminalControl(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>The player changed the control; the handler gets the raw argument (see UiEvent.ValueChanged).</summary>
        /// <param name="handler">Gets the argument; null removes it.</param>
        public void OnChangedRaw(Action<string> handler) { On(UiEvent.ValueChanged, handler); }
    }

    /// <summary>API 2. Terminal button.</summary>
    public class UiTerminalButton : UiTerminalControl
    {
        internal UiTerminalButton(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>The player pressed the button.</summary>
        /// <param name="a">Handler; null removes it.</param>
        public void OnPressed(Action a) { OnClick(a); }
    }

    /// <summary>API 2. Terminal check box or on/off buttons.</summary>
    public class UiTerminalCheckBox : UiTerminalControl
    {
        internal UiTerminalCheckBox(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>The value.</summary>
        public bool Value { get { return GetBool(UiProp.Checked); } set { Set(UiProp.Checked, value); } }

        /// <summary>The player changed the value.</summary>
        /// <param name="handler">Gets the new value; null removes it.</param>
        public void OnChanged(Action<bool> handler) { On(UiEvent.ValueChanged, handler == null ? (Action<string>)null : s => handler(s == "1")); }
    }

    /// <summary>API 2. Terminal colour picker.</summary>
    public class UiTerminalColorPicker : UiTerminalControl
    {
        internal UiTerminalColorPicker(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>The colour.</summary>
        public Color Value { get { return GetColor(UiProp.ValueColor); } set { Set(UiProp.ValueColor, value); } }

        /// <summary>The player changed the colour.</summary>
        /// <param name="handler">Gets the colour; null removes it.</param>
        public void OnChanged(Action<Color> handler) { On(UiEvent.ValueChanged, handler == null ? (Action<string>)null : s => handler(SyntrixUi.ParseColor(s))); }
    }

    /// <summary>API 2. Terminal drag box.</summary>
    public class UiTerminalDragBox : UiTerminalControl
    {
        internal UiTerminalDragBox(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Position on the screen.</summary>
        public Vector2 Value { get { return GetVector(UiProp.ValueVector); } set { Set(UiProp.ValueVector, value); } }

        /// <summary>Size of the dragged box.</summary>
        public Vector2 BoxSize { get { return GetVector(UiProp.BoxSize); } set { Set(UiProp.BoxSize, value); } }

        /// <summary>Snap to the screen edge.</summary>
        public bool AlignToEdge { get { return GetBool(UiProp.AlignToEdge); } set { Set(UiProp.AlignToEdge, value); } }

        /// <summary>The player moved the box.</summary>
        /// <param name="handler">Gets the new position; null removes it.</param>
        public void OnChanged(Action<Vector2> handler) { On(UiEvent.ValueChanged, handler == null ? (Action<string>)null : s => handler(Value)); }
    }

    /// <summary>API 2. Terminal drop-down or list. Every entry has a key and a text with markup.</summary>
    public class UiTerminalList : UiTerminalControl
    {
        internal UiTerminalList(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Appends an entry; returns its index or -1.</summary>
        /// <param name="key">Key of the entry.</param>
        /// <param name="text">Text (markup).</param>
        public int Add(string key, string text) { return Ui.ItemInsert(Handle, -1, key, text); }

        /// <summary>Inserts an entry; returns its index or -1.</summary>
        /// <param name="index">Position.</param>
        /// <param name="key">Key of the entry.</param>
        /// <param name="text">Text (markup).</param>
        public int Insert(int index, string key, string text) { return Ui.ItemInsert(Handle, index, key, text); }

        /// <summary>Removes an entry.</summary>
        /// <param name="index">Entry index.</param>
        public void RemoveAt(int index) { if (index >= 0) Ui.ItemRemove(Handle, index); }

        /// <summary>Removes all entries.</summary>
        public void ClearItems() { Ui.ItemRemove(Handle, -1); }

        /// <summary>Replaces the text of an entry.</summary>
        /// <param name="index">Entry index.</param>
        /// <param name="text">Text (markup).</param>
        public void SetItemText(int index, string text) { Ui.ItemText(Handle, index, text); }

        /// <summary>Shows or hides an entry.</summary>
        /// <param name="index">Entry index.</param>
        /// <param name="enabled">True to show it.</param>
        public void SetItemEnabled(int index, bool enabled) { Ui.ItemEnabled(Handle, index, enabled); }

        /// <summary>Key of an entry, null outside the list.</summary>
        /// <param name="index">Entry index.</param>
        public string KeyAt(int index) { return Ui.ItemKey(Handle, index); }

        /// <summary>Index of the first entry with this key, -1 if none.</summary>
        /// <param name="key">The key.</param>
        public int IndexOf(string key) { return Ui.ItemFind(Handle, key); }

        /// <summary>Number of entries.</summary>
        public int ItemCount { get { return GetInt(UiProp.ItemCount); } }

        /// <summary>Selected index, -1 if none.</summary>
        public int SelectedIndex { get { return GetInt(UiProp.SelectedIndex); } set { Set(UiProp.SelectedIndex, value); } }

        /// <summary>Key of the selected entry; set to select by key.</summary>
        public string SelectedKey { get { return GetText(UiProp.SelectedKey); } set { Set(UiProp.SelectedKey, value); } }

        /// <summary>The player selected an entry; the handler gets its key.</summary>
        /// <param name="handler">Gets the key; null removes it.</param>
        public void OnSelected(Action<string> handler) { On(UiEvent.ValueChanged, handler); }
    }

    /// <summary>API 2. Terminal slider.</summary>
    public class UiTerminalSlider : UiTerminalControl
    {
        internal UiTerminalSlider(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Smallest value.</summary>
        public float Min { get { return GetFloat(UiProp.Min); } set { Set(UiProp.Min, value); } }

        /// <summary>Largest value.</summary>
        public float Max { get { return GetFloat(UiProp.Max); } set { Set(UiProp.Max, value); } }

        /// <summary>Current value.</summary>
        public float Value { get { return GetFloat(UiProp.Value); } set { Set(UiProp.Value, value); } }

        /// <summary>Value as 0..1.</summary>
        public float Percent { get { return GetFloat(UiProp.Percent); } set { Set(UiProp.Percent, value); } }

        /// <summary>Text shown next to the name (plain text).</summary>
        public string ValueText { get { return GetText(UiProp.ValueText); } set { Set(UiProp.ValueText, value); } }

        /// <summary>The player moved the slider.</summary>
        /// <param name="handler">Gets the new value; null removes it.</param>
        public void OnChanged(Action<float> handler) { On(UiEvent.ValueChanged, handler == null ? (Action<string>)null : s => handler(Value)); }
    }

    /// <summary>API 2. Terminal text field.</summary>
    public class UiTerminalTextField : UiTerminalControl
    {
        internal UiTerminalTextField(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>The text.</summary>
        public string Value { get { return GetText(UiProp.BodyText); } set { Set(UiProp.BodyText, value); } }

        /// <summary>UiText.FilterLine, FilterDigits, FilterNumber or FilterAny.</summary>
        public int CharFilter { set { Set(UiProp.CharFilter, value); } }

        /// <summary>The player changed the text.</summary>
        /// <param name="handler">Gets the text; null removes it.</param>
        public void OnChanged(Action<string> handler) { On(UiEvent.ValueChanged, handler); }
    }
}
