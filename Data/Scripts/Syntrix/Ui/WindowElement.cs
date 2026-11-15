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

        public bool CloseOnEscape = true;
        public bool HideHud;
        public bool LockView = true;
        public bool Modal = true;

        /// <summary>Last known visibility, for the Closed event.</summary>
        public bool WasVisible;

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

        protected override void HandleInput(Vector2 cursorPos)
        {
            base.HandleInput(cursorPos);
            if (CloseOnEscape && Visible && WindowActive && SharedBinds.Escape.IsNewPressed && !UiService.TypedRecently)
                Visible = false;
        }
    }
}
