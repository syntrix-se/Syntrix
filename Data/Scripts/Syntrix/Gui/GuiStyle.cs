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
        private const string C = "SyxGui_";

        /// <summary>Window size of Keen's terminal and inventory, normalised (GUI width, GUI height).</summary>
        public static readonly Vector2 WindowSize = new Vector2(1.0157f, 0.9172f);

        /// <summary>Normal text font.</summary>
        public const string Font = "Blue";
        /// <summary>Highlighted text font (headings, hover, bold).</summary>
        public const string FontBright = "White";
        /// <summary>Normal text scale.</summary>
        public const float TextScale = 0.8f;

        /// <summary>Normal text colour.</summary>
        public static readonly Color Text = new Color(255, 255, 255);
        /// <summary>Subdued text colour (secondary information).</summary>
        public static readonly Color TextQuiet = new Color(150, 165, 175);
        /// <summary>Text colour of disabled elements.</summary>
        public static readonly Color TextDisabled = new Color(110, 120, 128);
        /// <summary>Accent colour (progress bar, highlights).</summary>
        public static readonly Color Accent = new Color(90, 200, 220);
        /// <summary>Colour for important notes and badges.</summary>
        public static readonly Color Important = new Color(255, 205, 80);
        /// <summary>Colour for warnings and errors.</summary>
        public static readonly Color Danger = new Color(230, 80, 70);
        /// <summary>Colour for positive states.</summary>
        public static readonly Color Good = new Color(120, 210, 110);
        /// <summary>Link colour.</summary>
        public static readonly Color Link = new Color(120, 205, 235);
        /// <summary>Colour of thin lines and borders.</summary>
        public static readonly Color Line = new Color(77, 99, 113);
        /// <summary>Neutral background colour.</summary>
        public static readonly Color Background = new Color(73, 86, 94);
        /// <summary>Dark translucent fill colour.</summary>
        public static readonly Color Dark = new Color(20, 26, 31, 200);
        /// <summary>Fill colour of a hovered row.</summary>
        public static readonly Color HoverFill = new Color(70, 110, 140, 90);
        /// <summary>Fill colour of a selected row.</summary>
        public static readonly Color SelectedFill = new Color(90, 150, 185, 140);

        // Textures
        /// <summary>Texture of the default button.</summary>
        public const string Button = C + "button_default";
        /// <summary>Texture of the default button, hovered.</summary>
        public const string ButtonHover = C + "button_default_highlight";
        /// <summary>Texture of the default button, pressed.</summary>
        public const string ButtonPressed = C + "button_default_active";
        /// <summary>Texture of the outline-free button, focused.</summary>
        public const string ButtonFocus = C + "button_default_outlineless_focus";
        /// <summary>Texture of the outline-free button.</summary>
        public const string ButtonFlat = C + "button_default_outlineless";
        /// <summary>Texture of the red button.</summary>
        public const string ButtonRed = C + "button_red";
        /// <summary>Texture of the red button, hovered.</summary>
        public const string ButtonRedHover = C + "button_red_highlight";
        /// <summary>Texture of the window close symbol.</summary>
        public const string Close = C + "button_close_symbol";
        /// <summary>Texture of the window close symbol, hovered.</summary>
        public const string CloseHover = C + "button_close_symbol_highlight";
        /// <summary>Texture of a ticked check box.</summary>
        public const string CheckOn = C + "checkbox_checked";
        /// <summary>Texture of a ticked check box, hovered.</summary>
        public const string CheckOnHover = C + "checkbox_checked_highlight";
        /// <summary>Texture of an empty check box.</summary>
        public const string CheckOff = C + "checkbox_unchecked";
        /// <summary>Texture of an empty check box, hovered.</summary>
        public const string CheckOffHover = C + "checkbox_unchecked_highlight";
        /// <summary>Texture of the slider rail, left end.</summary>
        public const string RailLeft = C + "slider_rail_left";
        /// <summary>Texture of the slider rail, middle part.</summary>
        public const string RailCenter = C + "slider_rail_center";
        /// <summary>Texture of the slider rail, right end.</summary>
        public const string RailRight = C + "slider_rail_right";
        /// <summary>Texture of the slider rail, left end, hovered.</summary>
        public const string RailLeftHover = C + "slider_rail_left_highlight";
        /// <summary>Texture of the slider rail, middle part, hovered.</summary>
        public const string RailCenterHover = C + "slider_rail_center_highlight";
        /// <summary>Texture of the slider rail, right end, hovered.</summary>
        public const string RailRightHover = C + "slider_rail_right_highlight";
        /// <summary>Texture of the slider thumb.</summary>
        public const string Thumb = C + "slider_thumb";
        /// <summary>Texture of the slider thumb, hovered.</summary>
        public const string ThumbHover = C + "slider_thumb_highlight";
        /// <summary>Texture of the scroll bar thumb, top end.</summary>
        public const string ScrollTop = C + "scrollbar_v_thumb_top";
        /// <summary>Texture of the scroll bar thumb, middle part.</summary>
        public const string ScrollCenter = C + "scrollbar_v_thumb_center";
        /// <summary>Texture of the scroll bar thumb, bottom end.</summary>
        public const string ScrollBottom = C + "scrollbar_v_thumb_bottom";
        /// <summary>Texture of the scroll bar thumb, top end, hovered.</summary>
        public const string ScrollTopHover = C + "scrollbar_v_thumb_top_highlight";
        /// <summary>Texture of the scroll bar thumb, middle part, hovered.</summary>
        public const string ScrollCenterHover = C + "scrollbar_v_thumb_center_highlight";
        /// <summary>Texture of the scroll bar thumb, bottom end, hovered.</summary>
        public const string ScrollBottomHover = C + "scrollbar_v_thumb_bottom_highlight";
        /// <summary>Texture of the text box, left end.</summary>
        public const string BoxLeft = C + "textbox_left";
        /// <summary>Texture of the text box, middle part.</summary>
        public const string BoxCenter = C + "textbox_center";
        /// <summary>Texture of the text box, right end.</summary>
        public const string BoxRight = C + "textbox_right";
        /// <summary>Texture of the text box, left end, hovered.</summary>
        public const string BoxLeftHover = C + "textbox_left_highlight";
        /// <summary>Texture of the text box, middle part, hovered.</summary>
        public const string BoxCenterHover = C + "textbox_center_highlight";
        /// <summary>Texture of the text box, right end, hovered.</summary>
        public const string BoxRightHover = C + "textbox_right_highlight";
        /// <summary>Texture of the text box, left end, focused.</summary>
        public const string BoxLeftFocus = C + "textbox_left_focus";
        /// <summary>Texture of the text box, middle part, focused.</summary>
        public const string BoxCenterFocus = C + "textbox_center_focus";
        /// <summary>Texture of the text box, right end, focused.</summary>
        public const string BoxRightFocus = C + "textbox_right_focus";
        /// <summary>Texture of the drop-down, left end.</summary>
        public const string ComboLeft = C + "combobox_default_left";
        /// <summary>Texture of the drop-down, middle part.</summary>
        public const string ComboCenter = C + "combobox_default_center";
        /// <summary>Texture of the drop-down, right end.</summary>
        public const string ComboRight = C + "combobox_default_right";
        /// <summary>Texture of the drop-down, left end, hovered.</summary>
        public const string ComboLeftHover = C + "combobox_default_highlight_left";
        /// <summary>Texture of the drop-down, middle part, hovered.</summary>
        public const string ComboCenterHover = C + "combobox_default_highlight_center";
        /// <summary>Texture of the drop-down, right end, hovered.</summary>
        public const string ComboRightHover = C + "combobox_default_highlight_right";
        /// <summary>Texture of a hovered list row.</summary>
        public const string ItemHover = C + "item_highlight_dark";
        /// <summary>Texture of a selected list row.</summary>
        public const string ItemSelected = C + "item_active_dark";
        /// <summary>Texture of a tab.</summary>
        public const string Tab = C + "ToolBarTab";
        /// <summary>Texture of a tab, hovered.</summary>
        public const string TabHover = C + "ToolBarTab_highlight";
        /// <summary>Texture of the right arrow button.</summary>
        public const string ArrowRight = C + "button_arrow_right";
        /// <summary>Texture of the right arrow button, hovered.</summary>
        public const string ArrowRightHover = C + "button_arrow_right_highlight";
        /// <summary>Texture of the left arrow button.</summary>
        public const string ArrowLeft = C + "button_arrow_left";
        /// <summary>Texture of the left arrow button, hovered.</summary>
        public const string ArrowLeftHover = C + "button_arrow_left_highlight";
        /// <summary>Texture of the tooltip background.</summary>
        public const string Tooltip = "SyxGui_TooltipBackground";
        /// <summary>Texture of the progress bar fill.</summary>
        public const string Progress = "SyxGui_ProgressBar";

        // Nine-part areas: prefix, centre
        /// <summary>Name prefix of the nine-part dark area.</summary>
        public const string DarkPrefix = C + "rectangle_dark";
        /// <summary>Centre texture of the dark area.</summary>
        public const string DarkCenter = C + "rectangle_dark_center";
        /// <summary>Name prefix of the nine-part border.</summary>
        public const string BorderPrefix = C + "rectangle_dark_border";
        /// <summary>Name prefix of the nine-part list background.</summary>
        public const string ListPrefix = C + "scrollable_list";
        /// <summary>Centre texture of the list background.</summary>
        public const string ListCenter = C + "scrollable_list_center";
        /// <summary>Name prefix of the nine-part neutral area.</summary>
        public const string NeutralPrefix = C + "rectangle_neutral";
        /// <summary>Centre texture of the neutral area.</summary>
        public const string NeutralCenter = C + "rectangle_neutral_center";

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
