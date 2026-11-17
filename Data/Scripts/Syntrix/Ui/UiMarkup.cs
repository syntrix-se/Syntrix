// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using RichHudFramework.UI.Rendering.Client;
using VRageMath;

namespace Syntrix.Ui
{
    /// <summary>
    /// Small markup language for UI texts, turned into Rich HUD RichText:
    /// [b]bold[/b], [i]italic[/i], [u]underline[/u], [color=#RRGGBB]..[/color] (also #RRGGBBAA or a name),
    /// [size=1.3]..[/size] (absolute text size, 1 = normal), [font=Name]..[/font], [align=left|center|right]..[/align],
    /// [link=target]text[/link] (clickable, raises the Link event with target), "[[" writes a literal "[".
    /// Unknown or broken tags stay as text. Closing a tag also closes the tags opened after it.
    /// </summary>
    public static class UiMarkup
    {
        /// <summary>Base text format of an element.</summary>
        public struct Style
        {
            /// <summary>Text colour.</summary>
            public Color Color;
            /// <summary>Font size (1 = normal).</summary>
            public float Size;
            /// <summary>Font index, -1 = Rich HUD default font.</summary>
            public int Font;
            /// <summary>Bit 1 bold, bit 2 italic, bit 4 underline.</summary>
            public int FontStyle;
            /// <summary>Text alignment.</summary>
            public TextAlignment Align;
        }

        private struct Frame
        {
            /// <summary>Tag name.</summary>
            public string Tag;
            /// <summary>Format before the tag.</summary>
            public Style Before;
            /// <summary>Link target before the tag.</summary>
            public string LinkBefore;
        }

        private static readonly List<Frame> _stack = new List<Frame>();
        private static readonly StringBuilder _run = new StringBuilder();
        private static readonly Dictionary<string, int> _fonts = new Dictionary<string, int>();

        /// <summary>Forgets cached font indices (after a Rich HUD restart).</summary>
        public static void ResetFonts() { _fonts.Clear(); }

        /// <summary>Index of a Rich HUD font by name, -1 if unknown.</summary>
        public static int FontIndex(string name)
        {
            if (string.IsNullOrEmpty(name))
                return -1;
            int index;
            if (_fonts.TryGetValue(name, out index))
                return index;
            var font = FontManager.GetFont(name);
            index = font != null ? font.Index : -1;
            _fonts[name] = index;
            return index;
        }

        /// <summary>Glyph format for a style; styles the font lacks fall back to regular.</summary>
        public static GlyphFormat Format(Style s)
        {
            int fontIndex = s.Font >= 0 ? s.Font : FontManager.Default.X;
            int style = s.FontStyle & 7;
            var font = FontManager.GetFont(fontIndex);
            if (font != null && (style & 3) != 0 && !font.IsStyleDefined((FontStyles)(style & 3)))
                style &= 4;
            return new GlyphFormat(s.Color == default(Color) ? Color.White : s.Color, s.Align, s.Size > 0f ? s.Size : 1f, new Vector2I(fontIndex, style));
        }

        /// <summary>Plain text in one format.</summary>
        public static RichText Plain(string text, Style s)
        {
            return new RichText(text ?? "", Format(s));
        }

        /// <summary>
        /// Parses markup. links (may be null) receives the packed colour of each link run and its target;
        /// links get linkColor with the lowest bits changed per link, so a click finds its target by colour.
        /// </summary>
        public static RichText Parse(string text, Style baseStyle, Color linkColor, Dictionary<uint, string> links)
        {
            var result = new RichText(Format(baseStyle));
            if (links != null)
                links.Clear();
            if (string.IsNullOrEmpty(text))
                return result;

            _stack.Clear();
            _run.Clear();
            Style s = baseStyle;
            string link = null;
            int linkCount = 0;
            Color linkRunColor = linkColor;
            int i = 0;
            while (i < text.Length)
            {
                char ch = text[i];
                if (ch != '[')
                {
                    _run.Append(ch);
                    i++;
                    continue;
                }
                if (i + 1 < text.Length && text[i + 1] == '[')
                {
                    _run.Append('[');
                    i += 2;
                    continue;
                }
                int end = text.IndexOf(']', i + 1);
                if (end < 0)
                {
                    _run.Append(ch);
                    i++;
                    continue;
                }
                string tag = text.Substring(i + 1, end - i - 1);
                Style next = s;
                string nextLink = link;
                if (!Apply(tag, ref next, ref nextLink))
                {
                    _run.Append(ch);
                    i++;
                    continue;
                }
                Flush(result, s, link, linkRunColor);
                if (nextLink != null && nextLink != link)
                {
                    linkCount++;
                    linkRunColor = LinkColor(linkColor, linkCount);
                    if (links != null)
                        links[linkRunColor.PackedValue] = nextLink;
                }
                s = next;
                link = nextLink;
                i = end + 1;
            }
            Flush(result, s, link, linkRunColor);
            _stack.Clear();
            return result;
        }

