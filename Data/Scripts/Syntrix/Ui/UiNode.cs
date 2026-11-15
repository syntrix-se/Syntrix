// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System.Collections.Generic;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using VRageMath;

namespace Syntrix.Ui
{
    /// <summary>
    /// One element of a docked mod: the real Rich HUD element, its place in the tree and its state.
    /// Properties are dispatched by the element's type, so a property works on every element that has it.
    /// Set calls do not allocate except for texts and textures.
    /// </summary>
    public sealed class UiNode
    {
        public readonly UiClient Client;
        public readonly int Handle;
        public readonly UiKind Kind;

        public HudElementBase Element;
        public UiNode Parent;
        public List<UiNode> Children;
        /// <summary>Entry in the parent's scroll box, null otherwise.</summary>
        public ScrollBoxEntry Entry;
        /// <summary>Member of the parent's chain.</summary>
        public bool InChain;
        public bool Dead;
        public UiNode[] Parts;
        /// <summary>Bit per UiEvent the mod listens to.</summary>
        public int Listened;
        /// <summary>Label links: packed colour to target.</summary>
        public Dictionary<uint, string> Links;

        // Typed views of Element (null where the element is no such type)
        public TexturedBox Box;
        public BorderBox Border;
        public LabelElement Label;
        public LabelBox LabelBox;
        public LabelBoxBase LabelBase;
        public LabelBoxButton Button;
        public PanelElement Panel;
        public TextFieldElement Field;
        public ScrollBox Scroll;
        public HudChain Chain;
        public MouseInputElement Click;
        public WindowElement Window;
        public BorderedCheckBox Check;
        public SliderBox Slider;
        private ITextBuilder _text;
        private ITextBoard _board;
        private BorderBox _buttonBorder;

        // Text state
        private UiMarkup.Style _style;
        private Color _linkColor = new Color(47, 209, 197);
        private string _markup, _placeholder;
        private bool _plain;

        // Texture state
        private string _texture;
        private Vector2 _texSize, _texOffset, _texRegion;

        public UiNode(UiClient client, int handle, UiKind kind)
        {
            Client = client;
            Handle = handle;
            Kind = kind;
            _style = new UiMarkup.Style { Color = Color.White, Size = 1f, Font = -1, Align = TextAlignment.Left };
        }

        /// <summary>Makes the Rich HUD element for this node's kind; null for Part.</summary>
        public static HudElementBase Make(UiNode n)
        {
            switch (n.Kind)
            {
                case UiKind.Empty: return new EmptyHudElement();
                case UiKind.Box: return new TexturedBox { Color = Color.White, Size = new Vector2(32f, 32f) };
                case UiKind.Border: return new BorderBox { Color = Color.White, Thickness = 1f, Size = new Vector2(32f, 32f) };
                case UiKind.Label: return new LabelElement(n);
                case UiKind.LabelBox: return new LabelBox { AutoResize = true, Color = new Color(0, 0, 0, 160) };
                case UiKind.Panel: return new PanelElement(n);
                case UiKind.Button: return new LabelBoxButton { AutoResize = false, Size = new Vector2(120f, 34f) };
                case UiKind.TextField: return new TextFieldElement(n);
                case UiKind.ScrollBox:
                case UiKind.ScrollBoxHorizontal:
                    return new ScrollBox(n.Kind == UiKind.ScrollBox)
                    {
                        SizingMode = HudChainSizingModes.FitMembersOffAxis,
                        EnableScrolling = true,
                        UseSmoothScrolling = true,
                        Size = new Vector2(300f, 300f),
                    };
                case UiKind.Chain:
                case UiKind.ChainHorizontal:
                    return new HudChain(n.Kind == UiKind.Chain) { SizingMode = HudChainSizingModes.FitChainBoth };
                case UiKind.ClickArea: return new MouseInputElement { Size = new Vector2(32f, 32f) };
                case UiKind.Window: return new WindowElement(n);
                case UiKind.CheckBox: return new BorderedCheckBox();
                case UiKind.Slider: return new SliderBox { Size = new Vector2(240f, 30f) };
            }
            return null;
        }

