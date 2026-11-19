// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Globalization;
using VRageMath;

namespace Syntrix.Gui
{
    /// <summary>
    /// Drawing area: the owner draws into it from a callback (screen pixels rect, alpha), clipped to the area.
    /// Mouse events go to the owner as GuiEvent.Mouse with coordinates relative to the area in reference pixels.
    /// </summary>
    public class GuiCanvas : GuiElement
    {
        /// <summary>Draw callback: (x, y, width, height in screen pixels), alpha.</summary>
        public Action<Vector4, float> DrawCallback;
        /// <summary>Also report mouse moves without a pressed button (GuiEvent.Mouse, index 2).</summary>
        public bool ReportMoves;

        /// <summary>Creates a canvas that clips its drawing to its area.</summary>
        public GuiCanvas() : base(GuiKind.Canvas)
        {
            Clip = true;
        }

        public override bool Interactive => true;

        public override Vector2 Measure(float width, float height)
        {
            return new Vector2(width, Math.Max(100f * K, height));
        }

        protected override void DrawContent()
        {
            if (DrawCallback == null)
                return;
            using (GuiDraw.PushClip(Rect))
            {
                try
                {
                    DrawCallback(new Vector4(Rect.X, Rect.Y, Rect.Width, Rect.Height), GuiDraw.Alpha);
                }
                catch (Exception e)
                {
                    Log.Error("canvas draw callback of " + (Owner != null ? Owner.ModName : "Syntrix"), e);
                    DrawCallback = null;
                }
            }
        }

        private string Local(Vector2 p, string extra)
        {
            var l = (p - new Vector2(Rect.X, Rect.Y)) / K;
            return l.X.ToString("0.#", CultureInfo.InvariantCulture) + ";" + l.Y.ToString("0.#", CultureInfo.InvariantCulture) + ";" + extra;
        }

        public override bool MouseDown(Vector2 p, int button)
        {
            Fire(GuiEvent.Mouse, 0, Local(p, button.ToString(CultureInfo.InvariantCulture)));
            return true;
        }

        public override void MouseDrag(Vector2 p)
        {
            if (ReportMoves)
                Fire(GuiEvent.Mouse, 2, Local(p, "0"));
        }

        public override void MouseMove(Vector2 p)
        {
            if (ReportMoves)
                Fire(GuiEvent.Mouse, 2, Local(p, "-1"));
        }

        public override void MouseUp(Vector2 p, int button, bool inside)
        {
            Fire(GuiEvent.Mouse, 1, Local(p, button.ToString(CultureInfo.InvariantCulture)));
            if (inside)
                Fire(GuiEvent.Click, button);
        }

        public override bool Wheel(Vector2 p, int delta)
        {
            Fire(GuiEvent.Mouse, 3, Local(p, delta.ToString(CultureInfo.InvariantCulture)));
            return true;
        }

        public override bool SetBool(int prop, bool v)
        {
            if (prop == GuiProp.MouseMove) { ReportMoves = v; return true; }
            return base.SetBool(prop, v);
        }
    }
}
