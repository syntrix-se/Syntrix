// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRageMath;

namespace Syntrix.Gui
{
    /// <summary>
    /// Layer on the screen without a window: no mouse, children placed by anchor on the whole screen.
    /// Hidden with the HUD and while a menu or window shows the mouse cursor, unless ShowInMenus.
    /// </summary>
    public class GuiOverlay : GuiElement
    {
        /// <summary>Also shown while a menu or window shows the mouse cursor.</summary>
        public bool ShowInMenus;
        /// <summary>Layer: 0 below the HUD, 1 above the HUD, 2 above everything.</summary>
        public int Layer;

        /// <summary>Creates an overlay.</summary>
        public GuiOverlay() : base(GuiKind.Overlay)
        {
        }

        /// <summary>Lays out the overlay over the whole screen and draws it; an error hides it.</summary>
        public void DrawOverlay()
        {
            var screen = GuiService.ScreenPixels;
            var rect = new RectangleF(0f, 0f, screen.X, screen.Y);
            GuiDraw.Layer = Layer == 1 ? MyScreenSpaceLayer.AboveHud : Layer == 2 ? MyScreenSpaceLayer.AboveEverything : MyScreenSpaceLayer.BelowHud;
            GuiDraw.Alpha = 1f;
            GuiDraw.Clip = new RectangleF();
            try
            {
                Arrange(rect, rect);
                DrawTree();
            }
            catch (Exception e)
            {
                Log.Error("overlay draw of " + (Owner != null ? Owner.ModName : "Syntrix"), e);
                Visible = false;
            }
            finally
            {
                GuiDraw.Layer = MyScreenSpaceLayer.BelowHud;
            }
        }

        public override bool SetBool(int prop, bool v)
        {
            if (prop == GuiProp.ShowInMenus) { ShowInMenus = v; return true; }
            return base.SetBool(prop, v);
        }

        public override bool GetBool(int prop, out bool v)
        {
            if (prop == GuiProp.ShowInMenus) { v = ShowInMenus; return true; }
            return base.GetBool(prop, out v);
        }

        public override bool SetInt(int prop, int v)
        {
            if (prop == GuiProp.Layer) { Layer = MathHelper.Clamp(v, 0, 2); return true; }
            return base.SetInt(prop, v);
        }

        public override bool GetInt(int prop, out int v)
        {
            if (prop == GuiProp.Layer) { v = Layer; return true; }
            return base.GetInt(prop, out v);
        }
    }
}