        /// <summary>Takes the element and wires its events. Called once.</summary>
        public void Init(HudElementBase e)
        {
            Element = e;
            Box = e as TexturedBox;
            Border = e as BorderBox;
            Label = e as LabelElement;
            LabelBox = e as LabelBox;
            LabelBase = e as LabelBoxBase;
            Button = e as LabelBoxButton;
            Panel = e as PanelElement;
            Field = e as TextFieldElement;
            Scroll = e as ScrollBox;
            Chain = e as HudChain;
            Click = e as MouseInputElement;
            Window = e as WindowElement;
            Check = e as BorderedCheckBox;
            Slider = e as SliderBox;

            if (Label != null)
            {
                _text = _board = Label.TextBoard;
                Label.BuilderMode = TextBuilderModes.Lined;
                Label.AutoResize = true;
            }
            else if (LabelBox != null)
            {
                _text = _board = LabelBox.TextBoard;
                if (Button != null)
                    _style.Align = TextAlignment.Center;
            }
            else if (Window != null)
                _text = Window.HeaderBuilder;
            else if (e is WindowBase)
                _text = ((WindowBase)e).HeaderBuilder;
            if (_text != null && Kind != UiKind.Part)
                RenderText();

            IMouseInput mouse = null;
            if (Button != null)
                mouse = Button.MouseInput;
            else if (Check != null)
                mouse = Check.MouseInput;
            else if (Click != null)
                mouse = Click;
            if (mouse != null)
            {
                mouse.LeftClicked += (o, a) => Fire(UiEvent.Click, null);
                mouse.RightClicked += (o, a) => Fire(UiEvent.RightClick, null);
                mouse.CursorEntered += (o, a) => Fire(UiEvent.CursorEnter, null);
                mouse.CursorExited += (o, a) => Fire(UiEvent.CursorExit, null);
            }
            if (Check != null)
                Check.ValueChanged += (o, a) => Fire(UiEvent.ValueChanged, Check.Value ? "1" : "0");
            if (Slider != null)
                Slider.ValueChanged += (o, a) => Fire(UiEvent.ValueChanged, null);
        }

        /// <summary>Where children of this node are registered.</summary>
        public HudParentBase ContentParent { get { return Window != null ? Window.body : Element; } }

        public void Fire(UiEvent ev, string arg)
        {
            if (!Dead && ((Listened >> (int)ev) & 1) != 0)
                Client.Queue(this, (int)ev, arg);
        }

        public void Listen(int ev, bool on)
        {
            if (ev <= 0 || ev >= 31)
                return;
            if (on)
                Listened |= 1 << ev;
            else
                Listened &= ~(1 << ev);
            if (on && Panel != null && ev >= (int)UiEvent.Click && ev <= (int)UiEvent.CursorExit)
                Panel.Input.Visible = true;
            if (on && Label != null && ev == (int)UiEvent.Link)
            {
                Label.UseCursor = true;
                Label.ShareCursor = true;
            }
        }

        private IMouseInput Mouse
        {
            get
            {
                if (Panel != null) return Panel.Input;
                if (Button != null) return Button.MouseInput;
                if (Check != null) return Check.MouseInput;
                if (Click != null) return Click;
                if (Field != null) return Field.MouseInput;
                if (Slider != null) return Slider.MouseInput;
                return null;
            }
        }

        private BorderBox ButtonBorder
        {
            get
            {
                if (_buttonBorder == null && Button != null)
                    _buttonBorder = new BorderBox(Button) { DimAlignment = DimAlignments.Size, Thickness = 1f, Color = Color.White };
                return _buttonBorder;
            }
        }

        // ---------------------------------------------------------------- parts

        /// <summary>Window: 1 header, 2 body, 3 border. Panel and scroll box: 1 background.</summary>
        public HudElementBase PartElement(int part)
        {
            if (Window != null)
            {
                if (part == 1) return Window.header;
                if (part == 2) return Window.body;
                if (part == 3) return Window.border;
            }
            if (part == 1 && Panel != null) return Panel.Background;
            if (part == 1 && Scroll != null) return Scroll.Background;
            return null;
        }

