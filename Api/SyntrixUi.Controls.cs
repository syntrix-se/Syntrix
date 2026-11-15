// Syntrix UI dock file, controls part (API version 2, Syntrix 1.1.0). Copy it together with SyntrixUi.cs.
// Do not edit it. Holds the factories and wrappers of the Rich HUD controls, HUD spaces and shapes.
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
        /// <summary>Multi-line text editor with selection, clipboard and caret.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        /// <param name="text">Text (markup).</param>
        public UiTextBox TextBox(UiElement parent, string text = null) { var t = new UiTextBox(this, Create(UiKind.TextBox, parent)); if (text != null) t.Text = text; return t; }

        /// <summary>Drop-down list; fill it with Add.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        public UiDropdown Dropdown(UiElement parent) { return new UiDropdown(this, Create(UiKind.Dropdown, parent)); }

        /// <summary>List box with scrolling; fill it with Add.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        public UiListBox ListBox(UiElement parent) { return new UiListBox(this, Create(UiKind.ListBox, parent)); }

        /// <summary>Collapsible list with a header; fill it with Add.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        /// <param name="header">Header text (markup).</param>
        public UiTreeList TreeList(UiElement parent, string header = null) { var t = new UiTreeList(this, Create(UiKind.TreeList, parent)); if (header != null) t.Text = header; return t; }

        /// <summary>Collapsible tree node; its children (labels, tree lists, tree boxes, text boxes, label buttons) are the entries.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        /// <param name="header">Header text (markup).</param>
        public UiTreeBox TreeBox(UiElement parent, string header = null) { var t = new UiTreeBox(this, Create(UiKind.TreeBox, parent)); if (header != null) t.Text = header; return t; }

        /// <summary>Colour picker with sliders.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        /// <param name="name">Name shown left (markup).</param>
        /// <param name="hsv">True for hue, saturation and value sliders instead of red, green and blue.</param>
        public UiColorPicker ColorPicker(UiElement parent, string name = null, bool hsv = false) { var c = new UiColorPicker(this, Create(hsv ? UiKind.ColorPickerHsv : UiKind.ColorPickerRgb, parent)); if (name != null) c.Text = name; return c; }

        /// <summary>Two buttons "on" and "off".</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        public UiOnOffButton OnOffButton(UiElement parent) { return new UiOnOffButton(this, Create(UiKind.OnOffButton, parent)); }

        /// <summary>Check box with a name.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        /// <param name="name">Name (markup).</param>
        public UiNamedCheckBox NamedCheckBox(UiElement parent, string name = null) { var c = new UiNamedCheckBox(this, Create(UiKind.NamedCheckBox, parent)); if (name != null) c.Text = name; return c; }

        /// <summary>On/off buttons with a name.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        /// <param name="name">Name (markup).</param>
        public UiNamedOnOffButton NamedOnOffButton(UiElement parent, string name = null) { var c = new UiNamedOnOffButton(this, Create(UiKind.NamedOnOffButton, parent)); if (name != null) c.Text = name; return c; }

        /// <summary>Slider with a name and a value text.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        /// <param name="name">Name (markup).</param>
        public UiNamedSlider NamedSlider(UiElement parent, string name = null) { var c = new UiNamedSlider(this, Create(UiKind.NamedSlider, parent)); if (name != null) c.Text = name; return c; }

        /// <summary>Scroll bar for own scrolling content.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        public UiScrollBar ScrollBar(UiElement parent) { return new UiScrollBar(this, Create(UiKind.ScrollBar, parent)); }

        /// <summary>Bare slider bar (progress bars, own sliders).</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        public UiSliderBar SliderBar(UiElement parent) { return new UiSliderBar(this, Create(UiKind.SliderBar, parent)); }

        /// <summary>Clickable image with highlight.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        /// <param name="texture">Transparent material subtype.</param>
        public UiIconButton IconButton(UiElement parent, string texture = null) { var b = new UiIconButton(this, Create(UiKind.IconButton, parent)); if (texture != null) b.Texture = texture; return b; }

        /// <summary>Clickable text without background.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        /// <param name="text">Text (markup).</param>
        public UiLabelButton LabelButton(UiElement parent, string text = null) { var b = new UiLabelButton(this, Create(UiKind.LabelButton, parent)); if (text != null) b.Text = text; return b; }

        /// <summary>Text button in the Rich HUD terminal style (border, focus colours).</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        /// <param name="text">Text (markup).</param>
        public UiBorderedButton BorderedButton(UiElement parent, string text = null) { var b = new UiBorderedButton(this, Create(UiKind.BorderedButton, parent)); if (text != null) b.Text = text; return b; }

        /// <summary>Label box with a left and a right text.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        /// <param name="left">Left text (markup).</param>
        /// <param name="right">Right text (markup).</param>
        public UiDoubleLabelBox DoubleLabelBox(UiElement parent, string left = null, string right = null)
        {
            var d = new UiDoubleLabelBox(this, Create(UiKind.DoubleLabelBox, parent));
            if (left != null) d.Text = left;
            if (right != null) d.RightText = right;
            return d;
        }

        /// <summary>Radial (pie) selection; its children are the slots.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        public UiRadialSelection RadialSelection(UiElement parent) { return new UiRadialSelection(this, Create(UiKind.RadialSelection, parent)); }

        /// <summary>HUD space in front of the camera (rotated and moved with RotationAxis, RotationAngle, TransformOffset).</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        public UiSpace CamSpace(UiElement parent) { return new UiSpace(this, Create(UiKind.CamSpace, parent)); }

        /// <summary>HUD space at a world matrix (SetMatrix): HUD elements on a block, a grid or a world position.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        public UiSpace WorldSpace(UiElement parent) { return new UiSpace(this, Create(UiKind.WorldSpace, parent)); }

        /// <summary>HUD space with its own scale (PlaneScale) in the parent's plane.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        public UiSpace ScaledSpace(UiElement parent) { return new UiSpace(this, Create(UiKind.ScaledSpace, parent)); }

        /// <summary>Line strip through points (SetPoints).</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        public UiShape Line(UiElement parent) { return new UiShape(this, Create(UiKind.Line, parent)); }

        /// <summary>Regular polygon (circle with many sides) filling the element.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        /// <param name="sides">Number of sides (3..256).</param>
        public UiShape Polygon(UiElement parent, int sides = 32) { var s = new UiShape(this, Create(UiKind.Polygon, parent)); s.Sides = sides; return s; }

        /// <summary>Ring (polygon with a hole) filling the element.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        /// <param name="innerRadius">Inner radius as part of the size (0..1).</param>
        public UiShape Ring(UiElement parent, float innerRadius = 0.5f) { var s = new UiShape(this, Create(UiKind.Ring, parent)); s.InnerRadius = innerRadius; return s; }

        /// <summary>Triangle mesh (SetPoints with indices).</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        public UiShape Mesh(UiElement parent) { return new UiShape(this, Create(UiKind.Mesh, parent)); }
    }

    /// <summary>API 2. Multi-line text editor: selection with the mouse, clipboard keys, caret.</summary>
    public class UiTextBox : UiTextElement
    {
        internal UiTextBox(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>The player may edit the text (default true).</summary>
        public bool EnableEditing { get { return GetBool(UiProp.EnableEditing); } set { Set(UiProp.EnableEditing, value); } }

        /// <summary>The player may select text (default true).</summary>
        public bool EnableHighlighting { get { return GetBool(UiProp.EnableHighlighting); } set { Set(UiProp.EnableHighlighting, value); } }

        /// <summary>Caret to the end when it gets the focus.</summary>
        public bool MoveToEndOnFocus { get { return GetBool(UiProp.MoveToEndOnFocus); } set { Set(UiProp.MoveToEndOnFocus, value); } }

        /// <summary>Clear the selection when the focus is lost.</summary>
        public bool ClearSelectionOnBlur { get { return GetBool(UiProp.ClearSelectionOnBlur); } set { Set(UiProp.ClearSelectionOnBlur, value); } }

        /// <summary>True while the player types; set to start or stop typing.</summary>
        public bool Typing { get { return GetBool(UiProp.InputOpen); } set { Set(UiProp.InputOpen, value); } }

        /// <summary>UiText.FilterLine, FilterDigits, FilterNumber or FilterAny (default any).</summary>
        public int CharFilter { set { Set(UiProp.CharFilter, value); } }

        /// <summary>Character that starts a new line (default '\n').</summary>
        public char NewLineChar { get { return (char)GetInt(UiProp.NewLineChar); } set { Set(UiProp.NewLineChar, (int)value); } }

        /// <summary>Wrap the text at the width of the box.</summary>
        public bool Wrap { get { return GetBool(UiProp.Wrap); } set { Set(UiProp.Wrap, value); } }

        /// <summary>Size follows the text.</summary>
        public bool AutoResize { get { return GetBool(UiProp.AutoResize); } set { Set(UiProp.AutoResize, value); } }

        /// <summary>Centre the text vertically.</summary>
        public bool VertCenter { get { return GetBool(UiProp.VertCenter); } set { Set(UiProp.VertCenter, value); } }

        /// <summary>Start of the selection (X line, Y character).</summary>
        public Vector2 SelectionStart { get { return GetVector(UiProp.SelectionStart); } }

        /// <summary>End of the selection (X line, Y character).</summary>
        public Vector2 SelectionEnd { get { return GetVector(UiProp.SelectionEnd); } }

        /// <summary>Caret (X line, Y character).</summary>
        public Vector2 CaretPosition { get { return GetVector(UiProp.CaretPosition); } }

        /// <summary>Selects text.</summary>
        /// <param name="start">Start (X line, Y character).</param>
        /// <param name="end">End (X line, Y character).</param>
        public void Select(Vector2 start, Vector2 end)
        {
            Set(UiProp.SelectionStart, start);
            Set(UiProp.SelectionEnd, end);
        }

        /// <summary>Every change of the text; the handler gets the text.</summary>
        /// <param name="handler">Gets the text; null removes it.</param>
        public void OnTextChanged(Action<string> handler) { On(UiEvent.TextChanged, handler); }
    }

    /// <summary>
    /// API 2. Base of the list controls (drop-down, list box, tree list). Every entry has a key (given back on
    /// selection) and a text with markup.
    /// </summary>
    public class UiListElement : UiTextElement
    {
        internal UiListElement(SyntrixUi ui, int handle) : base(ui, handle) { }

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

        /// <summary>Selected index, -1 if none; set -1 to clear (list box and tree list).</summary>
        public int SelectedIndex { get { return GetInt(UiProp.SelectedIndex); } set { Set(UiProp.SelectedIndex, value); } }

        /// <summary>Key of the selected entry; set to select by key.</summary>
        public string SelectedKey { get { return GetText(UiProp.SelectedKey); } set { Set(UiProp.SelectedKey, value); } }

        /// <summary>Background colour.</summary>
        public Color Color { get { return GetColor(UiProp.Color); } set { Set(UiProp.Color, value); } }

        /// <summary>Colour of the entry under the cursor.</summary>
        public Color HighlightColor { get { return GetColor(UiProp.HighlightColor); } set { Set(UiProp.HighlightColor, value); } }

        /// <summary>Colour of the selection tab.</summary>
        public Color TabColor { get { return GetColor(UiProp.TabColor); } set { Set(UiProp.TabColor, value); } }

        /// <summary>Text colour while focused.</summary>
        public Color FocusTextColor { set { Set(UiProp.FocusTextColor, value); } }

        /// <summary>Height of one entry.</summary>
        public float LineHeight { get { return GetFloat(UiProp.LineHeight); } set { Set(UiProp.LineHeight, value); } }

        /// <summary>Padding of the highlight box.</summary>
        public Vector2 HighlightPadding { get { return GetVector(UiProp.HighlightPadding); } set { Set(UiProp.HighlightPadding, value); } }

        /// <summary>The player selected an entry; the handler gets its key.</summary>
        /// <param name="handler">Gets the key; null removes it.</param>
        public void OnSelected(Action<string> handler) { On(UiEvent.ValueChanged, handler); }
    }

    /// <summary>API 2. Drop-down list.</summary>
    public class UiDropdown : UiListElement
    {
        internal UiDropdown(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Height of the opened list.</summary>
        public float DropdownHeight { get { return GetFloat(UiProp.DropdownHeight); } set { Set(UiProp.DropdownHeight, value); } }

        /// <summary>Smallest number of shown entries.</summary>
        public int MinVisibleCount { get { return GetInt(UiProp.MinVisibleCount); } set { Set(UiProp.MinVisibleCount, value); } }

        /// <summary>Padding of the entries.</summary>
        public Vector2 MemberPadding { get { return GetVector(UiProp.MemberPadding); } set { Set(UiProp.MemberPadding, value); } }

        /// <summary>List opened; set to open or close it.</summary>
        public bool IsOpen { get { return GetBool(UiProp.Open); } set { Set(UiProp.Open, value); } }

        /// <summary>Scroll bar colour.</summary>
        public Color BarColor { set { Set(UiProp.BarColor, value); } }

        /// <summary>Slider colour.</summary>
        public Color SliderColor { set { Set(UiProp.SliderColor, value); } }
    }

    /// <summary>API 2. List box with scrolling.</summary>
    public class UiListBox : UiListElement
    {
        internal UiListBox(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Smallest number of shown entries.</summary>
        public int MinVisibleCount { get { return GetInt(UiProp.MinVisibleCount); } set { Set(UiProp.MinVisibleCount, value); } }

        /// <summary>Padding of the entries.</summary>
        public Vector2 MemberPadding { get { return GetVector(UiProp.MemberPadding); } set { Set(UiProp.MemberPadding, value); } }

        /// <summary>Padding of the entry list.</summary>
        public Vector2 ListPadding { get { return GetVector(UiProp.ListPadding); } set { Set(UiProp.ListPadding, value); } }

        /// <summary>Smallest length of the entry list.</summary>
        public float MinLength { get { return GetFloat(UiProp.MinLength); } set { Set(UiProp.MinLength, value); } }

        /// <summary>Colour while focused.</summary>
        public Color FocusColor { set { Set(UiProp.FocusColor, value); } }

        /// <summary>Scrolling enabled.</summary>
        public bool Scrolling { get { return GetBool(UiProp.Scrolling); } set { Set(UiProp.Scrolling, value); } }

        /// <summary>Smooth scrolling.</summary>
        public bool SmoothScrolling { get { return GetBool(UiProp.SmoothScrolling); } set { Set(UiProp.SmoothScrolling, value); } }

        /// <summary>Scroll bar colour.</summary>
        public Color BarColor { set { Set(UiProp.BarColor, value); } }

        /// <summary>Slider colour.</summary>
        public Color SliderColor { set { Set(UiProp.SliderColor, value); } }
    }

    /// <summary>API 2. Collapsible list with a header (Text).</summary>
    public class UiTreeList : UiListElement
    {
        internal UiTreeList(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>List opened; set to open or close it.</summary>
        public bool IsOpen { get { return GetBool(UiProp.Open); } set { Set(UiProp.Open, value); } }

        /// <summary>Height of the opened list.</summary>
        public float DropdownHeight { get { return GetFloat(UiProp.DropdownHeight); } set { Set(UiProp.DropdownHeight, value); } }

        /// <summary>Indentation of the entries.</summary>
        public float IndentSize { get { return GetFloat(UiProp.IndentSize); } set { Set(UiProp.IndentSize, value); } }

        /// <summary>Height of the header.</summary>
        public float LabelHeight { get { return GetFloat(UiProp.LabelHeight); } set { Set(UiProp.LabelHeight, value); } }

        /// <summary>Header colour.</summary>
        public Color HeaderColor { get { return GetColor(UiProp.HeaderColor); } set { Set(UiProp.HeaderColor, value); } }

        /// <summary>The header was clicked (opened or closed; read IsOpen).</summary>
        /// <param name="a">Handler; null removes it.</param>
        public void OnToggled(Action a) { On(UiEvent.Toggled, a == null ? (Action<string>)null : s => a()); }
    }

    /// <summary>API 2. Collapsible tree node; its children are the entries (labels and other tree nodes).</summary>
    public class UiTreeBox : UiTextElement
    {
        internal UiTreeBox(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>List opened; set to open or close it.</summary>
        public bool IsOpen { get { return GetBool(UiProp.Open); } set { Set(UiProp.Open, value); } }

        /// <summary>Number of entries.</summary>
        public int ItemCount { get { return GetInt(UiProp.ItemCount); } }

        /// <summary>Selected index, -1 if none; set -1 to clear.</summary>
        public int SelectedIndex { get { return GetInt(UiProp.SelectedIndex); } set { Set(UiProp.SelectedIndex, value); } }

        /// <summary>Height of the opened list.</summary>
        public float DropdownHeight { get { return GetFloat(UiProp.DropdownHeight); } set { Set(UiProp.DropdownHeight, value); } }

        /// <summary>Indentation of the entries.</summary>
        public float IndentSize { get { return GetFloat(UiProp.IndentSize); } set { Set(UiProp.IndentSize, value); } }

        /// <summary>Height of the header.</summary>
        public float LabelHeight { get { return GetFloat(UiProp.LabelHeight); } set { Set(UiProp.LabelHeight, value); } }

        /// <summary>Header colour.</summary>
        public Color HeaderColor { get { return GetColor(UiProp.HeaderColor); } set { Set(UiProp.HeaderColor, value); } }

        /// <summary>Colour of the entry under the cursor.</summary>
        public Color HighlightColor { get { return GetColor(UiProp.HighlightColor); } set { Set(UiProp.HighlightColor, value); } }

        /// <summary>Colour of the selection tab.</summary>
        public Color TabColor { get { return GetColor(UiProp.TabColor); } set { Set(UiProp.TabColor, value); } }

        /// <summary>The player selected an entry; the handler gets the handle of the child element.</summary>
        /// <param name="handler">Gets the child handle; null removes it.</param>
        public void OnSelected(Action<int> handler)
        {
            On(UiEvent.ValueChanged, handler == null ? (Action<string>)null : s =>
            {
                int h;
                handler(int.TryParse(s, out h) ? h : 0);
            });
        }

        /// <summary>The header was clicked (opened or closed; read IsOpen).</summary>
        /// <param name="a">Handler; null removes it.</param>
        public void OnToggled(Action a) { On(UiEvent.Toggled, a == null ? (Action<string>)null : s => a()); }
    }

    /// <summary>API 2. Colour picker (RGB or HSV sliders) with a name (Text).</summary>
    public class UiColorPicker : UiTextElement
    {
        internal UiColorPicker(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>The colour.</summary>
        public Color Value { get { return GetColor(UiProp.ValueColor); } set { Set(UiProp.ValueColor, value); } }

        /// <summary>HSV picker: hue (0..360), saturation and value (0..100).</summary>
        public Vector3D Hsv { get { return GetVector3(UiProp.ColorHsv); } set { Set(UiProp.ColorHsv, value); } }

        /// <summary>The player changed the colour.</summary>
        /// <param name="handler">Gets the colour; null removes it.</param>
        public void OnChanged(Action<Color> handler) { On(UiEvent.ValueChanged, handler == null ? (Action<string>)null : s => handler(SyntrixUi.ParseColor(s))); }
    }

    /// <summary>API 2. Two buttons "on" and "off".</summary>
    public class UiOnOffButton : UiElement
    {
        internal UiOnOffButton(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>On state.</summary>
        public bool Value { get { return GetBool(UiProp.Checked); } set { Set(UiProp.Checked, value); } }

        /// <summary>Text of the on side (markup).</summary>
        public string OnText { get { return GetText(UiProp.OnText); } set { Set(UiProp.OnText, value); } }

        /// <summary>Text of the off side (markup).</summary>
        public string OffText { get { return GetText(UiProp.OffText); } set { Set(UiProp.OffText, value); } }

        /// <summary>Background colour.</summary>
        public Color Color { get { return GetColor(UiProp.Color); } set { Set(UiProp.Color, value); } }

        /// <summary>Colour under the cursor.</summary>
        public Color HighlightColor { get { return GetColor(UiProp.HighlightColor); } set { Set(UiProp.HighlightColor, value); } }

        /// <summary>Border colour.</summary>
        public Color BorderColor { get { return GetColor(UiProp.BorderColor); } set { Set(UiProp.BorderColor, value); } }

        /// <summary>Colour while focused.</summary>
        public Color FocusColor { set { Set(UiProp.FocusColor, value); } }

        /// <summary>Colour of the selected side.</summary>
        public Color SelectionColor { get { return GetColor(UiProp.SelectionColor); } set { Set(UiProp.SelectionColor, value); } }

        /// <summary>Colour of the other side.</summary>
        public Color UnselectedColor { get { return GetColor(UiProp.UnselectedColor); } set { Set(UiProp.UnselectedColor, value); } }

        /// <summary>Space between the buttons.</summary>
        public float ButtonSpacing { get { return GetFloat(UiProp.ButtonSpacing); } set { Set(UiProp.ButtonSpacing, value); } }

        /// <summary>Padding around the buttons.</summary>
        public Vector2 BackgroundPadding { get { return GetVector(UiProp.BackgroundPadding); } set { Set(UiProp.BackgroundPadding, value); } }

        /// <summary>Use the focus colours while focused.</summary>
        public bool UseFocusFormatting { get { return GetBool(UiProp.UseFocusFormatting); } set { Set(UiProp.UseFocusFormatting, value); } }

        /// <summary>The player switched.</summary>
        /// <param name="handler">Gets the new state; null removes it.</param>
        public void OnChanged(Action<bool> handler) { On(UiEvent.ValueChanged, handler == null ? (Action<string>)null : s => handler(s == "1")); }
    }

    /// <summary>API 2. Check box with a name (Text).</summary>
    public class UiNamedCheckBox : UiTextElement
    {
        internal UiNamedCheckBox(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Checked state.</summary>
        public bool Checked { get { return GetBool(UiProp.Checked); } set { Set(UiProp.Checked, value); } }

        /// <summary>Size follows the text.</summary>
        public bool AutoResize { get { return GetBool(UiProp.AutoResize); } set { Set(UiProp.AutoResize, value); } }

        /// <summary>Padding around the name.</summary>
        public Vector2 TextPadding { get { return GetVector(UiProp.TextPadding); } set { Set(UiProp.TextPadding, value); } }

        /// <summary>The player changed the state.</summary>
        /// <param name="handler">Gets the new state; null removes it.</param>
        public void OnChanged(Action<bool> handler) { On(UiEvent.ValueChanged, handler == null ? (Action<string>)null : s => handler(s == "1")); }
    }

    /// <summary>API 2. On/off buttons with a name (Text).</summary>
    public class UiNamedOnOffButton : UiTextElement
    {
        internal UiNamedOnOffButton(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>On state.</summary>
        public bool Value { get { return GetBool(UiProp.Checked); } set { Set(UiProp.Checked, value); } }

        /// <summary>Text of the on side (markup).</summary>
        public string OnText { get { return GetText(UiProp.OnText); } set { Set(UiProp.OnText, value); } }

        /// <summary>Text of the off side (markup).</summary>
        public string OffText { get { return GetText(UiProp.OffText); } set { Set(UiProp.OffText, value); } }

        /// <summary>Border colour.</summary>
        public Color BorderColor { get { return GetColor(UiProp.BorderColor); } set { Set(UiProp.BorderColor, value); } }

        /// <summary>Space between the buttons.</summary>
        public float ButtonSpacing { get { return GetFloat(UiProp.ButtonSpacing); } set { Set(UiProp.ButtonSpacing, value); } }

        /// <summary>Padding of the buttons.</summary>
        public Vector2 ButtonPadding { get { return GetVector(UiProp.ButtonPadding); } set { Set(UiProp.ButtonPadding, value); } }

        /// <summary>The player switched.</summary>
        /// <param name="handler">Gets the new state; null removes it.</param>
        public void OnChanged(Action<bool> handler) { On(UiEvent.ValueChanged, handler == null ? (Action<string>)null : s => handler(s == "1")); }
    }

    /// <summary>API 2. Slider with a name (Text) and a value text.</summary>
    public class UiNamedSlider : UiTextElement
    {
        internal UiNamedSlider(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Smallest value.</summary>
        public float Min { get { return GetFloat(UiProp.Min); } set { Set(UiProp.Min, value); } }

        /// <summary>Largest value.</summary>
        public float Max { get { return GetFloat(UiProp.Max); } set { Set(UiProp.Max, value); } }

        /// <summary>Current value.</summary>
        public float Value { get { return GetFloat(UiProp.Value); } set { Set(UiProp.Value, value); } }

        /// <summary>Value as 0..1.</summary>
        public float Percent { get { return GetFloat(UiProp.Percent); } set { Set(UiProp.Percent, value); } }

        /// <summary>Text shown right of the name, e.g. the formatted value (markup).</summary>
        public string ValueText { get { return GetText(UiProp.ValueText); } set { Set(UiProp.ValueText, value); } }

        /// <summary>Background colour.</summary>
        public Color Color { set { Set(UiProp.Color, value); } }

        /// <summary>Bar colour.</summary>
        public Color BarColor { set { Set(UiProp.BarColor, value); } }

        /// <summary>Slider colour.</summary>
        public Color SliderColor { set { Set(UiProp.SliderColor, value); } }

        /// <summary>The player moved the slider.</summary>
        /// <param name="handler">Gets the new value; null removes it.</param>
        public void OnChanged(Action<float> handler) { On(UiEvent.ValueChanged, handler == null ? (Action<string>)null : s => handler(Value)); }
    }

    /// <summary>API 2. Scroll bar.</summary>
    public class UiScrollBar : UiElement
    {
        internal UiScrollBar(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Smallest value.</summary>
        public float Min { get { return GetFloat(UiProp.Min); } set { Set(UiProp.Min, value); } }

        /// <summary>Largest value.</summary>
        public float Max { get { return GetFloat(UiProp.Max); } set { Set(UiProp.Max, value); } }

        /// <summary>Current value.</summary>
        public float Value { get { return GetFloat(UiProp.Value); } set { Set(UiProp.Value, value); } }

        /// <summary>Value as 0..1.</summary>
        public float Percent { get { return GetFloat(UiProp.Percent); } set { Set(UiProp.Percent, value); } }

        /// <summary>Visible part as 0..1 (length of the slider).</summary>
        public float VisiblePercent { get { return GetFloat(UiProp.VisiblePercent); } set { Set(UiProp.VisiblePercent, value); } }

        /// <summary>Vertical bar.</summary>
        public bool Vertical { get { return GetBool(UiProp.Vertical); } set { Set(UiProp.Vertical, value); } }

        /// <summary>Bar colour.</summary>
        public Color BarColor { set { Set(UiProp.BarColor, value); } }

        /// <summary>Slider colour.</summary>
        public Color SliderColor { set { Set(UiProp.SliderColor, value); } }

        /// <summary>The player scrolled.</summary>
        /// <param name="handler">Gets the new value; null removes it.</param>
        public void OnChanged(Action<float> handler) { On(UiEvent.ValueChanged, handler == null ? (Action<string>)null : s => handler(Value)); }
    }

    /// <summary>API 2. Bare slider bar, e.g. a progress bar (InputEnabled = false) or an own slider.</summary>
    public class UiSliderBar : UiElement
    {
        internal UiSliderBar(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Smallest value.</summary>
        public float Min { get { return GetFloat(UiProp.Min); } set { Set(UiProp.Min, value); } }

        /// <summary>Largest value.</summary>
        public float Max { get { return GetFloat(UiProp.Max); } set { Set(UiProp.Max, value); } }

        /// <summary>Current value.</summary>
        public float Value { get { return GetFloat(UiProp.Value); } set { Set(UiProp.Value, value); } }

        /// <summary>Value as 0..1.</summary>
        public float Percent { get { return GetFloat(UiProp.Percent); } set { Set(UiProp.Percent, value); } }

        /// <summary>Vertical bar.</summary>
        public bool Vertical { get { return GetBool(UiProp.Vertical); } set { Set(UiProp.Vertical, value); } }

        /// <summary>Reverse direction.</summary>
        public bool Reverse { get { return GetBool(UiProp.Reverse); } set { Set(UiProp.Reverse, value); } }

        /// <summary>Show the slider.</summary>
        public bool SliderVisible { get { return GetBool(UiProp.SliderVisible); } set { Set(UiProp.SliderVisible, value); } }

        /// <summary>Size of the bar.</summary>
        public Vector2 BarSize { get { return GetVector(UiProp.BarSize); } set { Set(UiProp.BarSize, value); } }

        /// <summary>Size of the slider.</summary>
        public Vector2 SliderSize { get { return GetVector(UiProp.SliderSize); } set { Set(UiProp.SliderSize, value); } }

        /// <summary>Highlight under the cursor.</summary>
        public bool HighlightEnabled { get { return GetBool(UiProp.HighlightEnabled); } set { Set(UiProp.HighlightEnabled, value); } }

        /// <summary>Bar colour.</summary>
        public Color BarColor { set { Set(UiProp.BarColor, value); } }

        /// <summary>Bar colour under the cursor.</summary>
        public Color BarHighlight { set { Set(UiProp.BarHighlight, value); } }

        /// <summary>Slider colour.</summary>
        public Color SliderColor { set { Set(UiProp.SliderColor, value); } }

        /// <summary>Slider colour under the cursor.</summary>
        public Color SliderHighlight { set { Set(UiProp.SliderHighlight, value); } }

        /// <summary>The player moved the slider.</summary>
        /// <param name="handler">Gets the new value; null removes it.</param>
        public void OnChanged(Action<float> handler) { On(UiEvent.ValueChanged, handler == null ? (Action<string>)null : s => handler(Value)); }
    }

    /// <summary>API 2. Clickable image with highlight colour (all UiBox properties apply).</summary>
    public class UiIconButton : UiBox
    {
        internal UiIconButton(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Tint under the cursor.</summary>
        public Color HighlightColor { get { return GetColor(UiProp.HighlightColor); } set { Set(UiProp.HighlightColor, value); } }

        /// <summary>Highlight under the cursor.</summary>
        public bool HighlightEnabled { get { return GetBool(UiProp.HighlightEnabled); } set { Set(UiProp.HighlightEnabled, value); } }
    }

    /// <summary>API 2. Clickable text without background.</summary>
    public class UiLabelButton : UiTextElement
    {
        internal UiLabelButton(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Size follows the text.</summary>
        public bool AutoResize { get { return GetBool(UiProp.AutoResize); } set { Set(UiProp.AutoResize, value); } }

        /// <summary>Centre the text vertically.</summary>
        public bool VertCenter { get { return GetBool(UiProp.VertCenter); } set { Set(UiProp.VertCenter, value); } }
    }

    /// <summary>API 2. Text button in the Rich HUD terminal style (all UiButton properties apply).</summary>
    public class UiBorderedButton : UiButton
    {
        internal UiBorderedButton(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Colour while focused.</summary>
        public Color FocusColor { set { Set(UiProp.FocusColor, value); } }

        /// <summary>Text colour while focused.</summary>
        public Color FocusTextColor { set { Set(UiProp.FocusTextColor, value); } }

        /// <summary>Use the focus colours while focused.</summary>
        public bool UseFocusFormatting { get { return GetBool(UiProp.UseFocusFormatting); } set { Set(UiProp.UseFocusFormatting, value); } }
    }

    /// <summary>API 2. Label box with a left text (Text) and a right text.</summary>
    public class UiDoubleLabelBox : UiTextElement
    {
        internal UiDoubleLabelBox(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Right text (markup).</summary>
        public string RightText { get { return GetText(UiProp.RightText); } set { Set(UiProp.RightText, value); } }

        /// <summary>Background colour.</summary>
        public Color Color { get { return GetColor(UiProp.Color); } set { Set(UiProp.Color, value); } }

        /// <summary>Padding around the texts.</summary>
        public Vector2 TextPadding { get { return GetVector(UiProp.TextPadding); } set { Set(UiProp.TextPadding, value); } }

        /// <summary>Size follows the texts.</summary>
        public bool AutoResize { get { return GetBool(UiProp.AutoResize); } set { Set(UiProp.AutoResize, value); } }
    }

    /// <summary>
    /// API 2. Radial (pie) selection; its children are the slots. It raises no events: read HighlightIndex
    /// (e.g. in a bind handler) and set SelectedIndex.
    /// </summary>
    public class UiRadialSelection : UiElement
    {
        internal UiRadialSelection(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Number of slots.</summary>
        public int MaxEntryCount { get { return GetInt(UiProp.MaxEntryCount); } set { Set(UiProp.MaxEntryCount, value); } }

        /// <summary>Number of children.</summary>
        public int ItemCount { get { return GetInt(UiProp.ItemCount); } }

        /// <summary>Number of shown children.</summary>
        public int EnabledCount { get { return GetInt(UiProp.EnabledCount); } }

        /// <summary>Selected index, -1 if none; set -1 to clear.</summary>
        public int SelectedIndex { get { return GetInt(UiProp.SelectedIndex); } set { Set(UiProp.SelectedIndex, value); } }

        /// <summary>Highlighted index (under the cursor or gesture), -1 if none; set -1 to clear.</summary>
        public int HighlightIndex { get { return GetInt(UiProp.HighlightIndex); } set { Set(UiProp.HighlightIndex, value); } }

        /// <summary>Highlight by mouse gesture instead of the cursor position.</summary>
        public bool UseGestureInput { get { return GetBool(UiProp.UseGestureInput); } set { Set(UiProp.UseGestureInput, value); } }

        /// <summary>Cursor sensitivity of the gesture input.</summary>
        public float CursorSensitivity { get { return GetFloat(UiProp.CursorSensitivity); } set { Set(UiProp.CursorSensitivity, value); } }

        /// <summary>Inner radius of the ring.</summary>
        public float InnerRadius { get { return GetFloat(UiProp.InnerRadius); } set { Set(UiProp.InnerRadius, value); } }

        /// <summary>Background colour.</summary>
        public Color Color { get { return GetColor(UiProp.Color); } set { Set(UiProp.Color, value); } }

        /// <summary>Colour of the highlighted slot.</summary>
        public Color HighlightColor { get { return GetColor(UiProp.HighlightColor); } set { Set(UiProp.HighlightColor, value); } }

        /// <summary>Colour of the selected slot.</summary>
        public Color SelectionColor { get { return GetColor(UiProp.SelectionColor); } set { Set(UiProp.SelectionColor, value); } }
    }

    /// <summary>
    /// API 2. HUD space: its children are drawn in another plane (in front of the camera, at a world matrix or
    /// with an own scale). Children use the units of that plane.
    /// </summary>
    public class UiSpace : UiElement
    {
        internal UiSpace(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>World space: plane to world matrix (the plane faces along Backward). Fine every frame.</summary>
        /// <param name="matrix">World matrix.</param>
        public void SetMatrix(MatrixD matrix) { Ui.SetMatrix(Handle, matrix); }

        /// <summary>Cam and scaled space: scale of the plane.</summary>
        public float PlaneScale { get { return GetFloat(UiProp.PlaneScale); } set { Set(UiProp.PlaneScale, value); } }

        /// <summary>Cam space: rotation angle in radians around RotationAxis.</summary>
        public float RotationAngle { get { return GetFloat(UiProp.RotationAngle); } set { Set(UiProp.RotationAngle, value); } }

        /// <summary>Cam space: rotation axis.</summary>
        public Vector3D RotationAxis { get { return GetVector3(UiProp.RotationAxis); } set { Set(UiProp.RotationAxis, value); } }

        /// <summary>Cam space: offset from the camera.</summary>
        public Vector3D TransformOffset { get { return GetVector3(UiProp.TransformOffset); } set { Set(UiProp.TransformOffset, value); } }

        /// <summary>Cam space: in screen space instead of world space.</summary>
        public bool IsScreenSpace { get { return GetBool(UiProp.IsScreenSpace); } set { Set(UiProp.IsScreenSpace, value); } }

        /// <summary>Cam space: scale with the screen resolution.</summary>
        public bool UseResScaling { get { return GetBool(UiProp.UseResScaling); } set { Set(UiProp.UseResScaling, value); } }

        /// <summary>The plane is in front of the camera.</summary>
        public bool IsInFront { get { return GetBool(UiProp.IsInFront); } }

        /// <summary>The plane faces the camera.</summary>
        public bool IsFacingCamera { get { return GetBool(UiProp.IsFacingCamera); } }

        /// <summary>Cursor position in the plane (Z = depth).</summary>
        public Vector3D CursorInPlane { get { return GetVector3(UiProp.SpaceCursor); } }
    }

    /// <summary>API 2. Custom drawing: line strip, regular polygon, ring or triangle mesh.</summary>
    public class UiShape : UiElement
    {
        internal UiShape(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Colour; transparent hides the shape.</summary>
        public Color Color { get { return GetColor(UiProp.Color); } set { Set(UiProp.Color, value); } }

        /// <summary>Texture (transparent material subtype); null = plain colour.</summary>
        public string Texture { get { return GetText(UiProp.Texture); } set { Set(UiProp.Texture, value); } }

        /// <summary>Line: thickness.</summary>
        public float Thickness { get { return GetFloat(UiProp.BorderThickness); } set { Set(UiProp.BorderThickness, value); } }

        /// <summary>Line: connect the last point with the first.</summary>
        public bool Closed { get { return GetBool(UiProp.ClosedLine); } set { Set(UiProp.ClosedLine, value); } }

        /// <summary>Polygon and ring: number of sides (3..256).</summary>
        public int Sides { get { return GetInt(UiProp.Sides); } set { Set(UiProp.Sides, value); } }

        /// <summary>Ring: inner radius as part of the size (0..1).</summary>
        public float InnerRadius { get { return GetFloat(UiProp.InnerRadius); } set { Set(UiProp.InnerRadius, value); } }

        /// <summary>Line and mesh: points relative to the centre of the element; mesh: three indices per triangle.</summary>
        /// <param name="points">Points; null clears.</param>
        /// <param name="indices">Mesh triangle indices; ignored for lines.</param>
        public void SetPoints(Vector2[] points, int[] indices = null) { Ui.SetPoints(Handle, points, indices); }
    }
}
