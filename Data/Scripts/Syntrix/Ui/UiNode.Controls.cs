// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using VRageMath;

namespace Syntrix.Ui
{
    /// <summary>
    /// Controls part of <see cref="UiNode"/>: the Rich HUD controls beyond the basic elements (text box, lists,
    /// trees, colour pickers, on/off buttons, named controls, bars, radial selection, HUD spaces and shapes).
    /// Each Set/Get hook returns true when it handled the property, otherwise the basic dispatch runs.
    /// </summary>
    public sealed partial class UiNode
    {
        // Typed views of Element for the controls (null where the element is no such type)

        /// <summary>Multi-line text editor.</summary>
        public TextBox Editor;
        /// <summary>Drop-down list.</summary>
        public Dropdown<string> Drop;
        /// <summary>List box.</summary>
        public ListBox<string> Items;
        /// <summary>Collapsible list.</summary>
        public TreeList<string> TreeL;
        /// <summary>Collapsible tree node with element entries.</summary>
        public TreeBox<string> Tree;
        /// <summary>RGB (and HSV) colour picker.</summary>
        public ColorPickerRGB Picker;
        /// <summary>HSV colour picker.</summary>
        public ColorPickerHSV PickerHsv;
        /// <summary>On/off buttons.</summary>
        public OnOffButton OnOff;
        /// <summary>Check box with name.</summary>
        public NamedCheckBox NamedCheck;
        /// <summary>On/off buttons with name.</summary>
        public NamedOnOffButton NamedOnOff;
        /// <summary>Slider with name.</summary>
        public NamedSliderBox NamedSlider;
        /// <summary>Scroll bar.</summary>
        public ScrollBar Bar;
        /// <summary>Bare slider bar.</summary>
        public SliderBar SlideBar;
        /// <summary>Textured button.</summary>
        public RichHudFramework.UI.Button Icon;
        /// <summary>Text button with border.</summary>
        public BorderedButton Bordered;
        /// <summary>Label box with two texts.</summary>
        public DoubleLabelBox Double;
        /// <summary>Radial selection.</summary>
        public RadialSelectionBox Radial;
        /// <summary>Custom shape.</summary>
        public ShapeElement Shape;
        /// <summary>Rich HUD label that is no LabelElement (text box, label button).</summary>
        public RichHudFramework.UI.Label AnyLabel;
        /// <summary>HUD space node holding Element (cam, world and scaled spaces), registered in the parent instead of Element.</summary>
        public HudSpaceNodeBase Outer;
        /// <summary>Entry of the parent's tree box.</summary>
        public bool InTree;
        /// <summary>Entry of the parent's radial selection.</summary>
        public bool InRadial;

        private CamSpaceNode _cam;
        private ScaledSpaceNode _scaled;
        private MatrixD _matrix = MatrixD.Identity;
        private Action<RichText> _textSink;
        private Vector2 _selectionStart;
        private string _tooltip;
        private Color _tooltipColor;

        /// <summary>The node registered in the parent: the space node, or Element.</summary>
        public HudNodeBase RootNode { get { return Outer != null ? (HudNodeBase)Outer : Element; } }

        /// <summary>True for kinds a mod may create.</summary>
        /// <param name="kind">UiKind value.</param>
        public static bool IsCreatable(int kind)
        {
            return (kind >= (int)UiKind.Empty && kind <= (int)UiKind.Slider)
                || (kind >= (int)UiKind.TextBox && kind <= (int)UiKind.Mesh)
                || (kind >= (int)UiKind.TerminalCategory && kind <= (int)UiKind.TextPage)
                || (kind >= (int)UiKind.ControlCategory && kind <= (int)UiKind.TerminalTextField);
        }

        /// <summary>True for kinds of the Rich HUD terminal.</summary>
        /// <param name="kind">UiKind value.</param>
        public static bool IsTerminalKind(UiKind kind) { return (int)kind >= (int)UiKind.TerminalCategory; }

        private static HudElementBase MakeControl(UiNode n)
        {
            switch (n.Kind)
            {
                case UiKind.TextBox: return new TextBox { Size = new Vector2(300f, 120f), BuilderMode = TextBuilderModes.Wrapped, AutoResize = false };
                case UiKind.Dropdown: return new Dropdown<string> { Size = new Vector2(220f, 32f) };
                case UiKind.ListBox: return new ListBox<string> { Size = new Vector2(220f, 200f) };
                case UiKind.TreeList: return new TreeList<string> { Size = new Vector2(220f, 32f) };
                case UiKind.TreeBox: return new TreeBox<string> { Size = new Vector2(220f, 32f) };
                case UiKind.ColorPickerRgb: return new ColorPickerRGB();
                case UiKind.ColorPickerHsv: return new ColorPickerHSV();
                case UiKind.OnOffButton: return new OnOffButton();
                case UiKind.NamedCheckBox: return new NamedCheckBox();
                case UiKind.NamedOnOffButton: return new NamedOnOffButton();
                case UiKind.NamedSlider: return new NamedSliderBox();
                case UiKind.ScrollBar: return new ScrollBar();
                case UiKind.SliderBar: return new SliderBar { Size = new Vector2(200f, 20f) };
                case UiKind.IconButton: return new RichHudFramework.UI.Button { Size = new Vector2(32f, 32f), Color = Color.White };
                case UiKind.LabelButton: return new LabelButton();
                case UiKind.BorderedButton: return new BorderedButton();
                case UiKind.DoubleLabelBox: return new DoubleLabelBox();
                case UiKind.RadialSelection: return new RadialSelectionBox { Size = new Vector2(400f, 400f) };
                case UiKind.CamSpace:
                    n._cam = new CamSpaceNode();
                    return n.Space(n._cam);
                case UiKind.WorldSpace:
                    return n.Space(new CustomSpaceNode { UpdateMatrixFunc = () => n._matrix });
                case UiKind.ScaledSpace:
                    n._scaled = new ScaledSpaceNode();
                    return n.Space(n._scaled);
                case UiKind.Line:
                case UiKind.Polygon:
                case UiKind.Ring:
                case UiKind.Mesh:
                    return new ShapeElement(n.Kind);
            }
            return null;
        }

