// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.

namespace Syntrix.Gui
{
    /// <summary>Events (API 3). Fixed numbers, also in Api/SyntrixGui.cs.</summary>
    public static class GuiEvent
    {
        /// <summary>Index = mouse button (0 left, 1 right).</summary>
        public const int Click = 1;
        /// <summary>Value, text, state or selection changed by the player; text = new text.</summary>
        public const int Changed = 2;
        /// <summary>Enter in a text field; text = text.</summary>
        public const int Submit = 3;
        /// <summary>Index = item, text = item key.</summary>
        public const int Select = 4;
        /// <summary>Index = item, text = "1" expanded or "0" collapsed.</summary>
        public const int Expand = 5;
        /// <summary>Text = link target.</summary>
        public const int Link = 6;
        /// <summary>Window opened.</summary>
        public const int Opened = 7;
        /// <summary>Window closed; index 1 = while unloading.</summary>
        public const int Closed = 8;
        /// <summary>Esc on a window that does not close on Esc.</summary>
        public const int Cancel = 9;
        /// <summary>Index = item (lists) or -1.</summary>
        public const int DoubleClick = 10;
        /// <summary>Screen resolution or UI scale changed.</summary>
        public const int Resized = 11;
        /// <summary>Canvas: index 0 down, 1 up, 2 move, 3 wheel; text "x;y;button|delta" (reference px).</summary>
        public const int Mouse = 12;
        /// <summary>Text field lost the keyboard.</summary>
        public const int FocusLost = 13;
    }
}