        // ---------------------------------------------------------------- text

        private void RenderText()
        {
            var format = UiMarkup.Format(_style);
            if (Field != null)
            {
                Field.Format = format;
                Field.SetPlain(Field.Current, format);
                RenderPlaceholder();
                return;
            }
            if (_text == null)
                return;
            _text.Format = format;
            if (_plain || string.IsNullOrEmpty(_markup))
                _text.SetText(_markup ?? "", format);
            else
            {
                Dictionary<uint, string> links = null;
                if (Label != null)
                    links = Links ?? (Links = new Dictionary<uint, string>());
                _text.SetText(UiMarkup.Parse(_markup, _style, _linkColor, links));
            }
        }

        private void RenderPlaceholder()
        {
            var s = _style;
            s.Color = new Color(s.Color.R * 6 / 10, s.Color.G * 6 / 10, s.Color.B * 6 / 10, s.Color.A);
            Field.Hint.Text = UiMarkup.Parse(_placeholder, s, _linkColor, null);
        }

        /// <summary>Base text format of this node.</summary>
        public UiMarkup.Style Style { get { return _style; } }

        // ---------------------------------------------------------------- texture

        private void UpdateMaterial()
        {
            Material m;
            if (string.IsNullOrEmpty(_texture))
                m = Material.Default;
            else
            {
                var size = _texSize.X > 0f && _texSize.Y > 0f ? _texSize : new Vector2(256f, 256f);
                m = _texRegion.X > 0f && _texRegion.Y > 0f
                    ? new Material(_texture, size, _texOffset, _texRegion)
                    : new Material(_texture, size);
            }
            if (Box != null)
                Box.Material = m;
            else if (Panel != null)
                Panel.Background.Material = m;
            else if (Border != null)
                Border.Material = m;
        }

        // ---------------------------------------------------------------- Vector2

        public void SetVector(int p, Vector2 v)
        {
            switch ((UiProperty)p)
            {
                case UiProperty.Offset: Element.Offset = v; break;
                case UiProperty.Size: Element.Size = v; break;
                case UiProperty.Padding: Element.Padding = v; break;
                case UiProperty.MinSize: if (Window != null) Window.MinimumSize = v; break;
                case UiProperty.TextPadding: if (LabelBase != null) LabelBase.TextPadding = v; break;
                case UiProperty.TextureSize: _texSize = v; UpdateMaterial(); break;
                case UiProperty.TextureOffset: _texOffset = v; UpdateMaterial(); break;
                case UiProperty.TextureRegion: _texRegion = v; UpdateMaterial(); break;
                case UiProperty.MemberMaxSize:
                    if (Scroll != null) Scroll.MemberMaxSize = v;
                    else if (Chain != null) Chain.MemberMaxSize = v;
                    break;
                case UiProperty.MemberMinSize:
                    if (Scroll != null) Scroll.MemberMinSize = v;
                    else if (Chain != null) Chain.MemberMinSize = v;
                    break;
            }
        }

        public Vector2 GetVector(int p)
        {
            switch ((UiProperty)p)
            {
                case UiProperty.Offset: return Element.Offset;
                case UiProperty.Size: return Element.Size;
                case UiProperty.Padding: return Element.Padding;
                case UiProperty.MinSize: return Window != null ? Window.MinimumSize : Vector2.Zero;
                case UiProperty.TextPadding: return LabelBase != null ? LabelBase.TextPadding : Vector2.Zero;
                case UiProperty.TextureSize: return _texSize;
                case UiProperty.TextureOffset: return _texOffset;
                case UiProperty.TextureRegion: return _texRegion;
                case UiProperty.Position: return Element.Position;
                case UiProperty.TextSize:
                    if (_board != null) return _board.TextSize;
                    if (Field != null) return Field.TextBoard.TextSize;
                    return Vector2.Zero;
                case UiProperty.MemberMaxSize: return Scroll != null ? Scroll.MemberMaxSize : Chain != null ? Chain.MemberMaxSize : Vector2.Zero;
                case UiProperty.MemberMinSize: return Scroll != null ? Scroll.MemberMinSize : Chain != null ? Chain.MemberMinSize : Vector2.Zero;
            }
            return Vector2.Zero;
        }

