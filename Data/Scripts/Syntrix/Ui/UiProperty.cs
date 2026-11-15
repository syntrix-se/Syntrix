// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.

namespace Syntrix.Ui
{
    /// <summary>
    /// Element properties of the UI service (API 2), grouped by value type: 1-19 and 200-219 Vector2,
    /// 20-39 and 220-239 float, 40-59 and 240-259 bool, 60-79 and 260-279 int, 80-99 and 280-299 Color,
    /// 100-119 and 300-319 string, 320-339 Vector3D. The numbers are part of the protocol: never change a value.
    /// A property an element does not have is ignored when set and reads as the default value.
    /// </summary>
    public enum UiProperty
    {
        // Vector2
        /// <summary>Offset from the aligned position.</summary>
        Offset = 1,
        /// <summary>Size of the element.</summary>
        Size = 2,
        /// <summary>Inner padding.</summary>
        Padding = 3,
        /// <summary>Window: smallest size when resized by the player.</summary>
        MinSize = 4,
        /// <summary>Padding around the text.</summary>
        TextPadding = 5,
        /// <summary>Full pixel size of the texture (needed for regions).</summary>
        TextureSize = 6,
        /// <summary>Top left pixel of the used region of the texture.</summary>
        TextureOffset = 7,
        /// <summary>Pixel size of the used region; zero = whole texture.</summary>
        TextureRegion = 8,
        /// <summary>Read only: centre of the element on the screen.</summary>
        Position = 9,
        /// <summary>Read only: size of the element's text.</summary>
        TextSize = 10,
        /// <summary>Chain and scroll box: largest member size.</summary>
        MemberMaxSize = 11,
        /// <summary>Chain and scroll box: smallest member size.</summary>
        MemberMinSize = 12,

        // float
        /// <summary>Width of the element.</summary>
        Width = 20,
        /// <summary>Height of the element.</summary>
        Height = 21,
        /// <summary>Chain and scroll box: space between members.</summary>
        Spacing = 22,
        /// <summary>Thickness of the border or line.</summary>
        BorderThickness = 23,
        /// <summary>Base font size of the text (1 = normal).</summary>
        FontSize = 24,
        /// <summary>Width at which wrapped text breaks.</summary>
        WrapWidth = 25,
        /// <summary>Label: wrap at the parent's width minus this margin; negative = off.</summary>
        WrapToParent = 26,
        /// <summary>Label: upper limit for WrapToParent; zero = none.</summary>
        WrapMax = 27,
        /// <summary>Slider value.</summary>
        Value = 28,
        /// <summary>Slider minimum.</summary>
        Min = 29,
        /// <summary>Slider maximum.</summary>
        Max = 30,
        /// <summary>Window: height of the header.</summary>
        HeaderHeight = 31,

        // bool
        /// <summary>Shown; in a scroll box hidden entries leave the list.</summary>
        Visible = 40,
        /// <summary>Size follows the text.</summary>
        AutoResize = 41,
        /// <summary>Wrap the text.</summary>
        Wrap = 42,
        /// <summary>Centre the text vertically.</summary>
        VertCenter = 43,
        /// <summary>Window: the player may move it.</summary>
        Draggable = 44,
        /// <summary>Window: the player may resize it.</summary>
        Resizable = 45,
        /// <summary>Window: hide the game HUD while open.</summary>
        HideHud = 46,
        /// <summary>Window: keep the view still while open (mouse look, also in seats).</summary>
        LockView = 47,
        /// <summary>Window: Escape closes it while focused.</summary>
        CloseOnEscape = 48,
        /// <summary>Scroll box: smooth scrolling.</summary>
        SmoothScrolling = 49,
        /// <summary>Scroll box: scrolling enabled.</summary>
        Scrolling = 50,
        /// <summary>Highlight under the cursor.</summary>
        HighlightEnabled = 51,
        /// <summary>Check box and on/off value.</summary>
        Checked = 52,
        /// <summary>Read only.</summary>
        MousedOver = 53,
        /// <summary>Text field typing; set true to start typing, false to stop.</summary>
        InputOpen = 54,
        /// <summary>The element takes mouse input.</summary>
        InputEnabled = 55,
        /// <summary>Clip children to this element's area.</summary>
        Masking = 56,
        /// <summary>Read only: window has the focus.</summary>
        Focused = 57,
        /// <summary>Window: show the cursor and keep mouse and camera from the game while open (default true).</summary>
        Modal = 58,

