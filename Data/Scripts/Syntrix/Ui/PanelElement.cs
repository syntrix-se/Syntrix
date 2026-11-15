// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using RichHudFramework.UI;
using VRageMath;

namespace Syntrix.Ui
{
    /// <summary>
    /// General purpose surface of the UI service: background, optional border, optional mouse input with a
    /// highlight colour, and a Resized event. Rows, cards, tabs and custom buttons are built from it.
    /// </summary>
    public sealed class PanelElement : HudElementBase
    {
        private readonly UiNode _node;
        private Color _color, _highlight;
        private Vector2 _lastSize;
        private bool _lit;

        public readonly TexturedBox Background;
        public readonly MouseInputElement Input;
        private BorderBox _border;

        public PanelElement(UiNode node) : base(null)
        {
            _node = node;
            Background = new TexturedBox(this) { DimAlignment = DimAlignments.Size, Color = Color.Transparent };
            // Created first so that it lies below everything the mod adds; off until a mouse event is wanted.
            Input = new MouseInputElement(this) { DimAlignment = DimAlignments.Size, Visible = false };
            Input.LeftClicked += (o, e) => _node.Fire(UiEvent.Click, null);
            Input.RightClicked += (o, e) => _node.Fire(UiEvent.RightClick, null);
            Input.CursorEntered += (o, e) => _node.Fire(UiEvent.CursorEnter, null);
            Input.CursorExited += (o, e) => _node.Fire(UiEvent.CursorExit, null);
            Size = new Vector2(100f, 40f);
        }

        public Color Color
        {
            get { return _color; }
            set { _color = value; if (!_lit) Background.Color = value; }
        }

        /// <summary>Background colour under the cursor; transparent = no highlight.</summary>
        public Color HighlightColor
        {
            get { return _highlight; }
            set { _highlight = value; if (value.A > 0) Input.Visible = true; }
        }

        /// <summary>Frame line, created on first use.</summary>
        public BorderBox Border
        {
            get
            {
                if (_border == null)
                    _border = new BorderBox(this) { DimAlignment = DimAlignments.Size, Thickness = 1f, Color = Color.White };
                return _border;
            }
        }

        public bool HasBorder { get { return _border != null; } }

        protected override void Layout()
        {
            base.Layout();
            bool lit = _highlight.A > 0 && Input.Visible && Input.IsMousedOver;
            if (lit != _lit)
            {
                _lit = lit;
                Background.Color = lit ? _highlight : _color;
            }
            var size = Size;
            if (Vector2.DistanceSquared(size, _lastSize) > 0.25f)
            {
                _lastSize = size;
                _node.Fire(UiEvent.Resized, null);
            }
        }
    }
}