        // ---------------------------------------------------------------- float

        public void SetFloat(int p, float v)
        {
            switch ((UiProperty)p)
            {
                case UiProperty.Width: Element.Width = v; break;
                case UiProperty.Height: Element.Height = v; break;
                case UiProperty.Spacing:
                    if (Scroll != null) Scroll.Spacing = v;
                    else if (Chain != null) Chain.Spacing = v;
                    break;
                case UiProperty.BorderThickness:
                    if (Border != null) Border.Thickness = v;
                    else if (Panel != null) Panel.Border.Thickness = v;
                    else if (Button != null) ButtonBorder.Thickness = v;
                    else if (Field != null) Field.BorderThickness = v;
                    else if (Check != null) Check.BorderThickness = v;
                    break;
                case UiProperty.FontSize:
                    if (_style.Size != v) { _style.Size = v; RenderText(); }
                    break;
                case UiProperty.WrapWidth:
                    if (_text != null) _text.LineWrapWidth = v;
                    break;
                case UiProperty.WrapToParent:
                    if (Label != null)
                    {
                        Label.WrapToParent = v;
                        if (v >= 0f) Label.BuilderMode = TextBuilderModes.Wrapped;
                    }
                    break;
                case UiProperty.WrapMax: if (Label != null) Label.WrapMax = v; break;
                case UiProperty.Value: if (Slider != null) Slider.Value = v; break;
                case UiProperty.Min: if (Slider != null) Slider.Min = v; break;
                case UiProperty.Max: if (Slider != null) Slider.Max = v; break;
                case UiProperty.HeaderHeight: if (Window != null) Window.header.Height = v; break;
            }
        }

        public float GetFloat(int p)
        {
            switch ((UiProperty)p)
            {
                case UiProperty.Width: return Element.Width;
                case UiProperty.Height: return Element.Height;
                case UiProperty.Spacing: return Scroll != null ? Scroll.Spacing : Chain != null ? Chain.Spacing : 0f;
                case UiProperty.BorderThickness:
                    if (Border != null) return Border.Thickness;
                    if (Panel != null) return Panel.HasBorder ? Panel.Border.Thickness : 0f;
                    if (Button != null) return _buttonBorder != null ? _buttonBorder.Thickness : 0f;
                    if (Field != null) return Field.BorderThickness;
                    return 0f;
                case UiProperty.FontSize: return _style.Size;
                case UiProperty.WrapWidth: return _text != null ? _text.LineWrapWidth : 0f;
                case UiProperty.WrapToParent: return Label != null ? Label.WrapToParent : -1f;
                case UiProperty.WrapMax: return Label != null ? Label.WrapMax : 0f;
                case UiProperty.Value: return Slider != null ? Slider.Value : 0f;
                case UiProperty.Min: return Slider != null ? Slider.Min : 0f;
                case UiProperty.Max: return Slider != null ? Slider.Max : 0f;
                case UiProperty.HeaderHeight: return Window != null ? Window.header.Height : 0f;
            }
            return 0f;
        }

        // ---------------------------------------------------------------- bool

