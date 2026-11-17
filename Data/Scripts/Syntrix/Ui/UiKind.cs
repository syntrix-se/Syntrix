// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.

namespace Syntrix.Ui
{
    /// <summary>
    /// Element kinds of the UI service (API 2). The numbers are part of the protocol: never change a value.
    /// 1-49 are HUD elements, 60-79 are pages and controls of the Rich HUD terminal (F2).
    /// </summary>
    public enum UiKind
    {
        /// <summary>Invisible element without content: anchor, spacer or group.</summary>
        Empty = 1,

        /// <summary>Coloured or textured rectangle (TexturedBox).</summary>
        Box = 2,

        /// <summary>Frame line around the element's area (BorderBox).</summary>
        Border = 3,

        /// <summary>Text with markup and clickable links.</summary>
        Label = 4,

        /// <summary>Text on a coloured background (LabelBox).</summary>
        LabelBox = 5,

        /// <summary>Background, optional border and optional mouse input with highlight colour.</summary>
        Panel = 6,

        /// <summary>Text button with highlight (LabelBoxButton) and optional border.</summary>
        Button = 7,

        /// <summary>One-line text entry.</summary>
        TextField = 8,

        /// <summary>Vertical scrolling list; children become its entries.</summary>
        ScrollBox = 9,

        /// <summary>Horizontal scrolling list.</summary>
        ScrollBoxHorizontal = 10,

        /// <summary>Vertical stack; children become its members.</summary>
        Chain = 11,

        /// <summary>Horizontal stack.</summary>
        ChainHorizontal = 12,

        /// <summary>Invisible mouse input area.</summary>
        ClickArea = 13,

        /// <summary>Window with header and body, draggable and resizable.</summary>
        Window = 14,

        /// <summary>Check box with a value.</summary>
        CheckBox = 15,

        /// <summary>Slider with minimum, maximum and value.</summary>
        Slider = 16,

        /// <summary>Fixed part of another element (window header, body, border); made by Ui.Part.</summary>
        Part = 17,

        /// <summary>Multi-line text editor with caret, selection and clipboard (Rich HUD TextBox).</summary>
        TextBox = 18,

        /// <summary>Drop-down list; entries through the Ui.Item endpoints.</summary>
        Dropdown = 19,

        /// <summary>List box with selection; entries through the Ui.Item endpoints.</summary>
        ListBox = 20,

        /// <summary>Collapsible list with a header; entries through the Ui.Item endpoints.</summary>
        TreeList = 21,

        /// <summary>Collapsible tree node; label children (also other tree nodes) become its entries.</summary>
        TreeBox = 22,

        /// <summary>Colour picker with red, green and blue sliders.</summary>
        ColorPickerRgb = 23,

        /// <summary>Colour picker with hue, saturation and value sliders.</summary>
        ColorPickerHsv = 24,

        /// <summary>Pair of on and off buttons.</summary>
        OnOffButton = 25,

        /// <summary>Check box with a name.</summary>
        NamedCheckBox = 26,

        /// <summary>On/off buttons with a name.</summary>
        NamedOnOffButton = 27,

        /// <summary>Slider with a name and a value text.</summary>
        NamedSlider = 28,

        /// <summary>Scroll bar (value with visible part).</summary>
        ScrollBar = 29,

        /// <summary>Bare slider bar with free bar and slider sizes.</summary>
        SliderBar = 30,

        /// <summary>Textured button (icon button).</summary>
        IconButton = 31,

        /// <summary>Clickable text without background.</summary>
        LabelButton = 32,

        /// <summary>Text button with border and focus colours.</summary>
        BorderedButton = 33,

        /// <summary>Label box with a left and a right text.</summary>
        DoubleLabelBox = 34,

        /// <summary>Radial selection wheel; children become its entries.</summary>
        RadialSelection = 35,

        /// <summary>HUD space in front of the camera: offset, rotation and scale (Rich HUD CamSpaceNode).</summary>
        CamSpace = 36,

        /// <summary>HUD space at a world matrix set by the mod (Ui.SetMatrix): billboards and panels in the world.</summary>
        WorldSpace = 37,

        /// <summary>HUD space scaled relative to its parent (Rich HUD ScaledSpaceNode).</summary>
        ScaledSpace = 38,

        /// <summary>Line strip through points (Ui.SetPoints) with thickness, colour and texture.</summary>
        Line = 39,

        /// <summary>Regular polygon filling the element (circle with many sides).</summary>
        Polygon = 40,

        /// <summary>Ring: polygon with a hole.</summary>
        Ring = 41,

        /// <summary>Triangle mesh from points and indices (Ui.SetPoints).</summary>
        Mesh = 42,

        /// <summary>Terminal: group of pages in the mod's entry of the Rich HUD terminal.</summary>
        TerminalCategory = 60,

        /// <summary>Terminal: page with control categories.</summary>
        ControlPage = 61,

        /// <summary>Terminal: page with header, sub-header and long text.</summary>
        TextPage = 62,

        /// <summary>Terminal: category (header and sub-header) on a control page.</summary>
        ControlCategory = 64,

        /// <summary>Terminal: tile of up to three controls in a category.</summary>
        ControlTile = 65,

        /// <summary>Terminal control: button.</summary>
        TerminalButton = 66,

        /// <summary>Terminal control: check box.</summary>
        TerminalCheckBox = 67,

        /// <summary>Terminal control: colour picker.</summary>
        TerminalColorPicker = 68,

        /// <summary>Terminal control: drag box that places a point on the screen.</summary>
        TerminalDragBox = 69,

        /// <summary>Terminal control: drop-down list (entries through Ui.Item).</summary>
        TerminalDropdown = 70,

        /// <summary>Terminal control: text line.</summary>
        TerminalLabel = 71,

        /// <summary>Terminal control: list (entries through Ui.Item).</summary>
        TerminalList = 72,

        /// <summary>Terminal control: on/off buttons.</summary>
        TerminalOnOffButton = 73,

        /// <summary>Terminal control: slider with value text.</summary>
        TerminalSlider = 74,

        /// <summary>Terminal control: text field.</summary>
        TerminalTextField = 75,
    }
}
