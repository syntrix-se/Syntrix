// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.

namespace Syntrix.Ui
{
    /// <summary>
    /// Element properties of the UI service (API 2), grouped by value type: 1-19 Vector2, 20-39 float,
    /// 40-59 bool, 60-79 int, 80-99 Color, 100-119 string. The numbers are part of the protocol: never change a value.
    /// A property an element does not have is ignored when set and reads as the default value.
    /// </summary>
    public enum UiProperty
    {
        // Vector2
        Offset = 1,
        Size = 2,
        Padding = 3,
        /// <summary>Window: smallest size when resized by the player.</summary>
        MinSize = 4,
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
        MemberMaxSize = 11,
        MemberMinSize = 12,

        // float
        Width = 20,
        Height = 21,
        Spacing = 22,
        BorderThickness = 23,
        FontSize = 24,
        WrapWidth = 25,
        /// <summary>Label: wrap at the parent's width minus this margin; negative = off.</summary>
        WrapToParent = 26,
        /// <summary>Label: upper limit for WrapToParent; zero = none.</summary>
        WrapMax = 27,
        Value = 28,
        Min = 29,
        Max = 30,
        HeaderHeight = 31,

        // bool
        Visible = 40,
        AutoResize = 41,
        Wrap = 42,
        VertCenter = 43,
        Draggable = 44,
        Resizable = 45,
        /// <summary>Window: hide the game HUD while open.</summary>
        HideHud = 46,
        /// <summary>Window: keep the view still while open (mouse look, also in seats).</summary>
        LockView = 47,
        CloseOnEscape = 48,
        SmoothScrolling = 49,
        Scrolling = 50,
        HighlightEnabled = 51,
        Checked = 52,
        /// <summary>Read only.</summary>
        MousedOver = 53,
        /// <summary>Text field typing; set true to start typing, false to stop.</summary>
        InputOpen = 54,
        InputEnabled = 55,
        /// <summary>Clip children to this element's area.</summary>
        Masking = 56,
        /// <summary>Read only: window has the focus.</summary>
        Focused = 57,
        /// <summary>Window: show the cursor and keep mouse and camera from the game while open (default true).</summary>
        Modal = 58,

        // int
        ParentAlign = 60,
        DimAlign = 61,
        TextAlign = 62,
        FontStyle = 63,
        Sizing = 64,
        Start = 65,
        ZOffset = 66,
        MaxLength = 67,
        CharFilter = 68,
        /// <summary>Read only: number of children.</summary>
        Count = 69,
        TextureAlign = 70,
        /// <summary>Read only: last visible entry of a scroll box.</summary>
        End = 71,
        /// <summary>Read only: number of visible entries of a scroll box.</summary>
        EnabledCount = 72,

        // Color
        Color = 80,
        HighlightColor = 81,
        BorderColor = 82,
        TextColor = 83,
        LinkColor = 84,
        FocusColor = 85,
        BarColor = 86,
        BarHighlight = 87,
        SliderColor = 88,
        SliderHighlight = 89,
        DividerColor = 90,
        HeaderColor = 91,
        BodyColor = 92,
        TickColor = 93,

        // string
        /// <summary>Text with markup (see UiMarkup).</summary>
        Text = 100,
        /// <summary>Text without markup.</summary>
        PlainText = 101,
        Font = 102,
        /// <summary>Transparent material subtype used as texture.</summary>
        Texture = 103,
        /// <summary>Text field: shown while empty and not typing (markup).</summary>
        Placeholder = 104,
        Tooltip = 105,
        /// <summary>Window: header text (markup).</summary>
        Title = 106,
    }
}