        public void SetBool(int p, bool v)
        {
            switch ((UiProperty)p)
            {
                case UiProperty.Visible:
                    if (Entry != null)
                        Entry.Enabled = v;
                    bool was = Element.Visible;
                    Element.Visible = v;
                    if (Window != null && v && !was)
                        Window.GetWindowFocus();
                    break;
                case UiProperty.AutoResize:
                    if (Label != null) Label.AutoResize = v;
                    else if (LabelBase != null) LabelBase.AutoResize = v;
                    break;
                case UiProperty.Wrap:
                    if (_text != null && Field == null)
                        _text.BuilderMode = v ? TextBuilderModes.Wrapped : TextBuilderModes.Lined;
                    break;
                case UiProperty.VertCenter:
                    if (Label != null) Label.VertCenterText = v;
                    else if (LabelBox != null) LabelBox.VertCenterText = v;
                    else if (Field != null) Field.VertCenterText = v;
                    break;
                case UiProperty.Draggable: if (Window != null) Window.CanDrag = v; break;
                case UiProperty.Resizable: if (Window != null) Window.AllowResizing = v; break;
                case UiProperty.HideHud: if (Window != null) Window.HideHud = v; break;
                case UiProperty.LockView: if (Window != null) Window.LockView = v; break;
                case UiProperty.CloseOnEscape: if (Window != null) Window.CloseOnEscape = v; break;
                case UiProperty.Modal: if (Window != null) Window.Modal = v; break;
                case UiProperty.SmoothScrolling: if (Scroll != null) Scroll.UseSmoothScrolling = v; break;
                case UiProperty.Scrolling: if (Scroll != null) Scroll.EnableScrolling = v; break;
                case UiProperty.HighlightEnabled:
                    if (Button != null) Button.HighlightEnabled = v;
                    else if (Field != null) Field.HighlightEnabled = v;
                    else if (Slider != null) Slider.HighlightEnabled = v;
                    else if (Check != null) Check.HighlightEnabled = v;
                    break;
                case UiProperty.Checked: if (Check != null) Check.Value = v; break;
                case UiProperty.InputOpen:
                    if (Field != null)
                    {
                        if (v && !Field.InputOpen) Field.OpenInput();
                        else if (!v && Field.InputOpen) Field.CloseInput();
                    }
                    break;
                case UiProperty.InputEnabled: Element.InputEnabled = v; break;
                case UiProperty.Masking: Element.IsMasking = v; break;
            }
        }

        public bool GetBool(int p)
        {
            switch ((UiProperty)p)
            {
                case UiProperty.Visible: return Element.Visible;
                case UiProperty.AutoResize: return Label != null ? Label.AutoResize : LabelBase != null && LabelBase.AutoResize;
                case UiProperty.Wrap: return _text != null && _text.BuilderMode == TextBuilderModes.Wrapped;
                case UiProperty.VertCenter: return Label != null ? Label.VertCenterText : LabelBox != null ? LabelBox.VertCenterText : Field != null && Field.VertCenterText;
                case UiProperty.Draggable: return Window != null && Window.CanDrag;
                case UiProperty.Resizable: return Window != null && Window.AllowResizing;
                case UiProperty.HideHud: return Window != null && Window.HideHud;
                case UiProperty.LockView: return Window != null && Window.LockView;
                case UiProperty.CloseOnEscape: return Window != null && Window.CloseOnEscape;
                case UiProperty.Modal: return Window != null && Window.Modal;
                case UiProperty.SmoothScrolling: return Scroll != null && Scroll.UseSmoothScrolling;
                case UiProperty.Scrolling: return Scroll != null && Scroll.EnableScrolling;
                case UiProperty.HighlightEnabled: return Button != null ? Button.HighlightEnabled : Field != null && Field.HighlightEnabled;
                case UiProperty.Checked: return Check != null && Check.Value;
                case UiProperty.MousedOver:
                    var m = Mouse;
                    return m != null ? m.IsMousedOver : Element.IsMousedOver;
                case UiProperty.InputOpen: return Field != null && Field.InputOpen;
                case UiProperty.InputEnabled: return Element.InputEnabled;
                case UiProperty.Masking: return Element.IsMasking;
                case UiProperty.Focused: return Window != null && Window.WindowActive;
            }
            return false;
        }

        // ---------------------------------------------------------------- int

