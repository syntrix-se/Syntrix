// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.

namespace Syntrix.Ui
{
    /// <summary>
    /// Events of the UI service (API 2). The numbers are part of the protocol: never change a value.
    /// Element events are only sent after Ui.Listen; service events (handle 0) always.
    /// </summary>
    public enum UiEvent
    {
        /// <summary>Left click on a clickable element; terminal button pressed.</summary>
        Click = 1,

        /// <summary>Right click.</summary>
        RightClick = 2,

        /// <summary>Cursor moved onto the element.</summary>
        CursorEnter = 3,

        /// <summary>Cursor left the element.</summary>
        CursorExit = 4,

        /// <summary>Text of a text field or text box changed; argument: the text.</summary>
        TextChanged = 5,

        /// <summary>Enter pressed in a text field; argument: the text.</summary>
        Submit = 6,

        /// <summary>
        /// Value changed. Argument: "1"/"0" (check boxes, on/off), the selected key (lists), the handle of the
        /// selected child (tree box), "#RRGGBBAA" (colour pickers), the text (terminal text
        /// field), null otherwise (read the value).
        /// </summary>
        ValueChanged = 7,

        /// <summary>Link in a label clicked; argument: the link target.</summary>
        Link = 8,

        /// <summary>Size of a window body or a panel changed.</summary>
        Resized = 9,

        /// <summary>Window was hidden (close key, Escape or Visible = false).</summary>
        Closed = 10,

        /// <summary>Header of a tree list or tree box was clicked (opened or closed; read Open).</summary>
        Toggled = 11,

        /// <summary>Key bind of the mod pressed (handle 0); argument: bind name.</summary>
        BindPressed = 20,

        /// <summary>UI is ready (handle 0). Build windows now; sent again after a Rich HUD restart.</summary>
        Ready = 21,

        /// <summary>All elements of all mods are gone (handle 0), e.g. Rich HUD restarted.</summary>
        Lost = 22,

        /// <summary>UI will not work in this session (handle 0): dedicated server or Rich HUD Master missing.</summary>
        Unavailable = 23,
    }
}
