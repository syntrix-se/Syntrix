// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.

namespace Syntrix.Gui
{
    /// <summary>Values of <see cref="GuiProp.Style"/>.</summary>
    public static class GuiStyleId
    {
        /// <summary>Default style: no panel; button: normal.</summary>
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
}
