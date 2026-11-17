// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using RichHudFramework.UI;
using RichHudFramework.UI.Client;
using VRageMath;

namespace Syntrix.Ui
{
    /// <summary>
    /// Terminal part of <see cref="UiNode"/>: pages, categories, tiles and controls of the Rich HUD terminal (F2 menu).
    /// Terminal nodes have no HUD element (Element is null). Rich HUD cannot remove terminal members, so removing
    /// one disables it.
    /// </summary>
    public sealed partial class UiNode
    {
        /// <summary>The Rich HUD terminal object (page category, page, control category, tile or control); null for HUD elements.</summary>
        public object Term;
        /// <summary>Page category.</summary>
        public TerminalPageCategory TermCategory;
        /// <summary>Any page.</summary>
        public TerminalPageBase TermPage;
        /// <summary>Control page.</summary>
        public ControlPage TermControlPage;
        /// <summary>Text page.</summary>
        public TextPage TermTextPage;
        /// <summary>Control category of a control page.</summary>
        public ControlCategory TermControlCategory;
        /// <summary>Tile of a control category.</summary>
        public ControlTile TermTile;
        /// <summary>Any control.</summary>
        public TerminalControlBase TermControl;
        /// <summary>Check box.</summary>
        public TerminalCheckbox TermCheck;
        /// <summary>On/off button.</summary>
        public TerminalOnOffButton TermOnOff;
        /// <summary>Colour picker.</summary>
        public TerminalColorPicker TermColor;
        /// <summary>Drag box.</summary>
        public TerminalDragBox TermDrag;
        /// <summary>Drop-down.</summary>
        public TerminalDropdown<string> TermDrop;
        /// <summary>List.</summary>
        public TerminalList<string> TermList;
        /// <summary>Slider.</summary>
        public TerminalSlider TermSlider;
        /// <summary>Text field.</summary>
        public TerminalTextField TermField;

        private string _termTooltip;

        private static object MakeTerminal(UiKind kind)
        {
            switch (kind)
            {
                case UiKind.TerminalCategory: return new TerminalPageCategory();
                case UiKind.ControlPage: return new ControlPage();
                case UiKind.TextPage: return new TextPage();
                case UiKind.ControlCategory: return new ControlCategory();
                case UiKind.ControlTile: return new ControlTile();
                case UiKind.TerminalButton: return new TerminalButton();
                case UiKind.TerminalCheckBox: return new TerminalCheckbox();
                case UiKind.TerminalColorPicker: return new TerminalColorPicker();
                case UiKind.TerminalDragBox: return new TerminalDragBox();
                case UiKind.TerminalDropdown: return new TerminalDropdown<string>();
                case UiKind.TerminalLabel: return new TerminalLabel();
                case UiKind.TerminalList: return new TerminalList<string>();
                case UiKind.TerminalOnOffButton: return new TerminalOnOffButton();
                case UiKind.TerminalSlider: return new TerminalSlider();
                case UiKind.TerminalTextField: return new TerminalTextField();
            }
            return null;
        }

        /// <summary>Makes the terminal object of this node's kind and wires its events. Called once.</summary>
        public void InitTerminal()
        {
            Term = MakeTerminal(Kind);
            TermCategory = Term as TerminalPageCategory;
            TermPage = Term as TerminalPageBase;
            TermControlPage = Term as ControlPage;
            TermTextPage = Term as TextPage;
            TermControlCategory = Term as ControlCategory;
            TermTile = Term as ControlTile;
            TermControl = Term as TerminalControlBase;
            TermCheck = Term as TerminalCheckbox;
            TermOnOff = Term as TerminalOnOffButton;
            TermColor = Term as TerminalColorPicker;
            TermDrag = Term as TerminalDragBox;
            TermDrop = Term as TerminalDropdown<string>;
            TermList = Term as TerminalList<string>;
            TermSlider = Term as TerminalSlider;
            TermField = Term as TerminalTextField;

            if (TermControl != null)
                TermControl.ControlChanged += (o, a) => TermChanged();
        }

