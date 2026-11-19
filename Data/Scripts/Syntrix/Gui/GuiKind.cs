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
}
