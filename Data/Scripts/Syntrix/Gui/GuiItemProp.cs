// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.

namespace Syntrix.Gui
{
    /// <summary>Item properties for Gui.ItemSet.</summary>
    public static class GuiItemProp
    {
        /// <summary>Item text (markup).</summary>
        public const int Text = 1;
        /// <summary>Materials separated by ';'.</summary>
        public const int Icons = 2;
        /// <summary>"1" / "0".</summary>
        public const int Enabled = 3;
        /// <summary>"1" / "0" (tree).</summary>
        public const int Expanded = 4;
        /// <summary>Second text (list), right aligned or below.</summary>
        public const int Extra = 5;
        /// <summary>"#RRGGBB" or "#RRGGBBAA", "" = default.</summary>
        public const int Color = 6;
        /// <summary>Tooltip text of the item.</summary>
        public const int Tooltip = 7;
        /// <summary>Small tag behind the text, e.g. "New".</summary>
        public const int Badge = 8;
    }
}
