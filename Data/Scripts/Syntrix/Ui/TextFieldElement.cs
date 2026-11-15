// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using RichHudFramework.UI;
using RichHudFramework.UI.Client;
using RichHudFramework.UI.Rendering;
using VRageMath;

namespace Syntrix.Ui
{
    /// <summary>
    /// Text field of the UI service. Rich HUD only takes typing while the chat is open, so the field opens its
    /// input itself: click into it to type, click elsewhere, Enter or Escape to stop. While typing, the keys
    /// do not reach the game, and TextChanged is raised with every change.
    /// </summary>
    public sealed class TextFieldElement : TextField
    {
        private readonly UiNode _node;
        private bool _changed;
        private string _text = "";

        /// <summary>Shown while the field is empty and not typing.</summary>
        public readonly Label Hint;

        /// <summary>0 one line, 1 digits, 2 number (digits, sign, decimal point), 3 anything.</summary>
        public int Filter;

        /// <summary>Longest text; zero = no limit.</summary>
        public int MaxLength;

        /// <summary>Makes the text field of a node.</summary>
        public TextFieldElement(UiNode node) : base(null)
        {
            _node = node;
            // A new Rich HUD text field holds "NewTextField"; start empty
            Text = new RichText("", Format);
            CharFilterFunc = Allowed;
            TextBoard.TextChanged += () => _changed = true;
            Hint = new Label(this)
            {
                ParentAlignment = ParentAlignments.Left | ParentAlignments.Inner,
                AutoResize = true,
                Text = new RichText(""),
            };
            Size = new Vector2(240f, 34f);
            VertCenterText = true;
            TextPadding = new Vector2(10f, 0f);
        }

        /// <summary>Current text as plain string.</summary>
        public string Current { get { return _text; } }

        /// <summary>Sets the text without raising TextChanged.</summary>
        public void SetPlain(string text, GlyphFormat format)
        {
            _text = text ?? "";
            Text = new RichText(_text, format);
            _changed = false;
        }

        private bool Allowed(char c)
        {
            if (c == '\n' || c == '\r')
                return false;
            switch (Filter)
            {
                case 1: return c >= '0' && c <= '9';
                case 2: return (c >= '0' && c <= '9') || c == '-' || c == '.' || c == ',';
                default: return true;
            }
        }

        /// <summary>Starts typing on a click inside, stops on a click outside, Enter (Submit) or Escape.</summary>
        protected override void HandleInput(Vector2 cursorPos)
        {
            base.HandleInput(cursorPos);
            if (SharedBinds.LeftButton.IsNewPressed)
            {
                if (IsMousedOver)
                {
                    if (!InputOpen)
                        OpenInput();
                }
                else if (InputOpen)
                    CloseInput();
            }
            else if (InputOpen && SharedBinds.Enter.IsNewPressed)
            {
                CloseInput();
                Check();
                _node.Fire(UiEvent.Submit, _text);
            }
            else if (InputOpen && SharedBinds.Escape.IsNewPressed)
                CloseInput();

            if (InputOpen)
            {
                // While typing, no key reaches the game (the character would walk off with WASD)
                BindManager.RequestTempBlacklist(SeBlacklistModes.Full);
                UiService.MarkTyping();
            }
            Check();
        }

        private void Check()
        {
            if (!_changed)
                return;
            _changed = false;
            string now = Text.ToString() ?? "";
            if (MaxLength > 0 && now.Length > MaxLength)
            {
                now = now.Substring(0, MaxLength);
                Text = new RichText(now, Format);
                _changed = false;
            }
            if (now == _text)
                return;
            _text = now;
            _node.Fire(UiEvent.TextChanged, now);
        }

        /// <summary>Shows the placeholder while the field is empty and not typed in.</summary>
        protected override void Layout()
        {
            base.Layout();
            Hint.Offset = new Vector2(TextPadding.X, 0f);
            bool show = !InputOpen && _text.Length == 0;
            if (Hint.Visible != show)
                Hint.Visible = show;
        }
    }
}
