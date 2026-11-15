// Syntrix UI dock file (API version 2, Syntrix 1.1.0). Optional dock files for the UI service: copy
// SyntrixUi.cs, SyntrixUi.Controls.cs and SyntrixUi.Terminal.cs next to SyntrixApi.cs only if your mod shows
// windows, HUD elements or Rich HUD terminal pages. Do not edit them.
// Needs Rich HUD Master (Workshop 1965654081) in the world, which is a dependency of Syntrix.
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
using VRage.Utils;
using VRageMath;

namespace Syntrix.Api
{
    /// <summary>Element kinds (API 2). Values are fixed.</summary>
    public static class UiKind
    {
        /// <summary>Invisible element without content: anchor, spacer or group.</summary>
        public const int Empty = 1;
        /// <summary>Coloured or textured rectangle (TexturedBox).</summary>
        public const int Box = 2;
        /// <summary>Frame line around the element's area (BorderBox).</summary>
        public const int Border = 3;
        /// <summary>Text with markup and clickable links.</summary>
        public const int Label = 4;
        /// <summary>Text on a coloured background (LabelBox).</summary>
        public const int LabelBox = 5;
        /// <summary>Background, optional border and optional mouse input with highlight colour.</summary>
        public const int Panel = 6;
        /// <summary>Text button with highlight (LabelBoxButton) and optional border.</summary>
        public const int Button = 7;
        /// <summary>One-line text entry.</summary>
        public const int TextField = 8;
        /// <summary>Vertical scrolling list; children become its entries.</summary>
        public const int ScrollBox = 9;
        /// <summary>Horizontal scrolling list.</summary>
        public const int ScrollBoxHorizontal = 10;
        /// <summary>Vertical stack; children become its members.</summary>
        public const int Chain = 11;
        /// <summary>Horizontal stack.</summary>
        public const int ChainHorizontal = 12;
        /// <summary>Invisible mouse input area.</summary>
        public const int ClickArea = 13;
        /// <summary>Window with header and body, draggable and resizable.</summary>
        public const int Window = 14;
        /// <summary>Check box with a value.</summary>
        public const int CheckBox = 15;
        /// <summary>Slider with minimum, maximum and value.</summary>
        public const int Slider = 16;
        /// <summary>Multi-line text editor with caret, selection and clipboard (Rich HUD TextBox).</summary>
        public const int TextBox = 18;
        /// <summary>Drop-down list; entries through the Ui.Item endpoints.</summary>
        public const int Dropdown = 19;
        /// <summary>List box with selection; entries through the Ui.Item endpoints.</summary>
        public const int ListBox = 20;
        /// <summary>Collapsible list with a header; entries through the Ui.Item endpoints.</summary>
        public const int TreeList = 21;
        /// <summary>Collapsible tree node; label children (also other tree nodes) become its entries.</summary>
        public const int TreeBox = 22;
        /// <summary>Colour picker with red, green and blue sliders.</summary>
        public const int ColorPickerRgb = 23;
        /// <summary>Colour picker with hue, saturation and value sliders.</summary>
        public const int ColorPickerHsv = 24;
        /// <summary>Pair of on and off buttons.</summary>
        public const int OnOffButton = 25;
        /// <summary>Check box with a name.</summary>
        public const int NamedCheckBox = 26;
        /// <summary>On/off buttons with a name.</summary>
        public const int NamedOnOffButton = 27;
        /// <summary>Slider with a name and a value text.</summary>
        public const int NamedSlider = 28;
        /// <summary>Scroll bar (value with visible part).</summary>
        public const int ScrollBar = 29;
        /// <summary>Bare slider bar with free bar and slider sizes.</summary>
        public const int SliderBar = 30;
        /// <summary>Textured button (icon button).</summary>
        public const int IconButton = 31;
        /// <summary>Clickable text without background.</summary>
        public const int LabelButton = 32;
        /// <summary>Text button with border and focus colours.</summary>
        public const int BorderedButton = 33;
        /// <summary>Label box with a left and a right text.</summary>
        public const int DoubleLabelBox = 34;
        /// <summary>Radial selection wheel; children become its entries.</summary>
        public const int RadialSelection = 35;
        /// <summary>HUD space in front of the camera: offset, rotation and scale (Rich HUD CamSpaceNode).</summary>
        public const int CamSpace = 36;
        /// <summary>HUD space at a world matrix set by the mod (Ui.SetMatrix): billboards and panels in the world.</summary>
        public const int WorldSpace = 37;
        /// <summary>HUD space scaled relative to its parent (Rich HUD ScaledSpaceNode).</summary>
        public const int ScaledSpace = 38;
        /// <summary>Line strip through points (Ui.SetPoints) with thickness, colour and texture.</summary>
        public const int Line = 39;
        /// <summary>Regular polygon filling the element (circle with many sides).</summary>
        public const int Polygon = 40;
        /// <summary>Ring: polygon with a hole.</summary>
        public const int Ring = 41;
        /// <summary>Triangle mesh from points and indices (Ui.SetPoints).</summary>
        public const int Mesh = 42;
        /// <summary>Terminal: group of pages in the mod's entry of the Rich HUD terminal.</summary>
        public const int TerminalCategory = 60;
        /// <summary>Terminal: page with control categories.</summary>
        public const int ControlPage = 61;
        /// <summary>Terminal: page with header, sub-header and long text.</summary>
        public const int TextPage = 62;
        /// <summary>Terminal: category (header and sub-header) on a control page.</summary>
        public const int ControlCategory = 64;
        /// <summary>Terminal: tile of up to three controls in a category.</summary>
        public const int ControlTile = 65;
        /// <summary>Terminal control: button.</summary>
        public const int TerminalButton = 66;
        /// <summary>Terminal control: check box.</summary>
        public const int TerminalCheckBox = 67;
        /// <summary>Terminal control: colour picker.</summary>
        public const int TerminalColorPicker = 68;
        /// <summary>Terminal control: drag box that places a point on the screen.</summary>
        public const int TerminalDragBox = 69;
        /// <summary>Terminal control: drop-down list (entries through Ui.Item).</summary>
        public const int TerminalDropdown = 70;
        /// <summary>Terminal control: text line.</summary>
        public const int TerminalLabel = 71;
        /// <summary>Terminal control: list (entries through Ui.Item).</summary>
        public const int TerminalList = 72;
        /// <summary>Terminal control: on/off buttons.</summary>
        public const int TerminalOnOffButton = 73;
        /// <summary>Terminal control: slider with value text.</summary>
        public const int TerminalSlider = 74;
        /// <summary>Terminal control: text field.</summary>
        public const int TerminalTextField = 75;
    }

    /// <summary>
    /// Element properties (API 2), by value type: 1-19 and 200-219 Vector2, 20-39 and 220-239 float, 40-59 and
    /// 240-259 bool, 60-79 and 260-279 int, 80-99 and 280-299 Color, 100-119 and 300-319 string, 320-339 Vector3D.
    /// Values are fixed. A property an element does not have is ignored and reads as the default.
    /// </summary>
    public static class UiProp
    {
        // Vector2
        /// <summary>Offset from the aligned position.</summary>
        public const int Offset = 1;
        /// <summary>Size of the element.</summary>
        public const int Size = 2;
        /// <summary>Inner padding.</summary>
        public const int Padding = 3;
        /// <summary>Window: smallest size when resized by the player.</summary>
        public const int MinSize = 4;
        /// <summary>Padding around the text.</summary>
        public const int TextPadding = 5;
        /// <summary>Full pixel size of the texture (needed for regions).</summary>
        public const int TextureSize = 6;
        /// <summary>Top left pixel of the used region of the texture.</summary>
        public const int TextureOffset = 7;
        /// <summary>Pixel size of the used region; zero = whole texture.</summary>
        public const int TextureRegion = 8;
        /// <summary>Read only: centre of the element on the screen.</summary>
        public const int Position = 9;
        /// <summary>Read only: size of the element's text.</summary>
        public const int TextSize = 10;
        /// <summary>Chain and scroll box: largest member size.</summary>
        public const int MemberMaxSize = 11;
        /// <summary>Chain and scroll box: smallest member size.</summary>
        public const int MemberMinSize = 12;

