// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.

namespace Syntrix.Ui
{
    /// <summary>
    /// Element kinds of the UI service (API 2). The numbers are part of the protocol: never change a value.
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
    }
}