        private HudElementBase Space(HudSpaceNodeBase node)
        {
            Outer = node;
            return new EmptyHudElement(node);
        }

        private void InitControls(HudElementBase e)
        {
            Editor = e as TextBox;
            Drop = e as Dropdown<string>;
            Items = e as ListBox<string>;
            TreeL = e as TreeList<string>;
            Tree = e as TreeBox<string>;
            Picker = e as ColorPickerRGB;
            PickerHsv = e as ColorPickerHSV;
            OnOff = e as OnOffButton;
            NamedCheck = e as NamedCheckBox;
            NamedOnOff = e as NamedOnOffButton;
            NamedSlider = e as NamedSliderBox;
            Bar = e as ScrollBar;
            SlideBar = e as SliderBar;
            Icon = e as RichHudFramework.UI.Button;
            Bordered = e as BorderedButton;
            Double = e as DoubleLabelBox;
            Radial = e as RadialSelectionBox;
            Shape = e as ShapeElement;
            AnyLabel = Label == null ? e as RichHudFramework.UI.Label : null;

            if (_text == null)
            {
                if (AnyLabel != null)
                    _text = _board = AnyLabel.TextBoard;
                else if (TreeL != null)
                    _text = _board = TreeL.TextBoard;
                else if (Tree != null)
                    _text = _board = Tree.TextBoard;
                else if (Double != null)
                    _text = Double.LeftTextBuilder;
                else if (NamedCheck != null)
                    _text = NamedCheck.NameBuilder;
                else if (NamedSlider != null)
                    _text = NamedSlider.NameBuilder;
                else if (Picker != null)
                    _text = Picker.NameBuilder;
                else if (NamedOnOff != null)
                    _textSink = r => NamedOnOff.Name = r;
            }

            // Mouse events of the controls (the basic elements are wired in Init)
            if (Button == null && Check == null && Click == null && Panel == null && Field == null && Window == null)
            {
                var clickable = e as IClickableElement;
                var mouse = clickable != null ? clickable.MouseInput : null;
                if (mouse != null)
                {
                    mouse.LeftClicked += (o, a) => Fire(UiEvent.Click, null);
                    mouse.RightClicked += (o, a) => Fire(UiEvent.RightClick, null);
                    mouse.CursorEntered += (o, a) => Fire(UiEvent.CursorEnter, null);
                    mouse.CursorExited += (o, a) => Fire(UiEvent.CursorExit, null);
                    if (TreeL != null || Tree != null)
                        mouse.LeftClicked += (o, a) => Fire(UiEvent.Toggled, null);
                }
            }

            if (Editor != null)
                Editor.ValueChanged += (o, a) => Fire(UiEvent.TextChanged, Editor.TextBoard.GetText().ToString());
            if (Drop != null)
                Drop.ValueChanged += (o, a) => Fire(UiEvent.ValueChanged, Drop.Value != null ? Drop.Value.AssocMember : null);
            if (Items != null)
                Items.ValueChanged += (o, a) => Fire(UiEvent.ValueChanged, Items.Value != null ? Items.Value.AssocMember : null);
            if (TreeL != null)
                TreeL.ValueChanged += (o, a) => Fire(UiEvent.ValueChanged, TreeL.Value != null ? TreeL.Value.AssocMember : null);
            if (Tree != null)
                Tree.ValueChanged += (o, a) => Fire(UiEvent.ValueChanged, Tree.Value != null ? Tree.Value.AssocMember : null);
            if (Picker != null)
                Picker.ValueChanged += (o, a) => Fire(UiEvent.ValueChanged, Hex(Picker.Value));
            if (OnOff != null)
                OnOff.ValueChanged += (o, a) => Fire(UiEvent.ValueChanged, OnOff.Value ? "1" : "0");
            if (NamedCheck != null)
                NamedCheck.ValueChanged += (o, a) => Fire(UiEvent.ValueChanged, NamedCheck.Value ? "1" : "0");
            if (NamedOnOff != null)
                NamedOnOff.ValueChanged += (o, a) => Fire(UiEvent.ValueChanged, NamedOnOff.Value ? "1" : "0");
            if (NamedSlider != null)
                NamedSlider.ValueChanged += (o, a) => Fire(UiEvent.ValueChanged, null);
            if (Bar != null)
                Bar.ValueChanged += (o, a) => Fire(UiEvent.ValueChanged, null);
            if (SlideBar != null)
                SlideBar.ValueChanged += (o, a) => Fire(UiEvent.ValueChanged, null);
        }