        // float
        /// <summary>Width of the element.</summary>
        public const int Width = 20;
        /// <summary>Height of the element.</summary>
        public const int Height = 21;
        /// <summary>Chain and scroll box: space between members.</summary>
        public const int Spacing = 22;
        /// <summary>Thickness of the border or line.</summary>
        public const int BorderThickness = 23;
        /// <summary>Base font size of the text (1 = normal).</summary>
        public const int FontSize = 24;
        /// <summary>Width at which wrapped text breaks.</summary>
        public const int WrapWidth = 25;
        /// <summary>Label: wrap at the parent's width minus this margin; negative = off.</summary>
        public const int WrapToParent = 26;
        /// <summary>Label: upper limit for WrapToParent; zero = none.</summary>
        public const int WrapMax = 27;
        /// <summary>Slider value.</summary>
        public const int Value = 28;
        /// <summary>Slider minimum.</summary>
        public const int Min = 29;
        /// <summary>Slider maximum.</summary>
        public const int Max = 30;
        /// <summary>Window: height of the header.</summary>
        public const int HeaderHeight = 31;

        // bool
        /// <summary>Shown; in a scroll box hidden entries leave the list.</summary>
        public const int Visible = 40;
        /// <summary>Size follows the text.</summary>
        public const int AutoResize = 41;
        /// <summary>Wrap the text.</summary>
        public const int Wrap = 42;
        /// <summary>Centre the text vertically.</summary>
        public const int VertCenter = 43;
        /// <summary>Window: the player may move it.</summary>
        public const int Draggable = 44;
        /// <summary>Window: the player may resize it.</summary>
        public const int Resizable = 45;
        /// <summary>Window: hide the game HUD while open.</summary>
        public const int HideHud = 46;
        /// <summary>Window: keep the view still while open (mouse look, also in seats).</summary>
        public const int LockView = 47;
        /// <summary>Window: Escape closes it while focused.</summary>
        public const int CloseOnEscape = 48;
        /// <summary>Scroll box: smooth scrolling.</summary>
        public const int SmoothScrolling = 49;
        /// <summary>Scroll box: scrolling enabled.</summary>
        public const int Scrolling = 50;
        /// <summary>Highlight under the cursor.</summary>
        public const int HighlightEnabled = 51;
        /// <summary>Check box and on/off value.</summary>
        public const int Checked = 52;
        /// <summary>Read only.</summary>
        public const int MousedOver = 53;
        /// <summary>Text field typing; set true to start typing, false to stop.</summary>
        public const int InputOpen = 54;
        /// <summary>The element takes mouse input.</summary>
        public const int InputEnabled = 55;
        /// <summary>Clip children to this element's area.</summary>
        public const int Masking = 56;
        /// <summary>Read only: window has the focus.</summary>
        public const int Focused = 57;
        /// <summary>Window: show the cursor and keep mouse and camera from the game while open (default true).</summary>
        public const int Modal = 58;

        // int
        /// <summary>Alignment to the parent (UiAlign flags).</summary>
        public const int ParentAlign = 60;
        /// <summary>Size taken from the parent (UiDim).</summary>
        public const int DimAlign = 61;
        /// <summary>Text alignment (left, centre, right).</summary>
        public const int TextAlign = 62;
        /// <summary>Font style flags (bold, italic, underline).</summary>
        public const int FontStyle = 63;
        /// <summary>Chain and scroll box sizing (UiSizing flags).</summary>
        public const int Sizing = 64;
        /// <summary>Scroll box: first shown entry.</summary>
        public const int Start = 65;
        /// <summary>Drawing order offset among siblings.</summary>
        public const int ZOffset = 66;
        /// <summary>Text field: longest text; 0 = no limit.</summary>
        public const int MaxLength = 67;
        /// <summary>Text field and text box: allowed characters (line, digits, number, any).</summary>
        public const int CharFilter = 68;
        /// <summary>Read only: number of children.</summary>
        public const int Count = 69;
        /// <summary>Texture alignment (stretch, fit vertical, fit horizontal, fit auto).</summary>
        public const int TextureAlign = 70;
        /// <summary>Read only: last visible entry of a scroll box.</summary>
        public const int End = 71;
        /// <summary>Read only: number of visible entries of a scroll box.</summary>
        public const int EnabledCount = 72;

        // Color
        /// <summary>Main colour (background or tint).</summary>
        public const int Color = 80;
        /// <summary>Colour under the cursor.</summary>
        public const int HighlightColor = 81;
        /// <summary>Border colour.</summary>
        public const int BorderColor = 82;
        /// <summary>Base text colour.</summary>
        public const int TextColor = 83;
        /// <summary>Label: colour of links.</summary>
        public const int LinkColor = 84;
        /// <summary>Colour while focused.</summary>
        public const int FocusColor = 85;
        /// <summary>Scroll or slider bar colour.</summary>
        public const int BarColor = 86;
        /// <summary>Bar colour under the cursor.</summary>
        public const int BarHighlight = 87;
        /// <summary>Slider colour.</summary>
        public const int SliderColor = 88;
        /// <summary>Slider colour under the cursor.</summary>
        public const int SliderHighlight = 89;
        /// <summary>Scroll box: divider colour.</summary>
        public const int DividerColor = 90;
        /// <summary>Window and tree: header colour.</summary>
        public const int HeaderColor = 91;
        /// <summary>Window: body colour.</summary>
        public const int BodyColor = 92;
        /// <summary>Check box: tick colour.</summary>
        public const int TickColor = 93;

        // string
        /// <summary>Text with markup (see UiMarkup).</summary>
        public const int Text = 100;
        /// <summary>Text without markup.</summary>
        public const int PlainText = 101;
        /// <summary>Rich HUD font name.</summary>
        public const int Font = 102;
        /// <summary>Transparent material subtype used as texture.</summary>
        public const int Texture = 103;
        /// <summary>Text field: shown while empty and not typing (markup).</summary>
        public const int Placeholder = 104;
        /// <summary>Tooltip text (markup).</summary>
        public const int Tooltip = 105;
        /// <summary>Window: header text (markup).</summary>
        public const int Title = 106;

        // ---------------------------------------------------------------- second ranges

        // Vector2
        /// <summary>Text box: start of the selection (X line, Y character); set SelectionEnd afterwards to apply.</summary>
        public const int SelectionStart = 200;
        /// <summary>Text box: end of the selection (X line, Y character); setting it selects from SelectionStart.</summary>
        public const int SelectionEnd = 201;
        /// <summary>Read only. Text box: caret (X line, Y character).</summary>
        public const int CaretPosition = 202;
        /// <summary>Lists: padding of the highlight box.</summary>
        public const int HighlightPadding = 203;
        /// <summary>Drop-down and lists: padding of the entries.</summary>
        public const int MemberPadding = 204;
        /// <summary>Lists: padding of the entry list.</summary>
        public const int ListPadding = 205;
        /// <summary>On/off button: padding around the two buttons.</summary>
        public const int BackgroundPadding = 206;
        /// <summary>Slider bar: size of the bar.</summary>
        public const int BarSize = 207;
        /// <summary>Slider bar: size of the slider.</summary>
        public const int SliderSize = 208;
        /// <summary>Terminal drag box: value (screen position).</summary>
        public const int ValueVector = 209;
        /// <summary>Terminal drag box: size of the dragged box.</summary>
        public const int BoxSize = 210;
        /// <summary>Named on/off button: padding of the buttons.</summary>
        public const int ButtonPadding = 211;

        // float
        /// <summary>Lists: height of one entry.</summary>
        public const int LineHeight = 220;
        /// <summary>Drop-down and tree: height of the opened list.</summary>
        public const int DropdownHeight = 221;
        /// <summary>Tree: indentation of the entries.</summary>
        public const int IndentSize = 222;
        /// <summary>Tree: height of the header.</summary>
        public const int LabelHeight = 223;
        /// <summary>On/off button: space between the buttons.</summary>
        public const int ButtonSpacing = 224;
        /// <summary>Sliders and scroll bars: value as 0..1.</summary>
        public const int Percent = 225;
        /// <summary>Scroll bar: visible part as 0..1 (slider length).</summary>
        public const int VisiblePercent = 226;
        /// <summary>Radial selection: cursor sensitivity of the gesture input.</summary>
        public const int CursorSensitivity = 227;
        /// <summary>Ring and radial selection: inner radius (ring: 0..1 of the size).</summary>
        public const int InnerRadius = 228;
        /// <summary>Cam and scaled space: scale of the plane.</summary>
        public const int PlaneScale = 229;
        /// <summary>Cam space: rotation angle in radians around RotationAxis.</summary>
        public const int RotationAngle = 230;
        /// <summary>Lists: smallest length of the entry list.</summary>
        public const int MinLength = 231;