        // int
        /// <summary>Alignment to the parent (UiAlign flags).</summary>
        ParentAlign = 60,
        /// <summary>Size taken from the parent (UiDim).</summary>
        DimAlign = 61,
        /// <summary>Text alignment (left, centre, right).</summary>
        TextAlign = 62,
        /// <summary>Font style flags (bold, italic, underline).</summary>
        FontStyle = 63,
        /// <summary>Chain and scroll box sizing (UiSizing flags).</summary>
        Sizing = 64,
        /// <summary>Scroll box: first shown entry.</summary>
        Start = 65,
        /// <summary>Drawing order offset among siblings.</summary>
        ZOffset = 66,
        /// <summary>Text field: longest text; 0 = no limit.</summary>
        MaxLength = 67,
        /// <summary>Text field and text box: allowed characters (line, digits, number, any).</summary>
        CharFilter = 68,
        /// <summary>Read only: number of children.</summary>
        Count = 69,
        /// <summary>Texture alignment (stretch, fit vertical, fit horizontal, fit auto).</summary>
        TextureAlign = 70,
        /// <summary>Read only: last visible entry of a scroll box.</summary>
        End = 71,
        /// <summary>Read only: number of visible entries of a scroll box.</summary>
        EnabledCount = 72,

        // Color
        /// <summary>Main colour (background or tint).</summary>
        Color = 80,
        /// <summary>Colour under the cursor.</summary>
        HighlightColor = 81,
        /// <summary>Border colour.</summary>
        BorderColor = 82,
        /// <summary>Base text colour.</summary>
        TextColor = 83,
        /// <summary>Label: colour of links.</summary>
        LinkColor = 84,
        /// <summary>Colour while focused.</summary>
        FocusColor = 85,
        /// <summary>Scroll or slider bar colour.</summary>
        BarColor = 86,
        /// <summary>Bar colour under the cursor.</summary>
        BarHighlight = 87,
        /// <summary>Slider colour.</summary>
        SliderColor = 88,
        /// <summary>Slider colour under the cursor.</summary>
        SliderHighlight = 89,
        /// <summary>Scroll box: divider colour.</summary>
        DividerColor = 90,
        /// <summary>Window and tree: header colour.</summary>
        HeaderColor = 91,
        /// <summary>Window: body colour.</summary>
        BodyColor = 92,
        /// <summary>Check box: tick colour.</summary>
        TickColor = 93,

        // string
        /// <summary>Text with markup (see UiMarkup).</summary>
        Text = 100,
        /// <summary>Text without markup.</summary>
        PlainText = 101,
        /// <summary>Rich HUD font name.</summary>
        Font = 102,
        /// <summary>Transparent material subtype used as texture.</summary>
        Texture = 103,
        /// <summary>Text field: shown while empty and not typing (markup).</summary>
        Placeholder = 104,
        /// <summary>Tooltip text (markup).</summary>
        Tooltip = 105,
        /// <summary>Window: header text (markup).</summary>
        Title = 106,

        // ---------------------------------------------------------------- second ranges

        // Vector2
        /// <summary>Text box: start of the selection (X line, Y character); set SelectionEnd afterwards to apply.</summary>
        SelectionStart = 200,
        /// <summary>Text box: end of the selection (X line, Y character); setting it selects from SelectionStart.</summary>
        SelectionEnd = 201,
        /// <summary>Read only. Text box: caret (X line, Y character).</summary>
        CaretPosition = 202,
        /// <summary>Lists: padding of the highlight box.</summary>
        HighlightPadding = 203,
        /// <summary>Drop-down and lists: padding of the entries.</summary>
        MemberPadding = 204,
        /// <summary>Lists: padding of the entry list.</summary>
        ListPadding = 205,
        /// <summary>On/off button: padding around the two buttons.</summary>
        BackgroundPadding = 206,
        /// <summary>Slider bar: size of the bar.</summary>
        BarSize = 207,
        /// <summary>Slider bar: size of the slider.</summary>
        SliderSize = 208,
        /// <summary>Terminal drag box: value (screen position).</summary>
        ValueVector = 209,
        /// <summary>Terminal drag box: size of the dragged box.</summary>
        BoxSize = 210,
        /// <summary>Named on/off button: padding of the buttons.</summary>
        ButtonPadding = 211,

        // float
        /// <summary>Lists: height of one entry.</summary>
        LineHeight = 220,
        /// <summary>Drop-down and tree: height of the opened list.</summary>
        DropdownHeight = 221,
        /// <summary>Tree: indentation of the entries.</summary>
        IndentSize = 222,
        /// <summary>Tree: height of the header.</summary>
        LabelHeight = 223,
        /// <summary>On/off button: space between the buttons.</summary>
        ButtonSpacing = 224,
        /// <summary>Sliders and scroll bars: value as 0..1.</summary>
        Percent = 225,
        /// <summary>Scroll bar: visible part as 0..1 (slider length).</summary>
        VisiblePercent = 226,
        /// <summary>Radial selection: cursor sensitivity of the gesture input.</summary>
        CursorSensitivity = 227,
        /// <summary>Ring and radial selection: inner radius (ring: 0..1 of the size).</summary>
        InnerRadius = 228,
        /// <summary>Cam and scaled space: scale of the plane.</summary>
        PlaneScale = 229,
        /// <summary>Cam space: rotation angle in radians around RotationAxis.</summary>
        RotationAngle = 230,
        /// <summary>Lists: smallest length of the entry list.</summary>
        MinLength = 231,