        private static void Flush(RichText result, Style s, string link, Color linkRunColor)
        {
            if (_run.Length == 0)
                return;
            if (link != null)
                s.Color = linkRunColor;
            result.Add(_run.ToString(), Format(s));
            _run.Clear();
        }

        private static Color LinkColor(Color c, int k)
        {
            k %= 64;
            if (k == 0)
                k = 1;
            return new Color(Nudge(c.R, k % 4), Nudge(c.G, (k / 4) % 4), Nudge(c.B, (k / 16) % 4), c.A);
        }

        private static int Nudge(byte v, int d) { return v >= 4 ? v - d : v + d; }

        /// <summary>Applies one tag (without brackets) to the style; false if it is no known tag.</summary>
        private static bool Apply(string tag, ref Style s, ref string link)
        {
            if (tag.Length == 0)
                return false;
            if (tag[0] == '/')
            {
                string name = tag.Substring(1).Trim().ToLowerInvariant();
                for (int k = _stack.Count - 1; k >= 0; k--)
                {
                    if (_stack[k].Tag != name)
                        continue;
                    s = _stack[k].Before;
                    link = _stack[k].LinkBefore;
                    _stack.RemoveRange(k, _stack.Count - k);
                    return true;
                }
                return false;
            }

            string key = tag, value = null;
            int eq = tag.IndexOf('=');
            if (eq >= 0)
            {
                key = tag.Substring(0, eq);
                value = tag.Substring(eq + 1).Trim();
                if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
                    value = value.Substring(1, value.Length - 2);
            }
            key = key.Trim().ToLowerInvariant();
            var before = s;
            var linkBefore = link;
            switch (key)
            {
                case "b": s.FontStyle |= 1; break;
                case "i": s.FontStyle |= 2; break;
                case "u": s.FontStyle |= 4; break;
                case "color":
                    Color c;
                    if (!TryColor(value, out c))
                        return false;
                    s.Color = c;
                    break;
                case "size":
                    float f;
                    if (value == null || !float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out f) || f <= 0f || f > 20f)
                        return false;
                    s.Size = f;
                    break;
                case "font":
                    int font = FontIndex(value);
                    if (font < 0)
                        return false;
                    s.Font = font;
                    break;
                case "align":
                    if (value == "left") s.Align = TextAlignment.Left;
                    else if (value == "center") s.Align = TextAlignment.Center;
                    else if (value == "right") s.Align = TextAlignment.Right;
                    else return false;
                    break;
                case "link":
                    if (string.IsNullOrEmpty(value))
                        return false;
                    link = value;
                    break;
                default:
                    return false;
            }
            _stack.Add(new Frame { Tag = key, Before = before, LinkBefore = linkBefore });
            return true;
        }

        /// <summary>Colour from #RRGGBB, #RRGGBBAA or a name (white, black, gray, red, green, blue, yellow, cyan, orange, magenta).</summary>
        public static bool TryColor(string v, out Color c)
        {
            c = Color.White;
            if (string.IsNullOrEmpty(v))
                return false;
            if (v[0] == '#')
            {
                uint x;
                string hex = v.Substring(1);
                if ((hex.Length != 6 && hex.Length != 8) || !uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out x))
                    return false;
                if (hex.Length == 6)
                    c = new Color((int)((x >> 16) & 255), (int)((x >> 8) & 255), (int)(x & 255));
                else
                    c = new Color((int)((x >> 24) & 255), (int)((x >> 16) & 255), (int)((x >> 8) & 255), (int)(x & 255));
                return true;
            }
            switch (v.ToLowerInvariant())
            {
                case "white": c = Color.White; return true;
                case "black": c = Color.Black; return true;
                case "gray": case "grey": c = Color.Gray; return true;
                case "red": c = Color.Red; return true;
                case "green": c = Color.Lime; return true;
                case "blue": c = Color.DodgerBlue; return true;
                case "yellow": c = Color.Yellow; return true;
                case "cyan": c = Color.Cyan; return true;
                case "orange": c = Color.Orange; return true;
                case "magenta": c = Color.Magenta; return true;
            }
            return false;
        }
    }
}