        // bool
        /// <summary>Text box: the player may edit the text.</summary>
        public const int EnableEditing = 240;
        /// <summary>Text box: the player may select text.</summary>
        public const int EnableHighlighting = 241;
        /// <summary>Text box: caret to the end when it gets the focus.</summary>
        public const int MoveToEndOnFocus = 242;
        /// <summary>Text box: clear the selection when the focus is lost.</summary>
        public const int ClearSelectionOnBlur = 243;
        /// <summary>Drop-down and tree: list opened (set to open or close).</summary>
        public const int Open = 244;
        /// <summary>Buttons and lists: use the focus colours while focused.</summary>
        public const int UseFocusFormatting = 245;
        /// <summary>Scroll bar and slider bar: vertical.</summary>
        public const int Vertical = 246;
        /// <summary>Slider bar: show the slider.</summary>
        public const int SliderVisible = 247;
        /// <summary>Slider bar: reverse direction.</summary>
        public const int Reverse = 248;
        /// <summary>Radial selection: select by mouse gesture.</summary>
        public const int UseGestureInput = 249;
        /// <summary>Cam space: in screen space instead of world space.</summary>
        public const int IsScreenSpace = 250;
        /// <summary>Cam space: scale with the screen resolution.</summary>
        public const int UseResScaling = 251;
        /// <summary>Read only. HUD space: in front of the camera.</summary>
        public const int IsInFront = 252;
        /// <summary>Read only. HUD space: facing the camera.</summary>
        public const int IsFacingCamera = 253;
        /// <summary>Terminal pages, categories, tiles and controls: enabled (shown).</summary>
        public const int Enabled = 254;
        /// <summary>Terminal drag box: snap to the screen edge.</summary>
        public const int AlignToEdge = 255;
        /// <summary>Line: connect the last point with the first.</summary>
        public const int ClosedLine = 256;

        // int
        /// <summary>Text box: character used for new lines (as number).</summary>
        public const int NewLineChar = 260;
        /// <summary>Lists, trees, radial selection: selected index (-1 = none; set -1 to clear).</summary>
        public const int SelectedIndex = 261;
        /// <summary>Drop-down and lists: smallest number of shown entries.</summary>
        public const int MinVisibleCount = 262;
        /// <summary>Radial selection: number of slots.</summary>
        public const int MaxEntryCount = 263;
        /// <summary>Radial selection: highlighted index (-1 = none).</summary>
        public const int HighlightIndex = 264;
        /// <summary>Polygon and ring: number of sides.</summary>
        public const int Sides = 265;
        /// <summary>Read only. Number of entries made with Ui.ItemInsert (or tree and radial children).</summary>
        public const int ItemCount = 266;

        // Color
        /// <summary>Lists: colour of the selection tab.</summary>
        public const int TabColor = 280;
        /// <summary>Buttons and lists: text colour while focused.</summary>
        public const int FocusTextColor = 281;
        /// <summary>Colour pickers and terminal colour picker: the value.</summary>
        public const int ValueColor = 282;
        /// <summary>On/off button: colour of the not selected side.</summary>
        public const int UnselectedColor = 283;
        /// <summary>On/off button and radial selection: colour of the selected side or slot.</summary>
        public const int SelectionColor = 284;
        /// <summary>Background of the tooltip of this element; transparent = Rich HUD default.</summary>
        public const int TooltipColor = 285;
        /// <summary>Slider: bar colour while focused.</summary>
        public const int BarFocusColor = 286;
        /// <summary>Slider: slider colour while focused.</summary>
        public const int SliderFocusColor = 287;

        // string
        /// <summary>Lists and terminal lists: key of the selected entry (set to select by key).</summary>
        public const int SelectedKey = 300;
        /// <summary>On/off buttons: text of the on side (markup).</summary>
        public const int OnText = 301;
        /// <summary>On/off buttons: text of the off side (markup).</summary>
        public const int OffText = 302;
        /// <summary>Double label box: right text (markup).</summary>
        public const int RightText = 303;
        /// <summary>Named slider and terminal slider: value text.</summary>
        public const int ValueText = 304;
        /// <summary>Terminal category and text page: sub-header.</summary>
        public const int SubHeader = 305;
        /// <summary>Terminal text page: page text (markup); terminal text field: the value.</summary>
        public const int BodyText = 306;

        // Vector3D
        /// <summary>HSV colour picker: hue (0..360), saturation and value (0..100).</summary>
        public const int ColorHsv = 320;
        /// <summary>Cam space: rotation axis.</summary>
        public const int RotationAxis = 321;
        /// <summary>Cam space: offset from the camera.</summary>
        public const int TransformOffset = 322;
        /// <summary>Read only. HUD space: cursor position in the plane (Z = depth).</summary>
        public const int SpaceCursor = 323;
    }

    /// <summary>Events (API 2). Values are fixed. 1-19 belong to an element, 20-23 to the service.</summary>
    public static class UiEvent
    {
        /// <summary>Left click on a clickable element; terminal button pressed.</summary>
        public const int Click = 1;
        /// <summary>Right click.</summary>
        public const int RightClick = 2;
        /// <summary>Cursor moved onto the element.</summary>
        public const int CursorEnter = 3;
        /// <summary>Cursor left the element.</summary>
        public const int CursorExit = 4;
        /// <summary>Text of a text field or text box changed; argument: the text.</summary>
        public const int TextChanged = 5;
        /// <summary>Enter pressed in a text field; argument: the text.</summary>
        public const int Submit = 6;
        /// <summary>Value changed. Argument: "1"/"0" (check boxes, on/off), the selected key (lists), the handle of the selected child (tree box), "#RRGGBBAA" (colour pickers), the text (terminal text field), null otherwise (read the value).</summary>
        public const int ValueChanged = 7;
        /// <summary>Link in a label clicked; argument: the link target.</summary>
        public const int Link = 8;
        /// <summary>Size of a window body or a panel changed.</summary>
        public const int Resized = 9;
        /// <summary>Window was hidden (close key, Escape or Visible = false).</summary>
        public const int Closed = 10;
        /// <summary>Header of a tree list or tree box was clicked (opened or closed; read Open).</summary>
        public const int Toggled = 11;
        /// <summary>Key bind of the mod pressed (handle 0); argument: bind name.</summary>
        public const int BindPressed = 20;
        /// <summary>UI is ready (handle 0). Build windows now; sent again after a Rich HUD restart.</summary>
        public const int Ready = 21;
        /// <summary>All elements of all mods are gone (handle 0), e.g. Rich HUD restarted.</summary>
        public const int Lost = 22;
        /// <summary>UI will not work in this session (handle 0): dedicated server or Rich HUD Master missing.</summary>
        public const int Unavailable = 23;
    }

    /// <summary>Position relative to the parent (Rich HUD ParentAlignments, flags). Inner = inside the parent's edge.</summary>
    public static class UiAlign
    {
        /// <summary>Centred on the parent.</summary>
        public const int Center = 0;
        /// <summary>Left of the parent.</summary>
        public const int Left = 1;
        /// <summary>Below the parent.</summary>
        public const int Bottom = 2;
        /// <summary>Right of the parent.</summary>
        public const int Right = 4;
        /// <summary>Above the parent.</summary>
        public const int Top = 8;
        /// <summary>Horizontally inside the parent's edge.</summary>
        public const int InnerH = 16;
        /// <summary>Vertically inside the parent's edge.</summary>
        public const int InnerV = 32;
        /// <summary>Respect the parent's padding.</summary>
        public const int UsePadding = 64;
        /// <summary>Inside the parent's edge on both axes.</summary>
        public const int Inner = InnerH | InnerV;
        /// <summary>Inside, at the left edge.</summary>
        public const int InnerLeft = Inner | Left;
        /// <summary>Inside, at the right edge.</summary>
        public const int InnerRight = Inner | Right;
        /// <summary>Inside, at the top edge.</summary>
        public const int InnerTop = Inner | Top;
        /// <summary>Inside, at the bottom edge.</summary>
        public const int InnerBottom = Inner | Bottom;
        /// <summary>Inside, top left corner.</summary>
        public const int InnerTopLeft = Inner | Top | Left;
        /// <summary>Inside, top right corner.</summary>
        public const int InnerTopRight = Inner | Top | Right;
        /// <summary>Inside, bottom left corner.</summary>
        public const int InnerBottomLeft = Inner | Bottom | Left;
        /// <summary>Inside, bottom right corner.</summary>
        public const int InnerBottomRight = Inner | Bottom | Right;
    }