        private void TermChanged()
        {
            if (Kind == UiKind.TerminalButton)
            {
                Fire(UiEvent.Click, null);
                return;
            }
            string arg = null;
            if (TermCheck != null) arg = TermCheck.Value ? "1" : "0";
            else if (TermOnOff != null) arg = TermOnOff.Value ? "1" : "0";
            else if (TermColor != null) arg = Hex(TermColor.Value);
            else if (TermDrop != null || TermList != null) arg = UiItemList.SelectedKey(this);
            else if (TermField != null) arg = TermField.Value;
            Fire(UiEvent.ValueChanged, arg);
        }

        /// <summary>True when this node may be placed under the parent (null = top level).</summary>
        /// <param name="kind">Kind of the child.</param>
        /// <param name="parent">Parent node or null.</param>
        public static bool CanAttach(UiKind kind, UiNode parent)
        {
            bool term = IsTerminalKind(kind);
            if (parent == null)
                return !term || kind == UiKind.TerminalCategory || kind == UiKind.ControlPage || kind == UiKind.TextPage;
            if (parent.Term == null)
                return !term;
            if (!term)
                return false;
            switch (kind)
            {
                case UiKind.ControlPage:
                case UiKind.TextPage:
                    return parent.TermCategory != null;
                case UiKind.ControlCategory: return parent.TermControlPage != null;
                case UiKind.ControlTile: return parent.TermControlCategory != null;
                case UiKind.TerminalCategory: return false;
            }
            return parent.TermTile != null;
        }

        /// <summary>Adds the terminal object to the parent's terminal object (or the mod root of the terminal).</summary>
        /// <param name="parent">Parent node or null.</param>
        public void AttachTerminal(UiNode parent)
        {
            if (parent == null)
            {
                RichHudTerminal.Root.Enabled = true;
                if (TermCategory != null) RichHudTerminal.Root.Add(TermCategory);
                else if (TermPage != null) RichHudTerminal.Root.Add(TermPage);
            }
            else if (TermPage != null) parent.TermCategory.Add(TermPage);
            else if (TermControlCategory != null) parent.TermControlPage.Add(TermControlCategory);
            else if (TermTile != null) parent.TermControlCategory.Add(TermTile);
            else if (TermControl != null) parent.TermTile.Add(TermControl);
        }

        /// <summary>Rich HUD cannot remove terminal members: disables the object instead.</summary>
        public void DetachTerminal() { TermSetBool((int)UiProperty.Enabled, false); }

        /// <summary>Opens the Rich HUD terminal on this page.</summary>
        public void OpenTerminalPage()
        {
            if (TermPage != null)
                RichHudTerminal.OpenToPage(TermPage);
        }

        // ---------------------------------------------------------------- property dispatch

        private void TermSetVector(int p, Vector2 v)
        {
            if (TermDrag == null)
                return;
            if (p == (int)UiProperty.ValueVector) TermDrag.Value = v;
            else if (p == (int)UiProperty.BoxSize) TermDrag.BoxSize = v;
        }

        private Vector2 TermGetVector(int p)
        {
            if (TermDrag == null)
                return Vector2.Zero;
            if (p == (int)UiProperty.ValueVector) return TermDrag.Value;
            if (p == (int)UiProperty.BoxSize) return TermDrag.BoxSize;
            return Vector2.Zero;
        }

        private void TermSetFloat(int p, float v)
        {
            if (TermSlider == null)
                return;
            switch ((UiProperty)p)
            {
                case UiProperty.Value: TermSlider.Value = v; break;
                case UiProperty.Min: TermSlider.Min = v; break;
                case UiProperty.Max: TermSlider.Max = v; break;
                case UiProperty.Percent: TermSlider.Percent = v; break;
            }
        }

        private float TermGetFloat(int p)
        {
            if (TermSlider == null)
                return 0f;
            switch ((UiProperty)p)
            {
                case UiProperty.Value: return TermSlider.Value;
                case UiProperty.Min: return TermSlider.Min;
                case UiProperty.Max: return TermSlider.Max;
                case UiProperty.Percent: return TermSlider.Percent;
            }
            return 0f;
        }

        private void TermSetBool(int p, bool v)
        {
            switch ((UiProperty)p)
            {
                case UiProperty.Visible:
                case UiProperty.Enabled:
                    if (TermCategory != null) TermCategory.Enabled = v;
                    else if (TermPage != null) TermPage.Enabled = v;
                    else if (TermControlCategory != null) TermControlCategory.Enabled = v;
                    else if (TermTile != null) TermTile.Enabled = v;
                    else if (TermControl != null) TermControl.Enabled = v;
                    break;
                case UiProperty.Checked:
                    if (TermCheck != null) TermCheck.Value = v;
                    else if (TermOnOff != null) TermOnOff.Value = v;
                    break;
                case UiProperty.AlignToEdge: if (TermDrag != null) TermDrag.AlignToEdge = v; break;
            }
        }

