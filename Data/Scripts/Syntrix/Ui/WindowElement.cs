// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using RichHudFramework.UI;
using VRageMath;

namespace Syntrix.Ui
{
    /// <summary>Window of the UI service: Rich HUD window with Resized event and closing by Escape.</summary>
    public sealed class WindowElement : WindowBase
    {
        private readonly UiNode _node;
        private Vector2 _lastBody;

        /// <summary>Escape closes the focused window.</summary>
        public bool CloseOnEscape = true;
        /// <summary>Hide the game HUD while open.</summary>
        public bool HideHud;
        /// <summary>Keep the view still while open.</summary>
        public bool LockView = true;
        /// <summary>Cursor on, mouse and camera kept from the game while open.</summary>
        public bool Modal = true;

        /// <summary>Last known visibility, for the Closed event.</summary>
        public bool WasVisible;

        /// <summary>Makes the window of a node.</summary>
        public WindowElement(UiNode node) : base(null)
        {
            _node = node;
            HeaderText = "";
            MouseInput.RequestCursor = true;
            Size = new Vector2(800f, 500f);
            MinimumSize = new Vector2(300f, 200f);
            AllowResizing = true;
            CanDrag = true;
            Visible = false;
        }

        /// <summary>Raises Resized when the body size changed.</summary>
        protected override void Layout()
        {
            base.Layout();
            var size = body.Size;
            if (Vector2.DistanceSquared(size, _lastBody) > 0.25f)
            {
                _lastBody = size;
                _node.Fire(UiEvent.Resized, null);
            }
        }

        /// <summary>Escape hides the focused window.</summary>
        protected override void HandleInput(Vector2 cursorPos)
        {
            base.HandleInput(cursorPos);
            if (CloseOnEscape && Visible && WindowActive && SharedBinds.Escape.IsNewPressed && !UiService.TypedRecently)
                Visible = false;
        }
    }
}