    /// <summary>Size taken from the parent (Rich HUD DimAlignments).</summary>
    public static class UiDim
    {
        /// <summary>Own size.</summary>
        public const int None = 0;
        /// <summary>Width of the parent.</summary>
        public const int Width = 1;
        /// <summary>Height of the parent.</summary>
        public const int Height = 2;
        /// <summary>Size of the parent.</summary>
        public const int Size = 3;
        /// <summary>Flag: ignore the parent's padding.</summary>
        public const int IgnorePadding = 4;
        /// <summary>Width of the parent without padding.</summary>
        public const int UnpaddedWidth = 5;
        /// <summary>Height of the parent without padding.</summary>
        public const int UnpaddedHeight = 6;
        /// <summary>Size of the parent without padding.</summary>
        public const int UnpaddedSize = 7;
    }

    /// <summary>Sizing of chains and scroll boxes (Rich HUD HudChainSizingModes, flags).</summary>
    public static class UiSizing
    {
        /// <summary>No sizing.</summary>
        public const int None = 0;
        /// <summary>Clamp the chain off its axis.</summary>
        public const int ClampChainOffAxis = 4;
        /// <summary>Clamp the chain along its axis.</summary>
        public const int ClampChainAlignAxis = 8;
        /// <summary>Chain fits its members off its axis.</summary>
        public const int FitChainOffAxis = 16;
        /// <summary>Chain fits its members along its axis.</summary>
        public const int FitChainAlignAxis = 32;
        /// <summary>Chain fits its members on both axes.</summary>
        public const int FitChainBoth = 48;
        /// <summary>Clamp the members off the axis.</summary>
        public const int ClampMembersOffAxis = 64;
        /// <summary>Clamp the members along the axis.</summary>
        public const int ClampMembersAlignAxis = 128;
        /// <summary>Members fill the chain off the axis.</summary>
        public const int FitMembersOffAxis = 256;
        /// <summary>Members fill the chain along the axis.</summary>
        public const int FitMembersAlignAxis = 512;
        /// <summary>Members fill the chain on both axes.</summary>
        public const int FitMembersBoth = 768;
    }

    /// <summary>Text alignment, font style flags and character filters.</summary>
    public static class UiText
    {
        /// <summary>Left aligned.</summary>
        public const int Left = 0;
        /// <summary>Centred.</summary>
        public const int Center = 1;
        /// <summary>Right aligned.</summary>
        public const int Right = 2;
        /// <summary>Regular font style.</summary>
        public const int Regular = 0;
        /// <summary>Bold flag.</summary>
        public const int Bold = 1;
        /// <summary>Italic flag.</summary>
        public const int Italic = 2;
        /// <summary>Underline flag.</summary>
        public const int Underline = 4;
        /// <summary>Filter: one line (no line breaks).</summary>
        public const int FilterLine = 0;
        /// <summary>Filter: digits only.</summary>
        public const int FilterDigits = 1;
        /// <summary>Filter: number (digits, sign, point, comma).</summary>
        public const int FilterNumber = 2;
        /// <summary>Filter: anything.</summary>
        public const int FilterAny = 3;
    }

    /// <summary>Values of <see cref="SyntrixUi.ScreenInfo"/>.</summary>
    public static class UiScreen
    {
        /// <summary>Screen width in pixels.</summary>
        public const int Width = 0;
        /// <summary>Screen height in pixels.</summary>
        public const int Height = 1;
        /// <summary>Aspect ratio (width / height).</summary>
        public const int AspectRatio = 2;
        /// <summary>Field of view in radians.</summary>
        public const int Fov = 3;
        /// <summary>Scale that compensates the field of view for world billboards.</summary>
        public const int FovScale = 4;
        /// <summary>Opacity of UI backgrounds from the game settings (0..1).</summary>
        public const int BackgroundOpacity = 5;
        /// <summary>Resolution scale of the high-DPI root.</summary>
        public const int ResScale = 6;
        /// <summary>Rich HUD input mode: 0 default, 1 cursor, 2 text input.</summary>
        public const int InputMode = 7;
        /// <summary>1 while the cursor is visible.</summary>
        public const int CursorVisible = 8;
        /// <summary>1 while an element captures the cursor.</summary>
        public const int CursorCaptured = 9;
    }

    /// <summary>
    /// API 2. Windows, HUD elements, formatted text, key binds and Rich HUD terminal pages through Syntrix (Rich HUD inside).
    /// Create one per mod after the SyntrixApi, call Load in LoadData and Unload in UnloadData.
    /// Build elements in onReady (it runs again after a Rich HUD restart, when all elements were lost).
    /// Every element is a handle; the wrappers never throw and do nothing without the framework.
    /// Texts use markup: [b] [i] [u] [color=#RRGGBB] [size=1.2] [font=Name] [align=center] [link=target] ... [/tag], "[[" = "[".
    /// The factories for the controls are in SyntrixUi.Controls.cs, those for the terminal in SyntrixUi.Terminal.cs.
    /// </summary>
    public partial class SyntrixUi
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
        private Func<int, int, string, string, int> _itemInsert;
        private Action<int, int> _itemRemove;
        private Action<int, int, string> _itemText;
        private Action<int, int, bool> _itemEnabled;
        private Func<int, int, string> _itemKey;
        private Func<int, string, int> _itemFind;
        private Action<int, int, Vector3D> _setVector3;
        private Func<int, int, Vector3D> _getVector3;
        private Action<int, MatrixD> _setMatrix;
        private Action<int, Vector2[], int[]> _setPoints;
        private Func<string> _clipboard;
        private Action<string> _setClipboard;
        private Action<int> _terminal;
        private Func<bool> _terminalOpen;
        private Func<int, float> _screenInfo;
        private Func<Vector3D> _cursorWorld;

        /// <summary>Makes the UI dock of a mod; call Load next.</summary>
        /// <param name="api">The mod's Syntrix API.</param>
        public SyntrixUi(SyntrixApi api) { _api = api; }

        /// <summary>
        /// Starts the UI service for this mod. onReady: build your elements; onLost: all elements are gone
        /// (onReady follows when Rich HUD is back); onUnavailable: no UI this session (Rich HUD Master missing).
        /// </summary>
        /// <param name="onReady">Build the elements here.</param>
        /// <param name="onLost">All elements are gone.</param>
        /// <param name="onUnavailable">No UI this session.</param>
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
                _itemInsert = Get<Func<int, int, string, string, int>>("Ui.ItemInsert");
                _itemRemove = Get<Action<int, int>>("Ui.ItemRemove");
                _itemText = Get<Action<int, int, string>>("Ui.ItemText");
                _itemEnabled = Get<Action<int, int, bool>>("Ui.ItemEnabled");
                _itemKey = Get<Func<int, int, string>>("Ui.ItemKey");
                _itemFind = Get<Func<int, string, int>>("Ui.ItemFind");
                _setVector3 = Get<Action<int, int, Vector3D>>("Ui.SetVector3");
                _getVector3 = Get<Func<int, int, Vector3D>>("Ui.GetVector3");
                _setMatrix = Get<Action<int, MatrixD>>("Ui.SetMatrix");
                _setPoints = Get<Action<int, Vector2[], int[]>>("Ui.SetPoints");
                _clipboard = Get<Func<string>>("Ui.Clipboard");
                _setClipboard = Get<Action<string>>("Ui.SetClipboard");
                _terminal = Get<Action<int>>("Ui.Terminal");
                _terminalOpen = Get<Func<bool>>("Ui.TerminalOpen");
                _screenInfo = Get<Func<int, float>>("Ui.ScreenInfo");
                _cursorWorld = Get<Func<Vector3D>>("Ui.CursorWorld");
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

