// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using VRageMath;

namespace Syntrix.Ui
{
    /// <summary>Label of the UI service: wraps at the parent's width on request and reports link clicks.</summary>
    public sealed class LabelElement : Label
    {
        private readonly UiNode _node;

        /// <summary>Margin for wrapping at the parent's width; negative = off.</summary>
        public float WrapToParent = -1f;

        /// <summary>Upper limit for the wrap width from WrapToParent; zero = none.</summary>
        public float WrapMax;

        /// <summary>Makes the label of a node.</summary>
        public LabelElement(UiNode node) : base(null)
        {
            _node = node;
        }

        /// <summary>Follows the parent width when wrapping to the parent.</summary>
        protected override void Layout()
        {
            base.Layout();
            if (WrapToParent < 0f)
                return;
            var parent = Parent as HudElementBase;
            if (parent == null)
                return;
            float w = parent.Width - parent.Padding.X - WrapToParent;
            if (WrapMax > 0f)
                w = Math.Min(w, WrapMax);
            w = Math.Max(20f, w);
            if (Math.Abs(LineWrapWidth - w) > 1f)
                LineWrapWidth = w;
        }

        /// <summary>Left click on a link run: the character under the cursor tells the target by its colour.</summary>
        protected override void HandleInput(Vector2 cursorPos)
        {
            var links = _node.Links;
            if (links == null || links.Count == 0 || !IsMousedOver || !SharedBinds.LeftButton.IsNewPressed)
                return;
            var board = TextBoard;
            var pos = cursorPos - Position;
            var idx = board.GetCharAtOffset(pos);
            if (idx.X < 0 || idx.Y < 0 || idx.X >= board.Count || idx.Y >= board[idx.X].Count)
                return;
            IRichChar ch = board[idx.X][idx.Y];
            // Only when the cursor is really on the character, not right of the line end
            if (Math.Abs(pos.X - ch.Offset.X) > ch.Size.X * 0.5f + 4f || Math.Abs(pos.Y - ch.Offset.Y) > ch.Size.Y * 0.5f + 4f)
                return;
            string target;
            if (links.TryGetValue(ch.Format.Color.PackedValue, out target))
                _node.Fire(UiEvent.Link, target);
        }
    }
}