        public void SetInt(int p, int v)
        {
            switch ((UiProperty)p)
            {
                case UiProperty.ParentAlign: Element.ParentAlignment = (ParentAlignments)v; break;
                case UiProperty.DimAlign: Element.DimAlignment = (DimAlignments)v; break;
                case UiProperty.TextAlign:
                    var a = (TextAlignment)MathHelper.Clamp(v, 0, 2);
                    if (_style.Align != a) { _style.Align = a; RenderText(); }
                    break;
                case UiProperty.FontStyle:
                    if (_style.FontStyle != v) { _style.FontStyle = v; RenderText(); }
                    break;
                case UiProperty.Sizing:
                    if (Scroll != null) Scroll.SizingMode = (HudChainSizingModes)v;
                    else if (Chain != null) Chain.SizingMode = (HudChainSizingModes)v;
                    break;
                case UiProperty.Start: if (Scroll != null) Scroll.Start = v; break;
                case UiProperty.ZOffset: Element.ZOffset = (sbyte)MathHelper.Clamp(v, sbyte.MinValue, sbyte.MaxValue); break;
                case UiProperty.MaxLength: if (Field != null) Field.MaxLength = v; break;
                case UiProperty.CharFilter: if (Field != null) Field.Filter = v; break;
                case UiProperty.TextureAlign:
                    if (Box != null) Box.MatAlignment = (MaterialAlignment)v;
                    else if (Panel != null) Panel.Background.MatAlignment = (MaterialAlignment)v;
                    break;
            }
        }

        public int GetInt(int p)
        {
            switch ((UiProperty)p)
            {
                case UiProperty.ParentAlign: return (int)Element.ParentAlignment;
                case UiProperty.DimAlign: return (int)Element.DimAlignment;
                case UiProperty.TextAlign: return (int)_style.Align;
                case UiProperty.FontStyle: return _style.FontStyle;
                case UiProperty.Sizing: return Scroll != null ? (int)Scroll.SizingMode : Chain != null ? (int)Chain.SizingMode : 0;
                case UiProperty.Start: return Scroll != null ? Scroll.Start : 0;
                case UiProperty.End: return Scroll != null ? Scroll.End : 0;
                case UiProperty.EnabledCount: return Scroll != null ? Scroll.EnabledCount : 0;
                case UiProperty.ZOffset: return Element.ZOffset;
                case UiProperty.MaxLength: return Field != null ? Field.MaxLength : 0;
                case UiProperty.CharFilter: return Field != null ? Field.Filter : 0;
                case UiProperty.Count: return Children != null ? Children.Count : 0;
                case UiProperty.TextureAlign: return Box != null ? (int)Box.MatAlignment : Panel != null ? (int)Panel.Background.MatAlignment : 0;
            }
            return 0;
        }

        // ---------------------------------------------------------------- Color

        public void SetColor(int p, Color v)
        {
            switch ((UiProperty)p)
            {
                case UiProperty.Color:
                    if (Box != null) Box.Color = v;
                    else if (Border != null) Border.Color = v;
                    else if (Panel != null) Panel.Color = v;
                    else if (LabelBase != null) LabelBase.Color = v;
                    else if (Scroll != null) Scroll.Color = v;
                    else if (Window != null) Window.BodyColor = v;
                    else if (Slider != null) Slider.BackgroundColor = v;
                    break;
                case UiProperty.HighlightColor:
                    if (Button != null) Button.HighlightColor = v;
                    else if (Panel != null) Panel.HighlightColor = v;
                    else if (Field != null) Field.HighlightColor = v;
                    else if (Slider != null) Slider.BackgroundHighlight = v;
                    else if (Check != null) Check.HighlightColor = v;
                    break;
                case UiProperty.BorderColor:
                    if (Border != null) Border.Color = v;
                    else if (Panel != null) Panel.Border.Color = v;
                    else if (Button != null) ButtonBorder.Color = v;
                    else if (Field != null) Field.BorderColor = v;
                    else if (Window != null) Window.BorderColor = v;
                    else if (Check != null) Check.BorderColor = v;
                    else if (Slider != null) Slider.BorderColor = v;
                    break;
                case UiProperty.TextColor:
                    if (_style.Color != v) { _style.Color = v; RenderText(); }
                    break;
                case UiProperty.LinkColor:
                    if (_linkColor != v) { _linkColor = v; RenderText(); }
                    break;
                case UiProperty.FocusColor:
                    if (Field != null) Field.FocusColor = v;
                    else if (Check != null) Check.FocusColor = v;
                    else if (Slider != null) Slider.BackgroundFocusColor = v;
                    break;
                case UiProperty.BarColor:
                    if (Scroll != null) Scroll.BarColor = v;
                    else if (Slider != null) Slider.BarColor = v;
                    break;
                case UiProperty.BarHighlight:
                    if (Scroll != null) Scroll.BarHighlight = v;
                    else if (Slider != null) Slider.BarHighlight = v;
                    break;
                case UiProperty.SliderColor:
                    if (Scroll != null) Scroll.SliderColor = v;
                    else if (Slider != null) Slider.SliderColor = v;
                    break;
                case UiProperty.SliderHighlight:
                    if (Scroll != null) Scroll.SliderHighlight = v;
                    else if (Slider != null) Slider.SliderHighlight = v;
                    break;
                case UiProperty.DividerColor: if (Scroll != null) Scroll.Divider.Color = v; break;
                case UiProperty.HeaderColor: if (Window != null) Window.header.Color = v; break;
                case UiProperty.BodyColor: if (Window != null) Window.BodyColor = v; break;
                case UiProperty.TickColor: if (Check != null) Check.TickBoxColor = v; break;
            }
        }