        /// <summary>Cursor position in the world (for HUD spaces and world billboards).</summary>
        public Vector3D CursorWorldPosition { get { return _cursorWorld != null ? _cursorWorld() : Vector3D.Zero; } }

        /// <summary>A screen value, see <see cref="UiScreen"/>.</summary>
        /// <param name="what">UiScreen constant.</param>
        public float ScreenInfo(int what) { return _screenInfo != null ? _screenInfo(what) : 0f; }

        /// <summary>The Rich HUD clipboard (shared by all text boxes) as plain text.</summary>
        public string Clipboard
        {
            get { return _clipboard != null ? _clipboard() : null; }
            set { if (_setClipboard != null) _setClipboard(value); }
        }

        /// <summary>Shows the cursor outside of windows (open modal windows show it anyway).</summary>
        /// <param name="on">True to show it.</param>
        public void ShowCursor(bool on) { if (_cursor != null) _cursor(on); }

        /// <summary>Size of a markup text; styleFrom gives the base format (font size, font), wrapWidth > 0 wraps.</summary>
        /// <param name="markup">Text with markup.</param>
        /// <param name="wrapWidth">Wrap width; 0 = no wrapping.</param>
        /// <param name="styleFrom">Element whose base format is used; null = default.</param>
        public Vector2 MeasureText(string markup, float wrapWidth = 0f, UiElement styleFrom = null)
        {
            return _measure != null ? _measure(styleFrom != null ? styleFrom.Handle : 0, markup, wrapWidth) : Vector2.Zero;
        }

        /// <summary>Makes text safe for markup ("[" becomes "[[").</summary>
        /// <param name="text">Plain text.</param>
        public static string Escape(string text) { return text == null ? null : text.Replace("[", "[["); }

        /// <summary>
        /// Key bind of this mod, e.g. Bind("OpenWiki", "F11", Toggle) or "Control+F1". Players change it on the rebind
        /// page (BindPage); Syntrix saves the choice. Not raised while typing or in the chat. May be called before ready.
        /// </summary>
        /// <param name="name">Bind name, unique in this mod.</param>
        /// <param name="keys">Default keys joined with '+'.</param>
        /// <param name="pressed">Runs when the keys are pressed.</param>
        public void Bind(string name, string keys, Action pressed)
        {
            if (string.IsNullOrEmpty(name))
                return;
            _binds[name] = new BindEntry { Keys = keys, Pressed = pressed };
            if (_bind != null)
                _bind(name, keys);
        }

        /// <summary>Current keys of a bind, e.g. "F11" (null if unknown).</summary>
        /// <param name="name">Bind name.</param>
        public string BindKeys(string name) { return _bindKeys != null ? _bindKeys(name) : null; }

        /// <summary>Page with this mod's binds in the Rich HUD terminal (F2). Call once, after the Bind calls.</summary>
        /// <param name="title">Page title; empty uses the mod name.</param>
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
        /// <param name="title">Header text (markup).</param>
        public UiWindow Window(string title = null) { var w = new UiWindow(this, Create(UiKind.Window, null)); if (title != null) w.Title = title; return w; }

        /// <summary>Surface with background, border, highlight and mouse events.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        public UiPanel Panel(UiElement parent) { return new UiPanel(this, Create(UiKind.Panel, parent)); }

        /// <summary>Text with markup and links.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        /// <param name="text">Text (markup).</param>
        public UiLabel Label(UiElement parent, string text = null) { var l = new UiLabel(this, Create(UiKind.Label, parent)); if (text != null) l.Text = text; return l; }

        /// <summary>Text on a coloured background.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        /// <param name="text">Text (markup).</param>
        public UiLabelBox LabelBox(UiElement parent, string text = null) { var l = new UiLabelBox(this, Create(UiKind.LabelBox, parent)); if (text != null) l.Text = text; return l; }

        /// <summary>Text button.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        /// <param name="text">Text (markup).</param>
        public UiButton Button(UiElement parent, string text = null) { var b = new UiButton(this, Create(UiKind.Button, parent)); if (text != null) b.Text = text; return b; }

        /// <summary>One-line text entry.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        public UiTextField TextField(UiElement parent) { return new UiTextField(this, Create(UiKind.TextField, parent)); }

        /// <summary>Coloured rectangle or image (texture = transparent material subtype).</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        /// <param name="texture">Transparent material subtype; null = plain colour.</param>
        public UiBox Box(UiElement parent, string texture = null) { var b = new UiBox(this, Create(UiKind.Box, parent)); if (texture != null) b.Texture = texture; return b; }

        /// <summary>Frame line.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        public UiBorder Border(UiElement parent) { return new UiBorder(this, Create(UiKind.Border, parent)); }

        /// <summary>Scrolling list; children become entries.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        /// <param name="vertical">False for a horizontal list.</param>
        public UiScrollBox ScrollBox(UiElement parent, bool vertical = true) { return new UiScrollBox(this, Create(vertical ? UiKind.ScrollBox : UiKind.ScrollBoxHorizontal, parent)); }

        /// <summary>Stack of members.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        /// <param name="vertical">False for a horizontal stack.</param>
        public UiChain Chain(UiElement parent, bool vertical = true) { return new UiChain(this, Create(vertical ? UiKind.Chain : UiKind.ChainHorizontal, parent)); }

        /// <summary>Invisible element: anchor, spacer or group.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        public UiElement Empty(UiElement parent) { return new UiElement(this, Create(UiKind.Empty, parent)); }

        /// <summary>Invisible element with mouse events.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        public UiElement ClickArea(UiElement parent) { return new UiElement(this, Create(UiKind.ClickArea, parent)); }

        /// <summary>Check box.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
        public UiCheckBox CheckBox(UiElement parent) { return new UiCheckBox(this, Create(UiKind.CheckBox, parent)); }

        /// <summary>Slider with background.</summary>
        /// <param name="parent">Parent element; null = Root.</param>
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
            for (int ev = 1; ev < UiEvent.BindPressed; ev++)
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
        internal void Set(int h, int p, Vector3D v) { if (_setVector3 != null) _setVector3(h, p, v); }
        internal bool GetBool(int h, int p) { return _getBool != null && _getBool(h, p); }
        internal int GetInt(int h, int p) { return _getInt != null ? _getInt(h, p) : 0; }
        internal float GetFloat(int h, int p) { return _getFloat != null ? _getFloat(h, p) : 0f; }
        internal Vector2 GetVector(int h, int p) { return _getVector != null ? _getVector(h, p) : Vector2.Zero; }
        internal Color GetColor(int h, int p) { return _getColor != null ? _getColor(h, p) : Color.Transparent; }
        internal string GetText(int h, int p) { return _getText != null ? _getText(h, p) : null; }
        internal Vector3D GetVector3(int h, int p) { return _getVector3 != null ? _getVector3(h, p) : Vector3D.Zero; }
        internal int ItemInsert(int h, int index, string key, string text) { return _itemInsert != null ? _itemInsert(h, index, key, text) : -1; }
        internal void ItemRemove(int h, int index) { if (_itemRemove != null) _itemRemove(h, index); }
        internal void ItemText(int h, int index, string text) { if (_itemText != null) _itemText(h, index, text); }
        internal void ItemEnabled(int h, int index, bool on) { if (_itemEnabled != null) _itemEnabled(h, index, on); }
        internal string ItemKey(int h, int index) { return _itemKey != null ? _itemKey(h, index) : null; }
        internal int ItemFind(int h, string key) { return _itemFind != null ? _itemFind(h, key) : -1; }
        internal void SetMatrix(int h, MatrixD m) { if (_setMatrix != null) _setMatrix(h, m); }
        internal void SetPoints(int h, Vector2[] points, int[] indices) { if (_setPoints != null) _setPoints(h, points, indices); }
        internal void Terminal(int mode) { if (_terminal != null) _terminal(mode); }
        internal bool TerminalOpen { get { return _terminalOpen != null && _terminalOpen(); } }

