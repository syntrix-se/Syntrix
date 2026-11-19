// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.

namespace Syntrix.Gui
{
    /// <summary>Element kinds of the window service (API 3). Fixed numbers, also in Api/SyntrixGui.cs; never change one.</summary>
    public static class GuiKind
    {
        /// <summary>Window in the game's screen stack (MyModScreen), with mouse and Esc.</summary>
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
        /// <summary>Image or icon (material, texture path or definition id).</summary>
        public const int Image = 8;
        /// <summary>Text field with keyboard input.</summary>
        public const int TextField = 9;
        /// <summary>List of items (one or more columns), with icons and selection.</summary>
        public const int List = 10;
        /// <summary>Tree of items that can be expanded.</summary>
        public const int Tree = 11;
        /// <summary>Table with header and rows; cells with wrapping and markup.</summary>
        public const int Table = 12;
        /// <summary>Check box with text.</summary>
        public const int CheckBox = 13;
        /// <summary>Slider.</summary>
        public const int Slider = 14;
        /// <summary>Progress bar.</summary>
        public const int ProgressBar = 15;
        /// <summary>Row of tabs; items are the tabs.</summary>
        public const int Tabs = 16;
        /// <summary>Drawing area: the mod draws into it from a callback (clipped by Syntrix).</summary>
        public const int Canvas = 17;
        /// <summary>Drop-down selection; items are the choices.</summary>
        public const int Dropdown = 18;

        /// <summary>Highest kind number of this build.</summary>
        public const int Last = 18;
    }

    /// <summary>Properties (API 3). Fixed numbers, also in Api/SyntrixGui.cs.</summary>
    public static class GuiProp
    {
        public const int Visible = 1;        // bool
        public const int Enabled = 2;        // bool
        public const int Offset = 3;         // vector, reference pixels
        public const int Size = 4;           // vector, reference pixels; 0 = automatic
        public const int Anchor = 5;         // int 0..8 (top left .. bottom right)
        public const int FillWidth = 6;      // bool
        public const int FillHeight = 7;     // bool
        public const int Padding = 8;        // vector (left/right, top/bottom)
        public const int Color = 9;          // colour: background
        public const int BorderColor = 10;   // colour
        public const int BorderWidth = 11;   // float
        public const int Tooltip = 12;       // text (markup)
        public const int Text = 13;          // text
        public const int TextColor = 14;     // colour
        public const int TextScale = 15;     // float, 0.8 = Vanilla
        public const int Font = 16;          // text, font name
        public const int Align = 17;         // int 0 left, 1 centre, 2 right
        public const int Wrap = 18;          // bool
        public const int Markup = 19;        // bool
        public const int Value = 20;         // float
        public const int Min = 21;           // float
        public const int Max = 22;           // float
        public const int Step = 23;          // float
        public const int Checked = 24;       // bool
        public const int Selected = 25;      // int, item index
        public const int Material = 26;      // text
        public const int Spacing = 27;       // float
        public const int Horizontal = 28;    // bool (stack)
        public const int ScrollPosition = 29;// float
        public const int Columns = 30;       // int
        public const int RowHeight = 31;     // float
        public const int Title = 32;         // text (window)
        public const int Pause = 33;         // bool (window, before Open)
        public const int EscapeCloses = 34;  // bool (window)
        public const int CloseButton = 35;   // bool (window)
        public const int BackgroundFade = 36;// bool (window)
        public const int MaxLength = 37;     // int (text field)
        public const int Placeholder = 38;   // text (text field)
        public const int Focused = 39;       // bool
        public const int Style = 40;         // int, see GuiStyleId
        public const int IconSize = 41;      // vector
        public const int Widths = 42;        // text (table: relative widths "2|1|3")
        public const int Header = 43;        // text (table: header cells separated by \u001f)
        public const int ScrollMax = 44;     // float, read only
        public const int ContentSize = 45;   // vector, read only (reference pixels)
        public const int ScreenPosition = 46;// vector, read only (reference pixels, top left)
        public const int ScreenSize = 47;    // vector, read only (reference pixels)
        public const int IsOpen = 48;        // bool, read only (window)
        public const int Position = 49;      // vector (window: centre, normalised 0..1)
        public const int Clip = 50;          // bool
        public const int Hover = 51;         // bool, read only
        public const int Format = 52;        // text (slider value format, e.g. "0.0")
        public const int MouseMove = 53;     // bool (canvas: report mouse moves)
        public const int SelectedKey = 54;   // text
        public const int Layer = 55;         // int 0 below HUD, 1 above HUD, 2 above everything (overlay)
        public const int WindowSize = 56;    // vector (window, normalised; 0 = Vanilla terminal size)
        public const int Multiline = 57;     // bool (text field)
        public const int ShowInMenus = 58;   // bool (overlay: stay visible while a screen is open)
        public const int TextHeight = 59;    // float, read only: height of the laid out text
        public const int ScrollToEnd = 60;   // bool (scroll: keep at the end when content grows)
    }

    /// <summary>Events (API 3). Fixed numbers, also in Api/SyntrixGui.cs.</summary>
    public static class GuiEvent
    {
        public const int Click = 1;          // index = mouse button (0 left, 1 right)
        public const int Changed = 2;        // value, text, state or selection changed by the player; text = new text
        public const int Submit = 3;         // Enter in a text field; text = text
        public const int Select = 4;         // index = item, text = item key
        public const int Expand = 5;         // index = item, text = "1" expanded or "0" collapsed
        public const int Link = 6;           // text = link target
        public const int Opened = 7;         // window opened
        public const int Closed = 8;         // window closed; index 1 = while unloading
        public const int Cancel = 9;         // Esc on a window that does not close on Esc
        public const int DoubleClick = 10;   // index = item (lists) or -1
        public const int Resized = 11;       // screen resolution or UI scale changed
        public const int Mouse = 12;         // canvas: index 0 down, 1 up, 2 move, 3 wheel; text "x;y;button|delta" (reference px)
        public const int FocusLost = 13;     // text field lost the keyboard
    }

    /// <summary>Values of <see cref="GuiProp.Style"/>.</summary>
    public static class GuiStyleId
    {
        public const int Default = 0;
        /// <summary>Panel: Vanilla dark area; button: red (danger).</summary>
        public const int Dark = 1;
        /// <summary>Panel: Vanilla dark area with border; button: without outline.</summary>
        public const int Border = 2;
        /// <summary>Panel: list background; button: flat text button (link look).</summary>
        public const int List = 3;
        /// <summary>Panel: no background at all.</summary>
        public const int None = 4;
        /// <summary>Button: toggle (stays pressed while Checked).</summary>
        public const int Toggle = 5;
    }

    /// <summary>Item properties for Gui.ItemSet.</summary>
    public static class GuiItemProp
    {
        public const int Text = 1;
        public const int Icons = 2;          // materials separated by ';'
        public const int Enabled = 3;        // "1" / "0"
        public const int Expanded = 4;       // "1" / "0" (tree)
        public const int Extra = 5;          // second text (list), right aligned or below
        public const int Color = 6;          // "#RRGGBB" or "#RRGGBBAA", "" = default
        public const int Tooltip = 7;
        public const int Badge = 8;          // small tag behind the text, e.g. "New"
    }
}