        public Color GetColor(int p)
        {
            switch ((UiProperty)p)
            {
                case UiProperty.Color:
                    if (Box != null) return Box.Color;
                    if (Border != null) return Border.Color;
                    if (Panel != null) return Panel.Color;
                    if (LabelBase != null) return LabelBase.Color;
                    if (Scroll != null) return Scroll.Color;
                    if (Window != null) return Window.BodyColor;
                    return Color.Transparent;
                case UiProperty.HighlightColor:
                    if (Button != null) return Button.HighlightColor;
                    if (Panel != null) return Panel.HighlightColor;
                    return Color.Transparent;
                case UiProperty.TextColor: return _style.Color;
                case UiProperty.LinkColor: return _linkColor;
                case UiProperty.HeaderColor: return Window != null ? Window.header.Color : Color.Transparent;
                case UiProperty.BodyColor: return Window != null ? Window.BodyColor : Color.Transparent;
            }
            return Color.Transparent;
        }

        // ---------------------------------------------------------------- string

        public void SetText(int p, string v)
        {
            switch ((UiProperty)p)
            {
                case UiProperty.Text:
                case UiProperty.Title:
                case UiProperty.PlainText:
                    if (Field != null)
                    {
                        Field.SetPlain(v, UiMarkup.Format(_style));
                        break;
                    }
                    bool plain = (UiProperty)p == UiProperty.PlainText;
                    if (_markup == v && _plain == plain)
                        break;
                    _markup = v;
                    _plain = plain;
                    RenderText();
                    break;
                case UiProperty.Font:
                    int font = UiMarkup.FontIndex(v);
                    if (_style.Font != font) { _style.Font = font; RenderText(); }
                    break;
                case UiProperty.Texture:
                    _texture = v;
                    UpdateMaterial();
                    break;
                case UiProperty.Placeholder:
                    if (Field != null)
                    {
                        _placeholder = v;
                        RenderPlaceholder();
                    }
                    break;
                case UiProperty.Tooltip:
                    var m = Mouse;
                    if (m != null)
                        m.ToolTip = string.IsNullOrEmpty(v) ? null : new ToolTip { text = UiMarkup.Parse(v, new UiMarkup.Style { Color = new Color(220, 235, 242), Size = 0.75f, Font = -1 }, _linkColor, null) };
                    break;
            }
        }

        public string GetText(int p)
        {
            switch ((UiProperty)p)
            {
                case UiProperty.Text:
                case UiProperty.Title:
                case UiProperty.PlainText:
                    return Field != null ? Field.Current : _markup;
                case UiProperty.Font:
                    var f = _style.Font >= 0 ? RichHudFramework.UI.Rendering.Client.FontManager.GetFont(_style.Font) : null;
                    return f != null ? f.Name : null;
                case UiProperty.Texture: return _texture;
                case UiProperty.Placeholder: return _placeholder;
            }
            return null;
        }
    }
}