        private bool TermGetBool(int p)
        {
            switch ((UiProperty)p)
            {
                case UiProperty.Visible:
                case UiProperty.Enabled:
                    if (TermCategory != null) return TermCategory.Enabled;
                    if (TermPage != null) return TermPage.Enabled;
                    if (TermControlCategory != null) return TermControlCategory.Enabled;
                    if (TermTile != null) return TermTile.Enabled;
                    return TermControl != null && TermControl.Enabled;
                case UiProperty.Checked: return TermCheck != null ? TermCheck.Value : TermOnOff != null && TermOnOff.Value;
                case UiProperty.AlignToEdge: return TermDrag != null && TermDrag.AlignToEdge;
            }
            return false;
        }

        private void TermSetInt(int p, int v)
        {
            switch ((UiProperty)p)
            {
                case UiProperty.SelectedIndex: UiItemList.SetSelectedIndex(this, v); break;
                case UiProperty.CharFilter: if (TermField != null) TermField.CharFilterFunc = Filter(v); break;
            }
        }

        private int TermGetInt(int p)
        {
            switch ((UiProperty)p)
            {
                case UiProperty.SelectedIndex: return UiItemList.SelectedIndex(this);
                case UiProperty.ItemCount: return UiItemList.Count(this);
                case UiProperty.Count: return Children != null ? Children.Count : 0;
            }
            return 0;
        }

        private void TermSetColor(int p, Color v)
        {
            if (TermColor != null && (p == (int)UiProperty.ValueColor || p == (int)UiProperty.Color))
                TermColor.Value = v;
        }

        private Color TermGetColor(int p)
        {
            if (TermColor != null && (p == (int)UiProperty.ValueColor || p == (int)UiProperty.Color))
                return TermColor.Value;
            return Color.Transparent;
        }

        private void TermSetText(int p, string v)
        {
            switch ((UiProperty)p)
            {
                case UiProperty.Text:
                case UiProperty.Title:
                case UiProperty.PlainText:
                    _markup = v;
                    if (TermCategory != null) TermCategory.Name = v;
                    else if (TermTextPage != null) { TermTextPage.Name = v; TermTextPage.HeaderText = Rich(v); }
                    else if (TermPage != null) TermPage.Name = v;
                    else if (TermControlCategory != null) TermControlCategory.HeaderText = v;
                    else if (TermControl != null) TermControl.Name = v;
                    break;
                case UiProperty.SubHeader:
                    _placeholder = v;
                    if (TermControlCategory != null) TermControlCategory.SubheaderText = v;
                    else if (TermTextPage != null) TermTextPage.SubHeaderText = Rich(v);
                    break;
                case UiProperty.BodyText:
                    if (TermTextPage != null) TermTextPage.Text = Rich(v);
                    else if (TermField != null) TermField.Value = v ?? "";
                    break;
                case UiProperty.ValueText: if (TermSlider != null) TermSlider.ValueText = v ?? ""; break;
                case UiProperty.SelectedKey: UiItemList.SelectKey(this, v); break;
                case UiProperty.Tooltip:
                    _termTooltip = v;
                    if (TermControl != null)
                        TermControl.ToolTip = string.IsNullOrEmpty(v) ? null : new ToolTip { text = Rich(v) };
                    break;
            }
        }

        private string TermGetText(int p)
        {
            switch ((UiProperty)p)
            {
                case UiProperty.Text:
                case UiProperty.Title:
                case UiProperty.PlainText:
                    return _markup;
                case UiProperty.SubHeader: return _placeholder;
                case UiProperty.BodyText:
                    if (TermTextPage != null) return TermTextPage.Text.ToString();
                    return TermField != null ? TermField.Value : null;
                case UiProperty.ValueText: return TermSlider != null ? TermSlider.ValueText : null;
                case UiProperty.SelectedKey: return UiItemList.SelectedKey(this);
                case UiProperty.Tooltip: return _termTooltip;
            }
            return null;
        }
    }
}
