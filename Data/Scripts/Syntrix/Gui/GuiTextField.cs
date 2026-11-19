// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Input;
using VRageMath;

namespace Syntrix.Gui
{
    /// <summary>
    /// Vanilla text field: click to type, characters from IMyInput.TextInput, Backspace, Delete, arrows, Home, End,
    /// Enter (event Submit; with Multiline a new line on Shift+Enter), Esc or a click elsewhere ends typing.
    /// Event Changed on every change.
    /// </summary>
    public class GuiTextField : GuiElement
    {
        /// <summary>Text; may contain line breaks with Multiline.</summary>
        public string Text = "";
        /// <summary>Hint shown while the field is empty and not focused; null = none.</summary>
        public string Placeholder;
        /// <summary>Maximum number of characters.</summary>
        public int MaxLength = 256;
        /// <summary>Several lines; Enter alone submits, Shift+Enter breaks the line.</summary>
        public bool Multiline;
        /// <summary>The field has the keyboard.</summary>
        public bool Focused;
        private int _caret;
        private float _blink;
        private float _scrollX;
        private readonly List<MyKeys> _keys = new List<MyKeys>();
        private int _repeatKey;
        private int _repeatFrames;

        /// <summary>Creates a text field.</summary>
        public GuiTextField() : base(GuiKind.TextField)
        {
        }

        public override bool Interactive => true;
        public override bool Focusable => true;

        public override Vector2 Measure(float width, float height)
        {
            float line = GuiDraw.LineHeight(EffectiveFont, TextScale);
            int lines = Multiline ? Math.Max(3, Text.Split('\n').Length) : 1;
            return new Vector2(Math.Max(width, 160f * K), Multiline ? lines * line + 16f * K : 40f * K);
        }

        protected override void DrawBackground()
        {
            bool hover = Hovered && IsEnabled;
            if (Focused)
                GuiDraw.Three(GuiStyle.BoxLeftFocus, GuiStyle.BoxCenterFocus, GuiStyle.BoxRightFocus, Rect, Color.White, 8f);
            else if (hover)
                GuiDraw.Three(GuiStyle.BoxLeftHover, GuiStyle.BoxCenterHover, GuiStyle.BoxRightHover, Rect, Color.White, 8f);
            else
                GuiDraw.Three(GuiStyle.BoxLeft, GuiStyle.BoxCenter, GuiStyle.BoxRight, Rect, IsEnabled ? Color.White : new Color(160, 160, 160, 200), 8f);
            if (Color.A > 0)
                GuiDraw.Rect(Rect, Color);
        }

        private RectangleF Inner => new RectangleF(Rect.X + 12f * K, Rect.Y + 4f * K, Math.Max(0f, Rect.Width - 24f * K), Math.Max(0f, Rect.Height - 8f * K));

        protected override void DrawContent()
        {
            var inner = Inner;
            var font = EffectiveFont;
            float line = GuiDraw.LineHeight(font, TextScale);
            using (GuiDraw.PushClip(inner))
            {
                if (Text.Length == 0 && !Focused && !string.IsNullOrEmpty(Placeholder))
                {
                    GuiDraw.TextIn(font, Placeholder, inner, TextScale, GuiStyle.TextQuiet, 0);
                    return;
                }
                if (!Multiline)
                {
                    float caretX = GuiDraw.Width(font, Text.Substring(0, Math.Min(_caret, Text.Length)), TextScale);
                    if (caretX - _scrollX > inner.Width - 4f)
                        _scrollX = caretX - inner.Width + 4f;
                    if (caretX - _scrollX < 0f)
                        _scrollX = Math.Max(0f, caretX - 20f * K);
                    float y = inner.Y + (inner.Height - line) / 2f;
                    GuiDraw.Text(font, Text, new Vector2(inner.X - _scrollX, y), TextScale, IsEnabled ? TextColor : GuiStyle.TextDisabled);
                    if (Focused && (_blink % 1f) < 0.55f)
                        GuiDraw.Rect(new RectangleF(inner.X - _scrollX + caretX, y + 2f * K, Math.Max(1f, 2f * K), line - 4f * K), TextColor);
                }
                else
                {
                    var lines = Text.Split('\n');
                    int pos = 0;
                    float y = inner.Y;
                    for (int i = 0; i < lines.Length; i++)
                    {
                        GuiDraw.Text(font, lines[i], new Vector2(inner.X, y), TextScale, IsEnabled ? TextColor : GuiStyle.TextDisabled);
                        if (Focused && _caret >= pos && _caret <= pos + lines[i].Length && (_blink % 1f) < 0.55f)
                        {
                            float cx = GuiDraw.Width(font, lines[i].Substring(0, _caret - pos), TextScale);
                            GuiDraw.Rect(new RectangleF(inner.X + cx, y + 2f * K, Math.Max(1f, 2f * K), line - 4f * K), TextColor);
                        }
                        pos += lines[i].Length + 1;
                        y += line;
                    }
                }
            }
        }

        public override bool MouseDown(Vector2 p, int button)
        {
            if (!IsEnabled || button != 0)
                return false;
            Focused = true;
            _blink = 0f;
            _caret = CaretAt(p);
            return true;
        }

