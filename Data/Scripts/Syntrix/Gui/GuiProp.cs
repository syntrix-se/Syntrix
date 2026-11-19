// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.

namespace Syntrix.Gui
{
    /// <summary>Properties (API 3). Fixed numbers, also in Api/SyntrixGui.cs.</summary>
    public static class GuiProp
    {
        /// <summary>Bool.</summary>
        public const int Visible = 1;
        /// <summary>Bool.</summary>
        public const int Enabled = 2;
        /// <summary>Vector, reference pixels.</summary>
        public const int Offset = 3;
        /// <summary>Vector, reference pixels; 0 = automatic.</summary>
        public const int Size = 4;
        /// <summary>Int 0..8 (top left .. bottom right).</summary>
        public const int Anchor = 5;
        /// <summary>Bool.</summary>
        public const int FillWidth = 6;
        /// <summary>Bool.</summary>
        public const int FillHeight = 7;
        /// <summary>Vector (left/right, top/bottom).</summary>
        public const int Padding = 8;
        /// <summary>Colour: background.</summary>
        public const int Color = 9;
        /// <summary>Colour.</summary>
        public const int BorderColor = 10;
        /// <summary>Float.</summary>
        public const int BorderWidth = 11;
        /// <summary>Text (markup).</summary>
        public const int Tooltip = 12;
        /// <summary>Text.</summary>
        public const int Text = 13;
        /// <summary>Colour.</summary>
        public const int TextColor = 14;
        /// <summary>Float, 0.8 = Vanilla.</summary>
        public const int TextScale = 15;
        /// <summary>Text, font name.</summary>
        public const int Font = 16;
        /// <summary>Int 0 left, 1 centre, 2 right.</summary>
        public const int Align = 17;
        /// <summary>Bool.</summary>
        public const int Wrap = 18;
        /// <summary>Bool.</summary>
        public const int Markup = 19;
        /// <summary>Float.</summary>
        public const int Value = 20;
        /// <summary>Float.</summary>
        public const int Min = 21;
        /// <summary>Float.</summary>
        public const int Max = 22;
        /// <summary>Float.</summary>
        public const int Step = 23;
        /// <summary>Bool.</summary>
        public const int Checked = 24;
        /// <summary>Int, item index.</summary>
        public const int Selected = 25;
        /// <summary>Text.</summary>
        public const int Material = 26;
        /// <summary>Float.</summary>
        public const int Spacing = 27;
        /// <summary>Bool (stack).</summary>
        public const int Horizontal = 28;
        /// <summary>Float.</summary>
        public const int ScrollPosition = 29;
        /// <summary>Int.</summary>
        public const int Columns = 30;
        /// <summary>Float.</summary>
        public const int RowHeight = 31;
        /// <summary>Text (window).</summary>
        public const int Title = 32;
        /// <summary>Bool (window, before Open).</summary>
        public const int Pause = 33;
        /// <summary>Bool (window).</summary>
        public const int EscapeCloses = 34;
        /// <summary>Bool (window).</summary>
        public const int CloseButton = 35;
        /// <summary>Bool (window).</summary>
        public const int BackgroundFade = 36;
        /// <summary>Int (text field).</summary>
        public const int MaxLength = 37;
        /// <summary>Text (text field).</summary>
        public const int Placeholder = 38;
        /// <summary>Bool.</summary>
        public const int Focused = 39;
        /// <summary>Int, see GuiStyleId.</summary>
        public const int Style = 40;
        /// <summary>Vector.</summary>
        public const int IconSize = 41;
        /// <summary>Text (table: relative widths "2|1|3").</summary>
        public const int Widths = 42;
        /// <summary>Text (table: header cells separated by \u001f).</summary>
        public const int Header = 43;
        /// <summary>Float, read only.</summary>
        public const int ScrollMax = 44;
        /// <summary>Vector, read only (reference pixels).</summary>
        public const int ContentSize = 45;
        /// <summary>Vector, read only (reference pixels, top left).</summary>
        public const int ScreenPosition = 46;
        /// <summary>Vector, read only (reference pixels).</summary>
        public const int ScreenSize = 47;
        /// <summary>Bool, read only (window).</summary>
        public const int IsOpen = 48;
        /// <summary>Vector (window: centre, normalised 0..1).</summary>
        public const int Position = 49;
        /// <summary>Bool.</summary>
        public const int Clip = 50;
        /// <summary>Bool, read only.</summary>
        public const int Hover = 51;
        /// <summary>Text (slider value format, e.g. "0.0").</summary>
        public const int Format = 52;
        /// <summary>Bool (canvas: report mouse moves).</summary>
        public const int MouseMove = 53;
        /// <summary>Text.</summary>
        public const int SelectedKey = 54;
        /// <summary>Int 0 below HUD, 1 above HUD, 2 above everything (overlay).</summary>
        public const int Layer = 55;
        /// <summary>Vector (window, normalised; 0 = Vanilla terminal size).</summary>
        public const int WindowSize = 56;
        /// <summary>Bool (text field).</summary>
        public const int Multiline = 57;
        /// <summary>Bool (overlay: stay visible while a screen is open).</summary>
        public const int ShowInMenus = 58;
        /// <summary>Float, read only: height of the laid out text.</summary>
        public const int TextHeight = 59;
        /// <summary>Bool (scroll: keep at the end when content grows).</summary>
        public const int ScrollToEnd = 60;
    }
}