        /// <summary>Parses "#RRGGBBAA" (or "#RRGGBB") from a ValueChanged argument.</summary>
        /// <param name="hex">The text.</param>
        public static Color ParseColor(string hex)
        {
            if (hex == null || hex.Length < 7 || hex[0] != '#')
                return Color.Transparent;
            uint v = 0;
            for (int i = 1; i < hex.Length && i < 9; i++)
            {
                char c = char.ToUpperInvariant(hex[i]);
                int d = c >= '0' && c <= '9' ? c - '0' : c >= 'A' && c <= 'F' ? c - 'A' + 10 : -1;
                if (d < 0)
                    return Color.Transparent;
                v = (v << 4) | (uint)d;
            }
            if (hex.Length < 9)
                return new Color((int)(v >> 16) & 255, (int)(v >> 8) & 255, (int)v & 255);
            return new Color((int)(v >> 24) & 255, (int)(v >> 16) & 255, (int)(v >> 8) & 255, (int)v & 255);
        }
    }

    /// <summary>
    /// API 2. Any UI element. Handle 0 means "not made" (framework or Rich HUD missing); every call is then ignored.
    /// Use Set/Get with UiProp for properties the typed wrappers do not list.
    /// </summary>
    public class UiElement
    {
        /// <summary>The UI dock that made the element.</summary>
        public readonly SyntrixUi Ui;

        /// <summary>Handle of the element; 0 = not made.</summary>
        public readonly int Handle;

        internal UiElement(SyntrixUi ui, int handle) { Ui = ui; Handle = handle; }

        /// <summary>True when the element was made.</summary>
        public bool IsValid { get { return Handle != 0; } }

        /// <summary>Sets a bool property.</summary>
        /// <param name="prop">UiProp constant.</param>
        /// <param name="v">New value.</param>
        public void Set(int prop, bool v) { Ui.Set(Handle, prop, v); }

        /// <summary>Sets an int property.</summary>
        /// <param name="prop">UiProp constant.</param>
        /// <param name="v">New value.</param>
        public void Set(int prop, int v) { Ui.Set(Handle, prop, v); }

        /// <summary>Sets a float property.</summary>
        /// <param name="prop">UiProp constant.</param>
        /// <param name="v">New value.</param>
        public void Set(int prop, float v) { Ui.Set(Handle, prop, v); }

        /// <summary>Sets a Vector2 property.</summary>
        /// <param name="prop">UiProp constant.</param>
        /// <param name="v">New value.</param>
        public void Set(int prop, Vector2 v) { Ui.Set(Handle, prop, v); }

        /// <summary>Sets a Color property.</summary>
        /// <param name="prop">UiProp constant.</param>
        /// <param name="v">New value.</param>
        public void Set(int prop, Color v) { Ui.Set(Handle, prop, v); }

        /// <summary>Sets a string property.</summary>
        /// <param name="prop">UiProp constant.</param>
        /// <param name="v">New value.</param>
        public void Set(int prop, string v) { Ui.Set(Handle, prop, v); }

        /// <summary>Sets a Vector3D property.</summary>
        /// <param name="prop">UiProp constant.</param>
        /// <param name="v">New value.</param>
        public void Set(int prop, Vector3D v) { Ui.Set(Handle, prop, v); }

        /// <summary>Reads a bool property.</summary>
        /// <param name="prop">UiProp constant.</param>
        public bool GetBool(int prop) { return Ui.GetBool(Handle, prop); }

        /// <summary>Reads an int property.</summary>
        /// <param name="prop">UiProp constant.</param>
        public int GetInt(int prop) { return Ui.GetInt(Handle, prop); }

        /// <summary>Reads a float property.</summary>
        /// <param name="prop">UiProp constant.</param>
        public float GetFloat(int prop) { return Ui.GetFloat(Handle, prop); }

        /// <summary>Reads a Vector2 property.</summary>
        /// <param name="prop">UiProp constant.</param>
        public Vector2 GetVector(int prop) { return Ui.GetVector(Handle, prop); }

        /// <summary>Reads a Color property.</summary>
        /// <param name="prop">UiProp constant.</param>
        public Color GetColor(int prop) { return Ui.GetColor(Handle, prop); }

        /// <summary>Reads a string property.</summary>
        /// <param name="prop">UiProp constant.</param>
        public string GetText(int prop) { return Ui.GetText(Handle, prop); }

        /// <summary>Reads a Vector3D property.</summary>
        /// <param name="prop">UiProp constant.</param>
        public Vector3D GetVector3(int prop) { return Ui.GetVector3(Handle, prop); }

        /// <summary>Offset from the aligned position (cheap, fine every frame).</summary>
        public Vector2 Offset { get { return GetVector(UiProp.Offset); } set { Set(UiProp.Offset, value); } }

        /// <summary>Size of the element.</summary>
        public Vector2 Size { get { return GetVector(UiProp.Size); } set { Set(UiProp.Size, value); } }

        /// <summary>Width of the element.</summary>
        public float Width { get { return GetFloat(UiProp.Width); } set { Set(UiProp.Width, value); } }

        /// <summary>Height of the element.</summary>
        public float Height { get { return GetFloat(UiProp.Height); } set { Set(UiProp.Height, value); } }

        /// <summary>Inner padding.</summary>
        public Vector2 Padding { get { return GetVector(UiProp.Padding); } set { Set(UiProp.Padding, value); } }

        /// <summary>Shown. In a scroll box this also takes the entry out of the list; terminal members are disabled.</summary>
        public bool Visible { get { return GetBool(UiProp.Visible); } set { Set(UiProp.Visible, value); } }

        /// <summary>UiAlign flags.</summary>
        public int ParentAlign { get { return GetInt(UiProp.ParentAlign); } set { Set(UiProp.ParentAlign, value); } }

        /// <summary>UiDim value.</summary>
        public int DimAlign { get { return GetInt(UiProp.DimAlign); } set { Set(UiProp.DimAlign, value); } }

        /// <summary>Drawing order offset among siblings.</summary>
        public int ZOffset { get { return GetInt(UiProp.ZOffset); } set { Set(UiProp.ZOffset, value); } }

        /// <summary>The element takes mouse input.</summary>
        public bool InputEnabled { get { return GetBool(UiProp.InputEnabled); } set { Set(UiProp.InputEnabled, value); } }

        /// <summary>Clip children to this element.</summary>
        public bool Masking { get { return GetBool(UiProp.Masking); } set { Set(UiProp.Masking, value); } }

        /// <summary>True while the cursor is over the element.</summary>
        public bool IsMousedOver { get { return GetBool(UiProp.MousedOver); } }

        /// <summary>Centre on the screen (read only).</summary>
        public Vector2 Position { get { return GetVector(UiProp.Position); } }

        /// <summary>Number of children.</summary>
        public int Count { get { return GetInt(UiProp.Count); } }

        /// <summary>Tooltip (markup) for elements with mouse input and terminal controls.</summary>
        public string Tooltip { set { Set(UiProp.Tooltip, value); } }

        /// <summary>Background colour of this element's tooltip; transparent = Rich HUD default.</summary>
        public Color TooltipColor { get { return GetColor(UiProp.TooltipColor); } set { Set(UiProp.TooltipColor, value); } }

        /// <summary>Removes the element and its children (terminal members are disabled, Rich HUD cannot remove them).</summary>
        public void Remove() { Ui.Remove(Handle); }

        /// <summary>Removes all children (e.g. the entries of a scroll box).</summary>
        public void Clear() { Ui.Clear(Handle); }

        /// <summary>Moves the element to another parent (null = root). Not for terminal members.</summary>
        /// <param name="parent">New parent; null = Root.</param>
        public void MoveTo(UiElement parent) { Ui.SetParent(Handle, parent != null ? parent.Handle : 0); }

        /// <summary>Handler for a UiEvent of this element (one per event; null removes it). Runs outside Rich HUD's update.</summary>
        /// <param name="uiEvent">UiEvent constant.</param>
        /// <param name="handler">Gets the event argument; null removes the handler.</param>
        public void On(int uiEvent, Action<string> handler) { Ui.Listen(Handle, uiEvent, handler); }

        /// <summary>Left click.</summary>
        /// <param name="a">Handler; null removes it.</param>
        public void OnClick(Action a) { On(UiEvent.Click, a == null ? (Action<string>)null : s => a()); }

        /// <summary>Right click.</summary>
        /// <param name="a">Handler; null removes it.</param>
        public void OnRightClick(Action a) { On(UiEvent.RightClick, a == null ? (Action<string>)null : s => a()); }

        /// <summary>Cursor moved onto the element.</summary>
        /// <param name="a">Handler; null removes it.</param>
        public void OnCursorEnter(Action a) { On(UiEvent.CursorEnter, a == null ? (Action<string>)null : s => a()); }

        /// <summary>Cursor left the element.</summary>
        /// <param name="a">Handler; null removes it.</param>
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

        /// <summary>Base text colour.</summary>
        public Color TextColor { get { return GetColor(UiProp.TextColor); } set { Set(UiProp.TextColor, value); } }

        /// <summary>Base font size (1 = normal).</summary>
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

        /// <summary>Wrap the text at WrapWidth.</summary>
        public bool Wrap { get { return GetBool(UiProp.Wrap); } set { Set(UiProp.Wrap, value); } }

        /// <summary>Width at which wrapped text breaks.</summary>
        public float WrapWidth { get { return GetFloat(UiProp.WrapWidth); } set { Set(UiProp.WrapWidth, value); } }

        /// <summary>Centre the text vertically.</summary>
        public bool VertCenter { get { return GetBool(UiProp.VertCenter); } set { Set(UiProp.VertCenter, value); } }

        /// <summary>Colour of links.</summary>
        public Color LinkColor { get { return GetColor(UiProp.LinkColor); } set { Set(UiProp.LinkColor, value); } }

        /// <summary>Wraps at the parent's inner width minus margin, at most max (0 = no limit); follows resizing.</summary>
        /// <param name="margin">Space left free.</param>
        /// <param name="max">Largest wrap width; 0 = no limit.</param>
        public void WrapToParent(float margin, float max = 0f)
        {
            Set(UiProp.WrapMax, max);
            Set(UiProp.WrapToParent, margin);
        }

        /// <summary>Click on a link; the handler gets the target.</summary>
        /// <param name="handler">Gets the link target; null removes it.</param>
        public void OnLink(Action<string> handler) { On(UiEvent.Link, handler); }
    }

    /// <summary>API 2. Text on a coloured background (badges, logos).</summary>
    public class UiLabelBox : UiTextElement
    {
        internal UiLabelBox(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Background colour.</summary>
        public Color Color { get { return GetColor(UiProp.Color); } set { Set(UiProp.Color, value); } }

        /// <summary>Padding around the text.</summary>
        public Vector2 TextPadding { get { return GetVector(UiProp.TextPadding); } set { Set(UiProp.TextPadding, value); } }

        /// <summary>Size follows the text.</summary>
        public bool AutoResize { get { return GetBool(UiProp.AutoResize); } set { Set(UiProp.AutoResize, value); } }

        /// <summary>Centre the text vertically.</summary>
        public bool VertCenter { get { return GetBool(UiProp.VertCenter); } set { Set(UiProp.VertCenter, value); } }
    }

    /// <summary>API 2. Text button with highlight colour and optional border.</summary>
    public class UiButton : UiTextElement
    {
        internal UiButton(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Background colour.</summary>
        public Color Color { get { return GetColor(UiProp.Color); } set { Set(UiProp.Color, value); } }

        /// <summary>Background under the cursor.</summary>
        public Color HighlightColor { get { return GetColor(UiProp.HighlightColor); } set { Set(UiProp.HighlightColor, value); } }

        /// <summary>Highlight under the cursor.</summary>
        public bool HighlightEnabled { get { return GetBool(UiProp.HighlightEnabled); } set { Set(UiProp.HighlightEnabled, value); } }

        /// <summary>Border colour (makes the border).</summary>
        public Color BorderColor { set { Set(UiProp.BorderColor, value); } }

        /// <summary>Border thickness.</summary>
        public float BorderThickness { get { return GetFloat(UiProp.BorderThickness); } set { Set(UiProp.BorderThickness, value); } }

        /// <summary>Padding around the text.</summary>
        public Vector2 TextPadding { get { return GetVector(UiProp.TextPadding); } set { Set(UiProp.TextPadding, value); } }

        /// <summary>Size follows the text.</summary>
        public bool AutoResize { get { return GetBool(UiProp.AutoResize); } set { Set(UiProp.AutoResize, value); } }
    }

    /// <summary>API 2. Surface with background, optional border, highlight colour and mouse events: rows, cards, tabs.</summary>
    public class UiPanel : UiElement
    {
        internal UiPanel(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Background colour.</summary>
        public Color Color { get { return GetColor(UiProp.Color); } set { Set(UiProp.Color, value); } }

        /// <summary>Background under the cursor; also turns on mouse input.</summary>
        public Color HighlightColor { get { return GetColor(UiProp.HighlightColor); } set { Set(UiProp.HighlightColor, value); } }

        /// <summary>Border colour (makes the border).</summary>
        public Color BorderColor { set { Set(UiProp.BorderColor, value); } }

        /// <summary>Border thickness.</summary>
        public float BorderThickness { get { return GetFloat(UiProp.BorderThickness); } set { Set(UiProp.BorderThickness, value); } }

        /// <summary>Background texture (transparent material subtype).</summary>
        public string Texture { get { return GetText(UiProp.Texture); } set { Set(UiProp.Texture, value); } }

        /// <summary>Background area as its own element (children drawn behind the panel's content).</summary>
        public UiElement Background { get { return new UiElement(Ui, Ui.Part(Handle, 1)); } }

        /// <summary>Raised when the size changed (also once after creation).</summary>
        /// <param name="a">Handler; null removes it.</param>
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
        /// <param name="offset">Top left pixel of the region.</param>
        /// <param name="size">Pixel size of the region.</param>
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

        /// <summary>Line colour.</summary>
        public Color Color { get { return GetColor(UiProp.Color); } set { Set(UiProp.Color, value); } }

        /// <summary>Line thickness.</summary>
        public float Thickness { get { return GetFloat(UiProp.BorderThickness); } set { Set(UiProp.BorderThickness, value); } }
    }

    /// <summary>API 2. One-line text entry: click to type, Enter/Escape/click elsewhere stops.</summary>
    public class UiTextField : UiTextElement
    {
        internal UiTextField(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Shown while empty (markup).</summary>
        public string Placeholder { get { return GetText(UiProp.Placeholder); } set { Set(UiProp.Placeholder, value); } }

        /// <summary>Background colour.</summary>
        public Color Color { get { return GetColor(UiProp.Color); } set { Set(UiProp.Color, value); } }

        /// <summary>Border colour.</summary>
        public Color BorderColor { set { Set(UiProp.BorderColor, value); } }

        /// <summary>Colour while typing.</summary>
        public Color FocusColor { set { Set(UiProp.FocusColor, value); } }

        /// <summary>Colour under the cursor.</summary>
        public Color HighlightColor { set { Set(UiProp.HighlightColor, value); } }

        /// <summary>Padding around the text.</summary>
        public Vector2 TextPadding { get { return GetVector(UiProp.TextPadding); } set { Set(UiProp.TextPadding, value); } }

        /// <summary>Longest text, 0 = no limit.</summary>
        public int MaxLength { get { return GetInt(UiProp.MaxLength); } set { Set(UiProp.MaxLength, value); } }

        /// <summary>UiText.FilterLine, FilterDigits, FilterNumber or FilterAny.</summary>
        public int CharFilter { get { return GetInt(UiProp.CharFilter); } set { Set(UiProp.CharFilter, value); } }

        /// <summary>True while the player types; set to start or stop typing.</summary>
        public bool Typing { get { return GetBool(UiProp.InputOpen); } set { Set(UiProp.InputOpen, value); } }

        /// <summary>Every change while typing; the handler gets the text.</summary>
        /// <param name="handler">Gets the text; null removes it.</param>
        public void OnTextChanged(Action<string> handler) { On(UiEvent.TextChanged, handler); }

        /// <summary>Enter pressed; the handler gets the text.</summary>
        /// <param name="handler">Gets the text; null removes it.</param>
        public void OnSubmit(Action<string> handler) { On(UiEvent.Submit, handler); }
    }

    /// <summary>API 2. Scrolling list; children become entries. Hidden children leave the list.</summary>
    public class UiScrollBox : UiElement
    {
        internal UiScrollBox(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Background colour.</summary>
        public Color Color { get { return GetColor(UiProp.Color); } set { Set(UiProp.Color, value); } }

        /// <summary>Space between entries.</summary>
        public float Spacing { get { return GetFloat(UiProp.Spacing); } set { Set(UiProp.Spacing, value); } }

        /// <summary>UiSizing flags (default FitMembersOffAxis: entries take the full width).</summary>
        public int Sizing { get { return GetInt(UiProp.Sizing); } set { Set(UiProp.Sizing, value); } }

        /// <summary>First shown entry; 0 scrolls to the top.</summary>
        public int Start { get { return GetInt(UiProp.Start); } set { Set(UiProp.Start, value); } }

        /// <summary>Last shown entry (read only).</summary>
        public int End { get { return GetInt(UiProp.End); } }

        /// <summary>Number of shown entries (read only).</summary>
        public int EnabledCount { get { return GetInt(UiProp.EnabledCount); } }

        /// <summary>Smooth scrolling.</summary>
        public bool SmoothScrolling { set { Set(UiProp.SmoothScrolling, value); } }

        /// <summary>Scrolling enabled.</summary>
        public bool Scrolling { set { Set(UiProp.Scrolling, value); } }

        /// <summary>Scroll bar colour.</summary>
        public Color BarColor { set { Set(UiProp.BarColor, value); } }

        /// <summary>Scroll bar colour under the cursor.</summary>
        public Color BarHighlight { set { Set(UiProp.BarHighlight, value); } }

        /// <summary>Slider colour.</summary>
        public Color SliderColor { set { Set(UiProp.SliderColor, value); } }

        /// <summary>Slider colour under the cursor.</summary>
        public Color SliderHighlight { set { Set(UiProp.SliderHighlight, value); } }

        /// <summary>Divider colour.</summary>
        public Color DividerColor { set { Set(UiProp.DividerColor, value); } }

        /// <summary>Background part: parent for overlays that are no entries (e.g. an empty-list hint).</summary>
        public UiElement Background { get { return new UiElement(Ui, Ui.Part(Handle, 1)); } }
    }

    /// <summary>API 2. Stack of members, vertical or horizontal.</summary>
    public class UiChain : UiElement
    {
        internal UiChain(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Space between members.</summary>
        public float Spacing { get { return GetFloat(UiProp.Spacing); } set { Set(UiProp.Spacing, value); } }

        /// <summary>UiSizing flags (default FitChainBoth: the chain takes the size of its members).</summary>
        public int Sizing { get { return GetInt(UiProp.Sizing); } set { Set(UiProp.Sizing, value); } }

        /// <summary>Smallest member size.</summary>
        public Vector2 MemberMinSize { set { Set(UiProp.MemberMinSize, value); } }

        /// <summary>Largest member size.</summary>
        public Vector2 MemberMaxSize { set { Set(UiProp.MemberMaxSize, value); } }
    }

    /// <summary>API 2. Window: header (Part Header) and body (children go there). Starts hidden.</summary>
    public class UiWindow : UiTextElement
    {
        internal UiWindow(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Header text (markup).</summary>
        public string Title { get { return GetText(UiProp.Title); } set { Set(UiProp.Title, value); } }

        /// <summary>Header colour.</summary>
        public Color HeaderColor { get { return GetColor(UiProp.HeaderColor); } set { Set(UiProp.HeaderColor, value); } }

        /// <summary>Body colour.</summary>
        public Color BodyColor { get { return GetColor(UiProp.BodyColor); } set { Set(UiProp.BodyColor, value); } }

        /// <summary>Border colour.</summary>
        public Color BorderColor { set { Set(UiProp.BorderColor, value); } }

        /// <summary>Height of the header.</summary>
        public float HeaderHeight { get { return GetFloat(UiProp.HeaderHeight); } set { Set(UiProp.HeaderHeight, value); } }

        /// <summary>Smallest size when resized by the player.</summary>
        public Vector2 MinSize { get { return GetVector(UiProp.MinSize); } set { Set(UiProp.MinSize, value); } }

        /// <summary>The player may move the window.</summary>
        public bool Draggable { get { return GetBool(UiProp.Draggable); } set { Set(UiProp.Draggable, value); } }

        /// <summary>The player may resize the window.</summary>
        public bool Resizable { get { return GetBool(UiProp.Resizable); } set { Set(UiProp.Resizable, value); } }

        /// <summary>Hide the game HUD while open (default false).</summary>
        public bool HideHud { get { return GetBool(UiProp.HideHud); } set { Set(UiProp.HideHud, value); } }

        /// <summary>Keep the view still while open (default true).</summary>
        public bool LockView { get { return GetBool(UiProp.LockView); } set { Set(UiProp.LockView, value); } }

        /// <summary>Escape closes the focused window (default true).</summary>
        public bool CloseOnEscape { get { return GetBool(UiProp.CloseOnEscape); } set { Set(UiProp.CloseOnEscape, value); } }

        /// <summary>Cursor on, mouse and camera kept from the game while open (default true).</summary>
        public bool Modal { get { return GetBool(UiProp.Modal); } set { Set(UiProp.Modal, value); } }

        /// <summary>True while the window has the focus.</summary>
        public bool Focused { get { return GetBool(UiProp.Focused); } }

        /// <summary>True while the window is shown.</summary>
        public bool IsOpen { get { return Visible; } }

        /// <summary>Header bar (a button-like label box: Color, Height, children for your own header content).</summary>
        public UiElement Header { get { return new UiElement(Ui, Ui.Part(Handle, 1)); } }

        /// <summary>Body area below the header.</summary>
        public UiElement Body { get { return new UiElement(Ui, Ui.Part(Handle, 2)); } }

        /// <summary>Shows the window.</summary>
        public void Open() { Visible = true; }

        /// <summary>Hides the window.</summary>
        public void Close() { Visible = false; }

        /// <summary>Shows or hides the window.</summary>
        public void Toggle() { Visible = !Visible; }

        /// <summary>Brings the open window to the front.</summary>
        public void Focus() { Ui.Focus(Handle); }

        /// <summary>The window was hidden (Escape, Close, Visible = false).</summary>
        /// <param name="a">Handler; null removes it.</param>
        public void OnClosed(Action a) { On(UiEvent.Closed, a == null ? (Action<string>)null : s => a()); }

        /// <summary>The body size changed (player resized the window, or first layout).</summary>
        /// <param name="a">Handler; null removes it.</param>
        public void OnResized(Action a) { On(UiEvent.Resized, a == null ? (Action<string>)null : s => a()); }
    }

    /// <summary>API 2. Check box.</summary>
    public class UiCheckBox : UiElement
    {
        internal UiCheckBox(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Checked state.</summary>
        public bool Checked { get { return GetBool(UiProp.Checked); } set { Set(UiProp.Checked, value); } }

        /// <summary>Background colour.</summary>
        public Color Color { set { Set(UiProp.Color, value); } }

        /// <summary>Border colour.</summary>
        public Color BorderColor { set { Set(UiProp.BorderColor, value); } }

        /// <summary>Tick colour.</summary>
        public Color TickColor { set { Set(UiProp.TickColor, value); } }

        /// <summary>The player changed the state.</summary>
        /// <param name="handler">Gets the new state; null removes it.</param>
        public void OnChanged(Action<bool> handler) { On(UiEvent.ValueChanged, handler == null ? (Action<string>)null : s => handler(s == "1")); }
    }

    /// <summary>API 2. Slider between Min and Max.</summary>
    public class UiSlider : UiElement
    {
        internal UiSlider(SyntrixUi ui, int handle) : base(ui, handle) { }

        /// <summary>Smallest value.</summary>
        public float Min { get { return GetFloat(UiProp.Min); } set { Set(UiProp.Min, value); } }

        /// <summary>Largest value.</summary>
        public float Max { get { return GetFloat(UiProp.Max); } set { Set(UiProp.Max, value); } }

        /// <summary>Current value.</summary>
        public float Value { get { return GetFloat(UiProp.Value); } set { Set(UiProp.Value, value); } }

        /// <summary>Value as 0..1.</summary>
        public float Percent { get { return GetFloat(UiProp.Percent); } set { Set(UiProp.Percent, value); } }

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
}