        private int CaretAt(Vector2 p)
        {
            var inner = Inner;
            var font = EffectiveFont;
            string line = Text;
            int start = 0;
            if (Multiline)
            {
                float lh = GuiDraw.LineHeight(font, TextScale);
                int row = (int)Math.Floor((p.Y - inner.Y) / Math.Max(1f, lh));
                var lines = Text.Split('\n');
                row = MathHelper.Clamp(row, 0, lines.Length - 1);
                for (int i = 0; i < row; i++)
                    start += lines[i].Length + 1;
                line = lines[row];
            }
            float x = p.X - inner.X + (Multiline ? 0f : _scrollX);
            int best = 0;
            for (int i = 1; i <= line.Length; i++)
            {
                if (GuiDraw.Width(font, line.Substring(0, i), TextScale) - x > 0f)
                {
                    float before = GuiDraw.Width(font, line.Substring(0, i - 1), TextScale);
                    float after = GuiDraw.Width(font, line.Substring(0, i), TextScale);
                    best = x - before < after - x ? i - 1 : i;
                    return start + best;
                }
                best = i;
            }
            return start + best;
        }

        public override void Blur()
        {
            if (!Focused)
                return;
            Focused = false;
            Fire(GuiEvent.FocusLost, 0, Text);
        }

        /// <summary>Called every frame while focused.</summary>
        public override void Keyboard()
        {
            var input = MyAPIGateway.Input;
            _blink += 1f / 60f;
            bool changed = false;
            foreach (char c in input.TextInput)
            {
                if (char.IsControl(c))
                    continue;
                if (Text.Length >= MaxLength)
                    break;
                Text = Text.Insert(Math.Min(_caret, Text.Length), c.ToString());
                _caret++;
                changed = true;
            }
            if (KeyPressed(MyKeys.Back) && _caret > 0)
            {
                Text = Text.Remove(_caret - 1, 1);
                _caret--;
                changed = true;
            }
            if (KeyPressed(MyKeys.Delete) && _caret < Text.Length)
            {
                Text = Text.Remove(_caret, 1);
                changed = true;
            }
            if (KeyPressed(MyKeys.Left))
                _caret = Math.Max(0, _caret - 1);
            if (KeyPressed(MyKeys.Right))
                _caret = Math.Min(Text.Length, _caret + 1);
            if (input.IsNewKeyPressed(MyKeys.Home))
                _caret = Multiline ? Text.LastIndexOf('\n', Math.Max(0, _caret - 1)) + 1 : 0;
            if (input.IsNewKeyPressed(MyKeys.End))
            {
                int e = Multiline ? Text.IndexOf('\n', _caret) : -1;
                _caret = e >= 0 ? e : Text.Length;
            }
            if (input.IsNewKeyPressed(MyKeys.Enter))
            {
                if (Multiline && input.IsAnyShiftKeyPressed() && Text.Length < MaxLength)
                {
                    Text = Text.Insert(_caret, "\n");
                    _caret++;
                    changed = true;
                }
                else
                    Fire(GuiEvent.Submit, 0, Text);
            }
            if (changed)
            {
                _blink = 0f;
                Invalidate();
                Fire(GuiEvent.Changed, 0, Text);
            }
        }

        /// <summary>New press or held long enough to repeat.</summary>
        private bool KeyPressed(MyKeys key)
        {
            var input = MyAPIGateway.Input;
            if (input.IsNewKeyPressed(key))
            {
                _repeatKey = (int)key;
                _repeatFrames = 0;
                _blink = 0f;
                return true;
            }
            if (_repeatKey == (int)key && input.IsKeyPress(key))
            {
                _repeatFrames++;
                if (_repeatFrames > 25 && _repeatFrames % 3 == 0)
                    return true;
            }
            else if (_repeatKey == (int)key)
                _repeatKey = 0;
            return false;
        }

        public override bool SetText(int prop, string v)
        {
            switch (prop)
            {
                case GuiProp.Text:
                    Text = v ?? "";
                    if (Text.Length > MaxLength)
                        Text = Text.Substring(0, MaxLength);
                    _caret = Math.Min(_caret, Text.Length);
                    Invalidate();
                    return true;
                case GuiProp.Placeholder:
                    Placeholder = v;
                    return true;
            }
            return base.SetText(prop, v);
        }

        public override bool GetText(int prop, out string v)
        {
            if (prop == GuiProp.Text) { v = Text; return true; }
            if (prop == GuiProp.Placeholder) { v = Placeholder; return true; }
            return base.GetText(prop, out v);
        }

        public override bool SetInt(int prop, int v)
        {
            if (prop == GuiProp.MaxLength) { MaxLength = Math.Max(1, v); return true; }
            return base.SetInt(prop, v);
        }

        public override bool GetInt(int prop, out int v)
        {
            if (prop == GuiProp.MaxLength) { v = MaxLength; return true; }
            return base.GetInt(prop, out v);
        }

        public override bool SetBool(int prop, bool v)
        {
            if (prop == GuiProp.Multiline) { Multiline = v; Invalidate(); return true; }
            if (prop == GuiProp.Focused)
            {
                var w = Root as GuiWindow;
                if (w != null)
                    w.SetFocus(v ? this : null);
                else
                    Focused = v;
                return true;
            }
            return base.SetBool(prop, v);
        }

        public override bool GetBool(int prop, out bool v)
        {
            if (prop == GuiProp.Multiline) { v = Multiline; return true; }
            if (prop == GuiProp.Focused) { v = Focused; return true; }
            return base.GetBool(prop, out v);
        }
    }
}