        /// <summary>Colour as "#RRGGBBAA".</summary>
        /// <param name="c">The colour.</param>
        public static string Hex(Color c) { return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2") + c.A.ToString("X2"); }

        /// <summary>Markup parsed with this node's base format (no links).</summary>
        /// <param name="markup">Text with markup.</param>
        public RichText Rich(string markup) { return UiMarkup.Parse(markup ?? "", _style, _linkColor, null); }

        private IMouseInput ControlMouse
        {
            get
            {
                var clickable = Element as IClickableElement;
                return clickable != null ? clickable.MouseInput : null;
            }
        }

        private void ApplyFormat(GlyphFormat format)
        {
            if (Drop != null) Drop.Format = format;
            else if (Items != null) Items.Format = format;
            else if (TreeL != null) TreeL.Format = format;
            else if (Tree != null) Tree.Format = format;
            else if (OnOff != null) OnOff.Format = format;
            else if (NamedOnOff != null) NamedOnOff.Format = format;
        }

        private static Func<char, bool> Filter(int filter)
        {
            switch (filter)
            {
                case 0: return ch => ch != '\n' && ch != '\r';
                case 1: return ch => ch >= '0' && ch <= '9';
                case 2: return ch => (ch >= '0' && ch <= '9') || ch == '-' || ch == '+' || ch == '.' || ch == ',';
            }
            return null;
        }

        private void ApplyTooltip()
        {
            var m = Mouse;
            if (m == null)
                return;
            if (string.IsNullOrEmpty(_tooltip))
            {
                m.ToolTip = null;
                return;
            }
            var tip = new ToolTip { text = UiMarkup.Parse(_tooltip, new UiMarkup.Style { Color = new Color(220, 235, 242), Size = 0.75f, Font = -1 }, _linkColor, null) };
            if (_tooltipColor.A > 0)
                tip.bgColor = _tooltipColor;
            m.ToolTip = tip;
        }

        /// <summary>World matrix of a world space node.</summary>
        /// <param name="matrix">Plane to world matrix (the plane faces along Backward).</param>
        public void SetMatrix(MatrixD matrix) { _matrix = matrix; }

        // ---------------------------------------------------------------- Vector2

        private bool SetVectorControls(int p, Vector2 v)
        {
            if (Term != null) { TermSetVector(p, v); return true; }
            switch ((UiProperty)p)
            {
                case UiProperty.SelectionStart: _selectionStart = v; return Editor != null;
                case UiProperty.SelectionEnd:
                    if (Editor == null) return false;
                    Editor.SetSelection(new Vector2I((int)_selectionStart.X, (int)_selectionStart.Y), new Vector2I((int)v.X, (int)v.Y));
                    return true;
                case UiProperty.HighlightPadding:
                    if (Drop != null) Drop.HighlightPadding = v;
                    else if (Items != null) Items.HighlightPadding = v;
                    else if (TreeL != null) TreeL.HighlightPadding = v;
                    else if (Tree != null) Tree.HighlightPadding = v;
                    return true;
                case UiProperty.MemberPadding:
                    if (Drop != null) Drop.MemberPadding = v;
                    else if (Items != null) Items.MemberPadding = v;
                    return true;
                case UiProperty.ListPadding: if (Items != null) Items.ListPadding = v; return true;
                case UiProperty.BackgroundPadding: if (OnOff != null) OnOff.BackgroundPadding = v; return true;
                case UiProperty.BarSize: if (SlideBar != null) SlideBar.BarSize = v; return true;
                case UiProperty.SliderSize: if (SlideBar != null) SlideBar.SliderSize = v; return true;
                case UiProperty.ButtonPadding: if (NamedOnOff != null) NamedOnOff.ButtonPadding = v; return true;
                case UiProperty.TextPadding:
                    if (NamedCheck == null) return false;
                    NamedCheck.TextPadding = v;
                    return true;
            }
            return false;
        }

        private bool GetVectorControls(int p, out Vector2 v)
        {
            v = Vector2.Zero;
            if (Term != null) { v = TermGetVector(p); return true; }
            switch ((UiProperty)p)
            {
                case UiProperty.SelectionStart: if (Editor != null) v = new Vector2(Editor.SelectionStart.X, Editor.SelectionStart.Y); return true;
                case UiProperty.SelectionEnd: if (Editor != null) v = new Vector2(Editor.SelectionEnd.X, Editor.SelectionEnd.Y); return true;
                case UiProperty.CaretPosition: if (Editor != null) v = new Vector2(Editor.CaretPosition.X, Editor.CaretPosition.Y); return true;
                case UiProperty.HighlightPadding:
                    v = Drop != null ? Drop.HighlightPadding : Items != null ? Items.HighlightPadding : TreeL != null ? TreeL.HighlightPadding : Tree != null ? Tree.HighlightPadding : Vector2.Zero;
                    return true;
                case UiProperty.MemberPadding: v = Drop != null ? Drop.MemberPadding : Items != null ? Items.MemberPadding : Vector2.Zero; return true;
                case UiProperty.ListPadding: v = Items != null ? Items.ListPadding : Vector2.Zero; return true;
                case UiProperty.BackgroundPadding: v = OnOff != null ? OnOff.BackgroundPadding : Vector2.Zero; return true;
                case UiProperty.BarSize: v = SlideBar != null ? SlideBar.BarSize : Vector2.Zero; return true;
                case UiProperty.SliderSize: v = SlideBar != null ? SlideBar.SliderSize : Vector2.Zero; return true;
                case UiProperty.ButtonPadding: v = NamedOnOff != null ? NamedOnOff.ButtonPadding : Vector2.Zero; return true;
                case UiProperty.TextPadding:
                    if (NamedCheck == null) return false;
                    v = NamedCheck.TextPadding;
                    return true;
                case UiProperty.TextSize:
                    if (NamedCheck == null) return false;
                    v = NamedCheck.TextSize;
                    return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- float

        private bool SetFloatControls(int p, float v)
        {
            if (Term != null) { TermSetFloat(p, v); return true; }
            switch ((UiProperty)p)
            {
                case UiProperty.LineHeight:
                    if (Drop != null) Drop.LineHeight = v;
                    else if (Items != null) Items.LineHeight = v;
                    else if (TreeL != null) TreeL.LineHeight = v;
                    return true;
                case UiProperty.DropdownHeight:
                    if (Drop != null) Drop.DropdownHeight = v;
                    else if (TreeL != null) TreeL.DropdownHeight = v;
                    else if (Tree != null) Tree.DropdownHeight = v;
                    return true;
                case UiProperty.IndentSize:
                    if (TreeL != null) TreeL.IndentSize = v;
                    else if (Tree != null) Tree.IndentSize = v;
                    return true;
                case UiProperty.LabelHeight:
                    if (TreeL != null) TreeL.LabelHeight = v;
                    else if (Tree != null) Tree.LabelHeight = v;
                    return true;
                case UiProperty.ButtonSpacing:
                    if (OnOff != null) OnOff.ButtonSpacing = v;
                    else if (NamedOnOff != null) NamedOnOff.ButtonSpacing = v;
                    return true;
                case UiProperty.Percent:
                    if (NamedSlider != null) NamedSlider.Percent = v;
                    else if (Bar != null) Bar.Percent = v;
                    else if (SlideBar != null) SlideBar.Percent = v;
                    else if (Slider != null) Slider.Percent = v;
                    return true;
                case UiProperty.VisiblePercent: if (Bar != null) Bar.VisiblePercent = v; return true;
                case UiProperty.CursorSensitivity: if (Radial != null) Radial.CursorSensitivity = v; return true;
                case UiProperty.InnerRadius:
                    if (Radial != null) Radial.InnerRadius = v;
                    else if (Shape != null) Shape.InnerRadius = v;
                    return true;
                case UiProperty.PlaneScale:
                    if (_cam != null) _cam.PlaneScale = v;
                    else if (_scaled != null) _scaled.PlaneScale = v;
                    return true;
                case UiProperty.RotationAngle: if (_cam != null) _cam.RotationAngle = v; return true;
                case UiProperty.MinLength: if (Items != null) Items.MinLength = v; return true;
                case UiProperty.Value:
                case UiProperty.Min:
                case UiProperty.Max:
                    if (NamedSlider != null) SetRange(p, v, NamedSlider.SliderBox);
                    else if (Bar != null) { if (p == (int)UiProperty.Value) Bar.Value = v; else if (p == (int)UiProperty.Min) Bar.Min = v; else Bar.Max = v; }
                    else if (SlideBar != null) { if (p == (int)UiProperty.Value) SlideBar.Value = v; else if (p == (int)UiProperty.Min) SlideBar.Min = v; else SlideBar.Max = v; }
                    else return false;
                    return true;
                case UiProperty.BorderThickness:
                    if (Bordered != null) Bordered.BorderThickness = v;
                    else if (Shape != null) Shape.Thickness = v;
                    else return false;
                    return true;
            }
            return false;
        }

        private static void SetRange(int p, float v, SliderBox box)
        {
            if (p == (int)UiProperty.Value) box.Value = v;
            else if (p == (int)UiProperty.Min) box.Min = v;
            else box.Max = v;
        }

        private bool GetFloatControls(int p, out float v)
        {
            v = 0f;
            if (Term != null) { v = TermGetFloat(p); return true; }
            switch ((UiProperty)p)
            {
                case UiProperty.LineHeight: v = Drop != null ? Drop.LineHeight : Items != null ? Items.LineHeight : TreeL != null ? TreeL.LineHeight : 0f; return true;
                case UiProperty.DropdownHeight: v = Drop != null ? Drop.DropdownHeight : TreeL != null ? TreeL.DropdownHeight : Tree != null ? Tree.DropdownHeight : 0f; return true;
                case UiProperty.IndentSize: v = TreeL != null ? TreeL.IndentSize : Tree != null ? Tree.IndentSize : 0f; return true;
                case UiProperty.LabelHeight: v = TreeL != null ? TreeL.LabelHeight : Tree != null ? Tree.LabelHeight : 0f; return true;
                case UiProperty.ButtonSpacing: v = OnOff != null ? OnOff.ButtonSpacing : NamedOnOff != null ? NamedOnOff.ButtonSpacing : 0f; return true;
                case UiProperty.Percent:
                    v = NamedSlider != null ? NamedSlider.Percent : Bar != null ? Bar.Percent : SlideBar != null ? SlideBar.Percent : Slider != null ? Slider.Percent : 0f;
                    return true;
                case UiProperty.VisiblePercent: v = Bar != null ? Bar.VisiblePercent : 0f; return true;
                case UiProperty.CursorSensitivity: v = Radial != null ? Radial.CursorSensitivity : 0f; return true;
                case UiProperty.InnerRadius: v = Radial != null ? Radial.InnerRadius : Shape != null ? Shape.InnerRadius : 0f; return true;
                case UiProperty.PlaneScale: v = _cam != null ? _cam.PlaneScale : _scaled != null ? _scaled.PlaneScale : 0f; return true;
                case UiProperty.RotationAngle: v = _cam != null ? _cam.RotationAngle : 0f; return true;
                case UiProperty.MinLength: v = Items != null ? Items.MinLength : 0f; return true;
                case UiProperty.Value:
                    if (NamedSlider != null) v = NamedSlider.Value; else if (Bar != null) v = Bar.Value; else if (SlideBar != null) v = SlideBar.Value; else return false;
                    return true;
                case UiProperty.Min:
                    if (NamedSlider != null) v = NamedSlider.Min; else if (Bar != null) v = Bar.Min; else if (SlideBar != null) v = SlideBar.Min; else return false;
                    return true;
                case UiProperty.Max:
                    if (NamedSlider != null) v = NamedSlider.Max; else if (Bar != null) v = Bar.Max; else if (SlideBar != null) v = SlideBar.Max; else return false;
                    return true;
                case UiProperty.BorderThickness:
                    if (Bordered != null) v = Bordered.BorderThickness; else if (Shape != null) v = Shape.Thickness; else return false;
                    return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- bool

        private bool SetBoolControls(int p, bool v)
        {
            if (Term != null) { TermSetBool(p, v); return true; }
            switch ((UiProperty)p)
            {
                case UiProperty.Visible:
                    if (Outer != null) Outer.Visible = v;
                    return false;
                case UiProperty.EnableEditing: if (Editor != null) Editor.EnableEditing = v; return true;
                case UiProperty.EnableHighlighting: if (Editor != null) Editor.EnableHighlighting = v; return true;
                case UiProperty.MoveToEndOnFocus: if (Editor != null) Editor.MoveToEndOnGainFocus = v; return true;
                case UiProperty.ClearSelectionOnBlur: if (Editor != null) Editor.ClearSelectionOnLoseFocus = v; return true;
                case UiProperty.InputOpen:
                    if (Editor == null) return false;
                    if (v && !Editor.InputOpen) Editor.OpenInput();
                    else if (!v && Editor.InputOpen) Editor.CloseInput();
                    return true;
                case UiProperty.Open:
                    if (Drop != null) { if (v) Drop.OpenList(); else Drop.CloseList(); }
                    else if (TreeL != null) { if (v) TreeL.OpenList(); else TreeL.CloseList(); }
                    else if (Tree != null) { if (v) Tree.OpenList(); else Tree.CloseList(); }
                    return true;
                case UiProperty.UseFocusFormatting:
                    if (OnOff != null) OnOff.UseFocusFormatting = v;
                    else if (Bordered != null) Bordered.UseFocusFormatting = v;
                    else if (Slider != null) Slider.UseFocusFormatting = v;
                    return true;
                case UiProperty.Vertical:
                    if (Bar != null) Bar.Vertical = v;
                    else if (SlideBar != null) SlideBar.Vertical = v;
                    return true;
                case UiProperty.SliderVisible: if (SlideBar != null) SlideBar.SliderVisible = v; return true;
                case UiProperty.Reverse: if (SlideBar != null) SlideBar.Reverse = v; return true;
                case UiProperty.UseGestureInput: if (Radial != null) Radial.UseGestureInput = v; return true;
                case UiProperty.IsScreenSpace: if (_cam != null) _cam.IsScreenSpace = v; return true;
                case UiProperty.UseResScaling: if (_cam != null) _cam.UseResScaling = v; return true;
                case UiProperty.ClosedLine: if (Shape != null) Shape.Closed = v; return true;
                case UiProperty.Checked:
                    if (OnOff != null) OnOff.Value = v;
                    else if (NamedCheck != null) NamedCheck.Value = v;
                    else if (NamedOnOff != null) NamedOnOff.Value = v;
                    else return false;
                    return true;
                case UiProperty.HighlightEnabled:
                    if (OnOff != null) OnOff.HighlightEnabled = v;
                    else if (Icon != null) Icon.HighlightEnabled = v;
                    else if (SlideBar != null) SlideBar.EnableHighlight = v;
                    else return false;
                    return true;
                case UiProperty.AutoResize:
                    if (AnyLabel != null) AnyLabel.AutoResize = v;
                    else if (NamedCheck != null) NamedCheck.AutoResize = v;
                    else return false;
                    return true;
                case UiProperty.VertCenter:
                    if (AnyLabel != null) AnyLabel.VertCenterText = v;
                    else if (NamedCheck != null) NamedCheck.VertCenterText = v;
                    else return false;
                    return true;
                case UiProperty.Scrolling:
                    if (Items == null) return false;
                    Items.EnableScrolling = v;
                    return true;
                case UiProperty.SmoothScrolling:
                    if (Items == null) return false;
                    Items.UseSmoothScrolling = v;
                    return true;
            }
            return false;
        }

        private bool GetBoolControls(int p, out bool v)
        {
            v = false;
            if (Term != null) { v = TermGetBool(p); return true; }
            switch ((UiProperty)p)
            {
                case UiProperty.EnableEditing: v = Editor != null && Editor.EnableEditing; return true;
                case UiProperty.EnableHighlighting: v = Editor != null && Editor.EnableHighlighting; return true;
                case UiProperty.MoveToEndOnFocus: v = Editor != null && Editor.MoveToEndOnGainFocus; return true;
                case UiProperty.ClearSelectionOnBlur: v = Editor != null && Editor.ClearSelectionOnLoseFocus; return true;
                case UiProperty.InputOpen:
                    if (Editor == null) return false;
                    v = Editor.InputOpen;
                    return true;
                case UiProperty.Open: v = Drop != null ? Drop.Open : TreeL != null ? TreeL.ListOpen : Tree != null && Tree.ListOpen; return true;
                case UiProperty.UseFocusFormatting: v = OnOff != null ? OnOff.UseFocusFormatting : Bordered != null ? Bordered.UseFocusFormatting : Slider != null && Slider.UseFocusFormatting; return true;
                case UiProperty.Vertical: v = Bar != null ? Bar.Vertical : SlideBar != null && SlideBar.Vertical; return true;
                case UiProperty.SliderVisible: v = SlideBar != null && SlideBar.SliderVisible; return true;
                case UiProperty.Reverse: v = SlideBar != null && SlideBar.Reverse; return true;
                case UiProperty.UseGestureInput: v = Radial != null && Radial.UseGestureInput; return true;
                case UiProperty.IsScreenSpace: v = _cam != null && _cam.IsScreenSpace; return true;
                case UiProperty.UseResScaling: v = _cam != null && _cam.UseResScaling; return true;
                case UiProperty.ClosedLine: v = Shape != null && Shape.Closed; return true;
                case UiProperty.IsInFront: v = Element.HudSpace != null && Element.HudSpace.IsInFront; return true;
                case UiProperty.IsFacingCamera: v = Element.HudSpace != null && Element.HudSpace.IsFacingCamera; return true;
                case UiProperty.Checked:
                    if (OnOff != null) v = OnOff.Value; else if (NamedCheck != null) v = NamedCheck.Value; else if (NamedOnOff != null) v = NamedOnOff.Value; else return false;
                    return true;
                case UiProperty.HighlightEnabled:
                    if (OnOff != null) v = OnOff.HighlightEnabled; else if (Icon != null) v = Icon.HighlightEnabled; else if (SlideBar != null) v = SlideBar.EnableHighlight; else return false;
                    return true;
                case UiProperty.AutoResize:
                    if (AnyLabel != null) v = AnyLabel.AutoResize; else if (NamedCheck != null) v = NamedCheck.AutoResize; else return false;
                    return true;
                case UiProperty.VertCenter:
                    if (AnyLabel != null) v = AnyLabel.VertCenterText; else if (NamedCheck != null) v = NamedCheck.VertCenterText; else return false;
                    return true;
                case UiProperty.Scrolling:
                    if (Items == null) return false;
                    v = Items.EnableScrolling;
                    return true;
                case UiProperty.SmoothScrolling:
                    if (Items == null) return false;
                    v = Items.UseSmoothScrolling;
                    return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- int

        private bool SetIntControls(int p, int v)
        {
            if (Term != null) { TermSetInt(p, v); return true; }
            switch ((UiProperty)p)
            {
                case UiProperty.NewLineChar: if (Editor != null) Editor.NewLineChar = (char)v; return true;
                case UiProperty.CharFilter:
                    if (Editor == null) return false;
                    Editor.CharFilterFunc = Filter(v);
                    return true;
                case UiProperty.SelectedIndex:
                    if (Tree != null) { if (v < 0) Tree.ClearSelection(); else if (v < Tree.Count) Tree.SetSelectionAt(v); }
                    else if (Radial != null) { if (v < 0) Radial.ClearSelection(); else Radial.SetSelectionAt(v); }
                    else UiItemList.SetSelectedIndex(this, v);
                    return true;
                case UiProperty.MinVisibleCount:
                    if (Drop != null) Drop.MinVisibleCount = v;
                    else if (Items != null) Items.MinVisibleCount = v;
                    return true;
                case UiProperty.MaxEntryCount: if (Radial != null) Radial.MaxEntryCount = v; return true;
                case UiProperty.HighlightIndex:
                    if (Radial != null) { if (v < 0) Radial.ClearHighlight(); else Radial.SetHighlightAt(v); }
                    return true;
                case UiProperty.Sides: if (Shape != null) Shape.Sides = v; return true;
            }
            return false;
        }

        private bool GetIntControls(int p, out int v)
        {
            v = 0;
            if (Term != null) { v = TermGetInt(p); return true; }
            switch ((UiProperty)p)
            {
                case UiProperty.NewLineChar: v = Editor != null ? Editor.NewLineChar : 0; return true;
                case UiProperty.SelectedIndex:
                    v = Tree != null ? Tree.selectionBox.SelectionIndex : Radial != null ? Radial.SelectionIndex : UiItemList.SelectedIndex(this);
                    return true;
                case UiProperty.MinVisibleCount: v = Drop != null ? Drop.MinVisibleCount : Items != null ? Items.MinVisibleCount : 0; return true;
                case UiProperty.MaxEntryCount: v = Radial != null ? Radial.MaxEntryCount : 0; return true;
                case UiProperty.HighlightIndex: v = Radial != null ? Radial.HighlightIndex : -1; return true;
                case UiProperty.Sides: v = Shape != null ? Shape.Sides : 0; return true;
                case UiProperty.ItemCount: v = Tree != null ? Tree.Count : Radial != null ? Radial.Count : UiItemList.Count(this); return true;
                case UiProperty.EnabledCount:
                    if (Radial == null) return false;
                    v = Radial.EnabledCount;
                    return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- Color

        private bool SetColorControls(int p, Color v)
        {
            if (Term != null) { TermSetColor(p, v); return true; }
            switch ((UiProperty)p)
            {
                case UiProperty.Color:
                    if (Drop != null) Drop.Color = v;
                    else if (Items != null) Items.Color = v;
                    else if (OnOff != null) OnOff.BackgroundColor = v;
                    else if (Radial != null) Radial.BackgroundColor = v;
                    else if (Shape != null) Shape.Color = v;
                    else if (NamedSlider != null) NamedSlider.SliderBox.BackgroundColor = v;
                    else return false;
                    return true;
                case UiProperty.HighlightColor:
                    if (Drop != null) Drop.HighlightColor = v;
                    else if (Items != null) Items.HighlightColor = v;
                    else if (TreeL != null) TreeL.HighlightColor = v;
                    else if (Tree != null) Tree.HighlightColor = v;
                    else if (OnOff != null) OnOff.HighlightColor = v;
                    else if (Icon != null) Icon.HighlightColor = v;
                    else if (Radial != null) Radial.HighlightColor = v;
                    else if (NamedSlider != null) NamedSlider.SliderBox.BackgroundHighlight = v;
                    else return false;
                    return true;
                case UiProperty.BorderColor:
                    if (Bordered != null) Bordered.BorderColor = v;
                    else if (OnOff != null) OnOff.BorderColor = v;
                    else if (NamedOnOff != null) NamedOnOff.BorderColor = v;
                    else if (NamedSlider != null) NamedSlider.SliderBox.BorderColor = v;
                    else return false;
                    return true;
                case UiProperty.FocusColor:
                    if (Items != null) Items.FocusColor = v;
                    else if (TreeL != null) TreeL.FocusColor = v;
                    else if (Tree != null) Tree.FocusColor = v;
                    else if (OnOff != null) OnOff.FocusColor = v;
                    else if (Bordered != null) Bordered.FocusColor = v;
                    else if (NamedSlider != null) NamedSlider.SliderBox.BackgroundFocusColor = v;
                    else return false;
                    return true;
                case UiProperty.BarColor:
                    if (Drop != null) Drop.BarColor = v;
                    else if (Items != null) Items.BarColor = v;
                    else if (Bar != null) Bar.SlideInput.BarColor = v;
                    else if (SlideBar != null) SlideBar.BarColor = v;
                    else if (NamedSlider != null) NamedSlider.SliderBox.BarColor = v;
                    else return false;
                    return true;
                case UiProperty.BarHighlight:
                    if (Drop != null) Drop.BarHighlight = v;
                    else if (Items != null) Items.BarHighlight = v;
                    else if (Bar != null) Bar.SlideInput.BarHighlight = v;
                    else if (SlideBar != null) SlideBar.BarHighlight = v;
                    else if (NamedSlider != null) NamedSlider.SliderBox.BarHighlight = v;
                    else return false;
                    return true;
                case UiProperty.SliderColor:
                    if (Drop != null) Drop.SliderColor = v;
                    else if (Items != null) Items.SliderColor = v;
                    else if (Bar != null) Bar.SlideInput.SliderColor = v;
                    else if (SlideBar != null) SlideBar.SliderColor = v;
                    else if (NamedSlider != null) NamedSlider.SliderBox.SliderColor = v;
                    else return false;
                    return true;
                case UiProperty.SliderHighlight:
                    if (Drop != null) Drop.SliderHighlight = v;
                    else if (Items != null) Items.SliderHighlight = v;
                    else if (Bar != null) Bar.SlideInput.SliderHighlight = v;
                    else if (SlideBar != null) SlideBar.SliderHighlight = v;
                    else if (NamedSlider != null) NamedSlider.SliderBox.SliderHighlight = v;
                    else return false;
                    return true;
                case UiProperty.HeaderColor:
                    if (TreeL != null) TreeL.HeaderColor = v;
                    else if (Tree != null) Tree.HeaderColor = v;
                    else return false;
                    return true;
                case UiProperty.TabColor:
                    if (Drop != null) Drop.TabColor = v;
                    else if (Items != null) Items.TabColor = v;
                    else if (TreeL != null) TreeL.TabColor = v;
                    else if (Tree != null) Tree.TabColor = v;
                    return true;
                case UiProperty.FocusTextColor:
                    if (Items != null) Items.FocusTextColor = v;
                    else if (TreeL != null) TreeL.FocusTextColor = v;
                    else if (Tree != null) Tree.FocusTextColor = v;
                    else if (Bordered != null) Bordered.FocusTextColor = v;
                    return true;
                case UiProperty.ValueColor: if (Picker != null) Picker.Value = v; return true;
                case UiProperty.UnselectedColor: if (OnOff != null) OnOff.UnselectedColor = v; return true;
                case UiProperty.SelectionColor:
                    if (OnOff != null) OnOff.SelectionColor = v;
                    else if (Radial != null) Radial.SelectionColor = v;
                    return true;
                case UiProperty.TooltipColor:
                    _tooltipColor = v;
                    ApplyTooltip();
                    return true;
                case UiProperty.BarFocusColor:
                    if (Slider != null) Slider.BarFocusColor = v;
                    else if (NamedSlider != null) NamedSlider.SliderBox.BarFocusColor = v;
                    return true;
                case UiProperty.SliderFocusColor:
                    if (Slider != null) Slider.SliderFocusColor = v;
                    else if (NamedSlider != null) NamedSlider.SliderBox.SliderFocusColor = v;
                    return true;
            }
            return false;
        }

        private bool GetColorControls(int p, out Color v)
        {
            v = Color.Transparent;
            if (Term != null) { v = TermGetColor(p); return true; }
            switch ((UiProperty)p)
            {
                case UiProperty.Color:
                    if (Drop != null) v = Drop.Color; else if (Items != null) v = Items.Color; else if (OnOff != null) v = OnOff.BackgroundColor;
                    else if (Radial != null) v = Radial.BackgroundColor; else if (Shape != null) v = Shape.Color; else return false;
                    return true;
                case UiProperty.HighlightColor:
                    if (Drop != null) v = Drop.HighlightColor; else if (Items != null) v = Items.HighlightColor; else if (TreeL != null) v = TreeL.HighlightColor;
                    else if (Tree != null) v = Tree.HighlightColor; else if (OnOff != null) v = OnOff.HighlightColor; else if (Icon != null) v = Icon.HighlightColor;
                    else if (Radial != null) v = Radial.HighlightColor; else return false;
                    return true;
                case UiProperty.BorderColor:
                    if (Bordered != null) v = Bordered.BorderColor; else if (OnOff != null) v = OnOff.BorderColor; else if (NamedOnOff != null) v = NamedOnOff.BorderColor; else return false;
                    return true;
                case UiProperty.HeaderColor:
                    if (TreeL != null) v = TreeL.HeaderColor; else if (Tree != null) v = Tree.HeaderColor; else return false;
                    return true;
                case UiProperty.TabColor: v = Drop != null ? Drop.TabColor : Items != null ? Items.TabColor : TreeL != null ? TreeL.TabColor : Tree != null ? Tree.TabColor : v; return true;
                case UiProperty.ValueColor: if (Picker != null) v = Picker.Value; return true;
                case UiProperty.UnselectedColor: if (OnOff != null) v = OnOff.UnselectedColor; return true;
                case UiProperty.SelectionColor: v = OnOff != null ? OnOff.SelectionColor : Radial != null ? Radial.SelectionColor : v; return true;
                case UiProperty.TooltipColor: v = _tooltipColor; return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- string

        private bool SetTextControls(int p, string v)
        {
            if (Term != null) { TermSetText(p, v); return true; }
            switch ((UiProperty)p)
            {
                case UiProperty.OnText:
                    if (OnOff != null) OnOff.OnText = Rich(v);
                    else if (NamedOnOff != null) NamedOnOff.OnText = Rich(v);
                    return true;
                case UiProperty.OffText:
                    if (OnOff != null) OnOff.OffText = Rich(v);
                    else if (NamedOnOff != null) NamedOnOff.OffText = Rich(v);
                    return true;
                case UiProperty.RightText: if (Double != null) Double.RightText = Rich(v); return true;
                case UiProperty.ValueText: if (NamedSlider != null) NamedSlider.ValueText = Rich(v); return true;
                case UiProperty.SelectedKey: UiItemList.SelectKey(this, v); return true;
                case UiProperty.Tooltip:
                    _tooltip = v;
                    ApplyTooltip();
                    return true;
            }
            return false;
        }

        private bool GetTextControls(int p, out string v)
        {
            v = null;
            if (Term != null) { v = TermGetText(p); return true; }
            switch ((UiProperty)p)
            {
                case UiProperty.Text:
                case UiProperty.PlainText:
                    if (Editor == null) return false;
                    v = Editor.TextBoard.GetText().ToString();
                    return true;
                case UiProperty.OnText: v = OnOff != null ? OnOff.OnText.ToString() : NamedOnOff != null ? NamedOnOff.OnText.ToString() : null; return true;
                case UiProperty.OffText: v = OnOff != null ? OnOff.OffText.ToString() : NamedOnOff != null ? NamedOnOff.OffText.ToString() : null; return true;
                case UiProperty.RightText: v = Double != null ? Double.RightText.ToString() : null; return true;
                case UiProperty.ValueText: v = NamedSlider != null ? NamedSlider.ValueText.ToString() : null; return true;
                case UiProperty.SelectedKey: v = UiItemList.SelectedKey(this); return true;
                case UiProperty.Tooltip: v = _tooltip; return true;
            }
            return false;
        }

        // ---------------------------------------------------------------- Vector3D

        /// <summary>Sets a Vector3D property (HSV colour, cam space rotation axis and offset).</summary>
        /// <param name="p">UiProperty number.</param>
        /// <param name="v">New value.</param>
        public void SetVector3(int p, Vector3D v)
        {
            switch ((UiProperty)p)
            {
                case UiProperty.ColorHsv: if (PickerHsv != null) PickerHsv.ColorHSV = (Vector3)v; break;
                case UiProperty.RotationAxis: if (_cam != null) _cam.RotationAxis = (Vector3)v; break;
                case UiProperty.TransformOffset: if (_cam != null) _cam.TransformOffset = v; break;
            }
        }

        /// <summary>Reads a Vector3D property.</summary>
        /// <param name="p">UiProperty number.</param>
        public Vector3D GetVector3(int p)
        {
            switch ((UiProperty)p)
            {
                case UiProperty.ColorHsv: return PickerHsv != null ? (Vector3D)PickerHsv.ColorHSV : Vector3D.Zero;
                case UiProperty.RotationAxis: return _cam != null ? (Vector3D)_cam.RotationAxis : Vector3D.Zero;
                case UiProperty.TransformOffset: return _cam != null ? _cam.TransformOffset : Vector3D.Zero;
                case UiProperty.SpaceCursor: return Element != null && Element.HudSpace != null ? (Vector3D)Element.HudSpace.CursorPos : Vector3D.Zero;
            }
            return Vector3D.Zero;
        }
    }
}