        // bool
        /// <summary>Text box: the player may edit the text.</summary>
        EnableEditing = 240,
        /// <summary>Text box: the player may select text.</summary>
        EnableHighlighting = 241,
        /// <summary>Text box: caret to the end when it gets the focus.</summary>
        MoveToEndOnFocus = 242,
        /// <summary>Text box: clear the selection when the focus is lost.</summary>
        ClearSelectionOnBlur = 243,
        /// <summary>Drop-down and tree: list opened (set to open or close).</summary>
        Open = 244,
        /// <summary>Buttons and lists: use the focus colours while focused.</summary>
        UseFocusFormatting = 245,
        /// <summary>Scroll bar and slider bar: vertical.</summary>
        Vertical = 246,
        /// <summary>Slider bar: show the slider.</summary>
        SliderVisible = 247,
        /// <summary>Slider bar: reverse direction.</summary>
        Reverse = 248,
        /// <summary>Radial selection: select by mouse gesture.</summary>
        UseGestureInput = 249,
        /// <summary>Cam space: in screen space instead of world space.</summary>
        IsScreenSpace = 250,
        /// <summary>Cam space: scale with the screen resolution.</summary>
        UseResScaling = 251,
        /// <summary>Read only. HUD space: in front of the camera.</summary>
        IsInFront = 252,
        /// <summary>Read only. HUD space: facing the camera.</summary>
        IsFacingCamera = 253,
        /// <summary>Terminal pages, categories, tiles and controls: enabled (shown).</summary>
        Enabled = 254,
        /// <summary>Terminal drag box: snap to the screen edge.</summary>
        AlignToEdge = 255,
        /// <summary>Line: connect the last point with the first.</summary>
        ClosedLine = 256,

        // int
        /// <summary>Text box: character used for new lines (as number).</summary>
        NewLineChar = 260,
        /// <summary>Lists, trees, radial selection: selected index (-1 = none; set -1 to clear).</summary>
        SelectedIndex = 261,
        /// <summary>Drop-down and lists: smallest number of shown entries.</summary>
        MinVisibleCount = 262,
        /// <summary>Radial selection: number of slots.</summary>
        MaxEntryCount = 263,
        /// <summary>Radial selection: highlighted index (-1 = none).</summary>
        HighlightIndex = 264,
        /// <summary>Polygon and ring: number of sides.</summary>
        Sides = 265,
        /// <summary>Read only. Number of entries made with Ui.ItemInsert (or tree and radial children).</summary>
        ItemCount = 266,

        // Color
        /// <summary>Lists: colour of the selection tab.</summary>
        TabColor = 280,
        /// <summary>Buttons and lists: text colour while focused.</summary>
        FocusTextColor = 281,
        /// <summary>Colour pickers and terminal colour picker: the value.</summary>
        ValueColor = 282,
        /// <summary>On/off button: colour of the not selected side.</summary>
        UnselectedColor = 283,
        /// <summary>On/off button and radial selection: colour of the selected side or slot.</summary>
        SelectionColor = 284,
        /// <summary>Background of the tooltip of this element; transparent = Rich HUD default.</summary>
        TooltipColor = 285,
        /// <summary>Slider: bar colour while focused.</summary>
        BarFocusColor = 286,
        /// <summary>Slider: slider colour while focused.</summary>
        SliderFocusColor = 287,

        // string
        /// <summary>Lists and terminal lists: key of the selected entry (set to select by key).</summary>
        SelectedKey = 300,
        /// <summary>On/off buttons: text of the on side (markup).</summary>
        OnText = 301,
        /// <summary>On/off buttons: text of the off side (markup).</summary>
        OffText = 302,
        /// <summary>Double label box: right text (markup).</summary>
        RightText = 303,
        /// <summary>Named slider and terminal slider: value text.</summary>
        ValueText = 304,
        /// <summary>Terminal category and text page: sub-header.</summary>
        SubHeader = 305,
        /// <summary>Terminal text page: page text (markup); terminal text field: the value.</summary>
        BodyText = 306,

        // Vector3D
        /// <summary>HSV colour picker: hue (0..360), saturation and value (0..100).</summary>
        ColorHsv = 320,
        /// <summary>Cam space: rotation axis.</summary>
        RotationAxis = 321,
        /// <summary>Cam space: offset from the camera.</summary>
        TransformOffset = 322,
        /// <summary>Read only. HUD space: cursor position in the plane (Z = depth).</summary>
        SpaceCursor = 323,
    }
}
