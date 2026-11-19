// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using VRageMath;

namespace Syntrix.Gui
{
    /// <summary>
    /// Vanilla look: textures and colours as in Keen's terminal and inventory (MyGuiConstants), font "Blue" for normal
    /// text and "White" for highlighted text, text scale 0.8. Windows get the size of the terminal (1.0157 x 0.9172).
    /// </summary>
    public static class GuiStyle
    {
        private const string C = "Textures\\GUI\\Controls\\";

        /// <summary>Window size of Keen's terminal and inventory, normalised (GUI width, GUI height).</summary>
        public static readonly Vector2 WindowSize = new Vector2(1.0157f, 0.9172f);

        /// <summary>Normal text font.</summary>
        public const string Font = "Blue";
        /// <summary>Highlighted text font (headings, hover, bold).</summary>
        public const string FontBright = "White";
        /// <summary>Normal text scale.</summary>
        public const float TextScale = 0.8f;

        public static readonly Color Text = new Color(255, 255, 255);
        public static readonly Color TextQuiet = new Color(150, 165, 175);
        public static readonly Color TextDisabled = new Color(110, 120, 128);
        public static readonly Color Accent = new Color(90, 200, 220);
        public static readonly Color Important = new Color(255, 205, 80);
        public static readonly Color Danger = new Color(230, 80, 70);
        public static readonly Color Good = new Color(120, 210, 110);
        public static readonly Color Link = new Color(120, 205, 235);
        public static readonly Color Line = new Color(77, 99, 113);
        public static readonly Color Background = new Color(73, 86, 94);
        public static readonly Color Dark = new Color(20, 26, 31, 200);
        public static readonly Color HoverFill = new Color(70, 110, 140, 90);
        public static readonly Color SelectedFill = new Color(90, 150, 185, 140);

        // Textures
        public const string Button = C + "button_default.dds";
        public const string ButtonHover = C + "button_default_highlight.dds";
        public const string ButtonPressed = C + "button_default_active.dds";
        public const string ButtonFocus = C + "button_default_outlineless_focus.dds";
        public const string ButtonFlat = C + "button_default_outlineless.dds";
        public const string ButtonRed = C + "button_red.dds";
        public const string ButtonRedHover = C + "button_red_highlight.dds";
        public const string Close = C + "button_close_symbol.dds";
        public const string CloseHover = C + "button_close_symbol_highlight.dds";
        public const string CheckOn = C + "checkbox_checked.dds";
        public const string CheckOnHover = C + "checkbox_checked_highlight.dds";
        public const string CheckOff = C + "checkbox_unchecked.dds";
        public const string CheckOffHover = C + "checkbox_unchecked_highlight.dds";
        public const string RailLeft = C + "slider_rail_left.dds";
        public const string RailCenter = C + "slider_rail_center.dds";
        public const string RailRight = C + "slider_rail_right.dds";
        public const string RailLeftHover = C + "slider_rail_left_highlight.dds";
        public const string RailCenterHover = C + "slider_rail_center_highlight.dds";
        public const string RailRightHover = C + "slider_rail_right_highlight.dds";
        public const string Thumb = C + "slider_thumb.dds";
        public const string ThumbHover = C + "slider_thumb_highlight.dds";
        public const string ScrollTop = C + "scrollbar_v_thumb_top.dds";
        public const string ScrollCenter = C + "scrollbar_v_thumb_center.dds";
        public const string ScrollBottom = C + "scrollbar_v_thumb_bottom.dds";
        public const string ScrollTopHover = C + "scrollbar_v_thumb_top_highlight.dds";
        public const string ScrollCenterHover = C + "scrollbar_v_thumb_center_highlight.dds";
        public const string ScrollBottomHover = C + "scrollbar_v_thumb_bottom_highlight.dds";
        public const string BoxLeft = C + "textbox_left.dds";
        public const string BoxCenter = C + "textbox_center.dds";
        public const string BoxRight = C + "textbox_right.dds";
        public const string BoxLeftHover = C + "textbox_left_highlight.dds";
        public const string BoxCenterHover = C + "textbox_center_highlight.dds";
        public const string BoxRightHover = C + "textbox_right_highlight.dds";
        public const string BoxLeftFocus = C + "textbox_left_focus.dds";
        public const string BoxCenterFocus = C + "textbox_center_focus.dds";
        public const string BoxRightFocus = C + "textbox_right_focus.dds";
        public const string ComboLeft = C + "combobox_default_left.dds";
        public const string ComboCenter = C + "combobox_default_center.dds";
        public const string ComboRight = C + "combobox_default_right.dds";
        public const string ComboLeftHover = C + "combobox_default_highlight_left.dds";
        public const string ComboCenterHover = C + "combobox_default_highlight_center.dds";
        public const string ComboRightHover = C + "combobox_default_highlight_right.dds";
        public const string ItemHover = C + "item_highlight_dark.dds";
        public const string ItemSelected = C + "item_active_dark.dds";
        public const string Tab = C + "ToolBarTab.dds";
        public const string TabHover = C + "ToolBarTab_highlight.dds";
        public const string ArrowRight = C + "button_arrow_right.dds";
        public const string ArrowRightHover = C + "button_arrow_right_highlight.dds";
        public const string ArrowLeft = C + "button_arrow_left.dds";
        public const string ArrowLeftHover = C + "button_arrow_left_highlight.dds";
        public const string Tooltip = "Textures\\GUI\\TooltipBackground.dds";
        public const string Progress = "Textures\\GUI\\ProgressBar.dds";

        // Nine-part areas: prefix, centre
        public const string DarkPrefix = C + "rectangle_dark";
        public const string DarkCenter = C + "rectangle_dark_center.dds";
        public const string BorderPrefix = C + "rectangle_dark_border";
        public const string ListPrefix = C + "scrollable_list";
        public const string ListCenter = C + "scrollable_list_center.dds";
        public const string NeutralPrefix = C + "rectangle_neutral";
        public const string NeutralCenter = C + "rectangle_neutral_center.dds";

        /// <summary>A few colour names for markup ([color=accent]).</summary>
        public static bool Named(string name, out Color c)
        {
            switch ((name ?? "").ToLowerInvariant())
            {
                case "white": c = Color.White; return true;
                case "text": c = Text; return true;
                case "quiet": case "grey": case "gray": c = TextQuiet; return true;
                case "accent": c = Accent; return true;
                case "important": case "yellow": c = Important; return true;
                case "danger": case "red": c = Danger; return true;
                case "good": case "green": c = Good; return true;
                case "link": c = Link; return true;
                default: c = Color.White; return false;
            }
        }

        /// <summary>Draws a panel background of the given style (GuiStyleId).</summary>
        public static void Panel(int style, RectangleF r, Color tint)
        {
            switch (style)
            {
                case GuiStyleId.Dark:
                    GuiDraw.Nine(DarkPrefix, DarkCenter, r, tint, 4f);
                    break;
                case GuiStyleId.Border:
                    GuiDraw.Nine(DarkPrefix, DarkCenter, r, tint, 4f);
                    GuiDraw.Nine(BorderPrefix, null, r, tint, 4f);
                    break;
                case GuiStyleId.List:
                    GuiDraw.Nine(ListPrefix, ListCenter, r, tint, 4f);
                    break;
            }
        }
    }
}
