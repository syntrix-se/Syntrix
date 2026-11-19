// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using VRageMath;

namespace Syntrix.Gui
{
    /// <summary>
    /// Text with markup and word wrapping (MyScreenSpaceText.Draw only cuts at maxWidth, it never wraps).
    /// Markup: [b] bright, [i] quiet, [u] underline, [color=#RRGGBB|name], [size=1.2], [font=Name],
    /// [align=left|center|right] (per paragraph), [link=target] … [/link]; every tag closes with [/tag];
    /// "[[" is a literal "[". Words longer than a line are split.
    /// </summary>
    public class GuiRichText
    {
        private struct Style
        {
            public string Font;
            public float Scale;
            public Color Color;
            public bool Underline;
            public bool Bright;
            public string Link;
        }

        private class Span
        {
            public string Text;
            public Style Style;
            public float X, Width;
        }

        private class Line
        {
            public readonly List<Span> Spans = new List<Span>();
            public float Y, Height, Width;
            public int Align;
        }

        private class Run
        {
            public string Text;
            public Style Style;
            public int Align;
            public bool Break;
        }

        private readonly List<Run> _runs = new List<Run>();
        private readonly List<Line> _lines = new List<Line>();
        private string _source = "";
        private bool _markup = true;
        private string _font = GuiStyle.Font;
        private float _scale = GuiStyle.TextScale;
        private Color _color = GuiStyle.Text;
        private float _laidWidth = -1f, _laidK = -1f;
        private bool _wrap = true;
        private bool _parsed;

        /// <summary>Height of the laid out text in screen pixels.</summary>
        public float Height { get; private set; }

        /// <summary>Width of the widest line in screen pixels.</summary>
        public float Width { get; private set; }

        /// <summary>Text as given.</summary>
        public string Source => _source;

        /// <summary>Hovered link target (set by the owner), drawn underlined.</summary>
        public string HoverLink;

        /// <summary>Sets text and base style; the layout is redone only if something changed.</summary>
        public void Set(string text, bool markup, string font, float scale, Color color, bool wrap)
        {
            text = text ?? "";
            if (text == _source && markup == _markup && font == _font && scale == _scale && color == _color && wrap == _wrap)
                return;
            _source = text;
            _markup = markup;
            _font = font;
            _scale = scale;
            _color = color;
            _wrap = wrap;
            _parsed = false;
            _laidWidth = -1f;
        }

        /// <summary>Escapes "[" so the text shows as written.</summary>
        public static string Escape(string text)
        {
            return string.IsNullOrEmpty(text) ? "" : text.Replace("[", "[[");
        }

        /// <summary>Text without markup (for searching, measuring).</summary>
        public static string Plain(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('[') < 0)
                return text ?? "";
            var sb = new StringBuilder();
            int i = 0;
            while (i < text.Length)
            {
                char c = text[i];
                if (c == '[')
                {
                    if (i + 1 < text.Length && text[i + 1] == '[')
                    {
                        sb.Append('[');
                        i += 2;
                        continue;
                    }
                    int e = text.IndexOf(']', i + 1);
                    if (e > i && IsTag(text.Substring(i + 1, e - i - 1)))
                    {
                        i = e + 1;
                        continue;
                    }
                }
                sb.Append(c);
                i++;
            }
            return sb.ToString();
        }

        private static bool IsTag(string tag)
        {
            var t = tag.TrimStart('/').ToLowerInvariant();
            int eq = t.IndexOf('=');
            if (eq >= 0)
                t = t.Substring(0, eq);
            return t == "b" || t == "i" || t == "u" || t == "color" || t == "size" || t == "font" || t == "align" || t == "link";
        }

        // ---------- parsing ----------

        private void Parse()
        {
            _parsed = true;
            _runs.Clear();
            var stack = new List<Style>();
            var style = new Style { Font = _font, Scale = _scale, Color = _color };
            int align = 0;
            var sb = new StringBuilder();
            string text = _source;
            Action flush = () =>
            {
                if (sb.Length > 0)
                {
                    _runs.Add(new Run { Text = sb.ToString(), Style = style, Align = align });
                    sb.Clear();
                }
            };
            int i = 0;
            while (i < text.Length)
            {
                char c = text[i];
                if (c == '\n')
                {
                    flush();
                    _runs.Add(new Run { Break = true, Align = align, Style = style });
                    i++;
                    continue;
                }
                if (c == '\r' || c == '\t')
                {
                    if (c == '\t')
                        sb.Append("    ");
                    i++;
                    continue;
                }
                if (_markup && c == '[')
                {
                    if (i + 1 < text.Length && text[i + 1] == '[')
                    {
                        sb.Append('[');
                        i += 2;
                        continue;
                    }
                    int e = text.IndexOf(']', i + 1);
                    if (e > i)
                    {
                        var tag = text.Substring(i + 1, e - i - 1);
                        if (IsTag(tag))
                        {
                            flush();
                            ApplyTag(tag, ref style, ref align, stack);
                            i = e + 1;
                            continue;
                        }
                    }
                }
                sb.Append(c);
                i++;
            }
            flush();
        }

        private void ApplyTag(string tag, ref Style style, ref int align, List<Style> stack)
        {
            bool close = tag.StartsWith("/");
            var t = tag.TrimStart('/');
            string name = t, value = null;
            int eq = t.IndexOf('=');
            if (eq >= 0)
            {
                name = t.Substring(0, eq);
                value = t.Substring(eq + 1).Trim().Trim('"');
            }
            name = name.Trim().ToLowerInvariant();
            if (name == "align")
            {
                if (close)
                    align = 0;
                else
                    align = value == "center" || value == "centre" ? 1 : value == "right" ? 2 : 0;
                return;
            }
            if (close)
            {
                if (stack.Count > 0)
                {
                    style = stack[stack.Count - 1];
                    stack.RemoveAt(stack.Count - 1);
                }
                return;
            }
            stack.Add(style);
            switch (name)
            {
                case "b":
                    style.Bright = true;
                    style.Font = _font == GuiStyle.Font ? GuiStyle.FontBright : style.Font;
                    break;
                case "i":
                    style.Color = GuiStyle.TextQuiet;
                    break;
                case "u":
                    style.Underline = true;
                    break;
                case "color":
                    Color c;
                    if (GuiDraw.ParseColor(value, out c))
                        style.Color = c;
                    break;
                case "size":
                    float s;
                    if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out s) && s > 0.1f && s < 5f)
                        style.Scale = _scale * s;
                    break;
                case "font":
                    if (!string.IsNullOrEmpty(value))
                        style.Font = value;
                    break;
                case "link":
                    style.Link = value ?? "";
                    style.Color = GuiStyle.Link;
                    break;
            }
        }

        // ---------- layout ----------

        /// <summary>Lays the text out for a width in screen pixels (no wrapping if width &lt;= 0 or wrapping is off).</summary>
        public void Layout(float width)
        {
            if (!_parsed)
                Parse();
            if (Math.Abs(width - _laidWidth) < 0.5f && _laidK == GuiDraw.K)
                return;
            _laidWidth = width;
            _laidK = GuiDraw.K;
            _lines.Clear();
            bool wrap = _wrap && width > 0f;
            var line = new Line();
            float x = 0f;
            int align = 0;
            Action newLine = () =>
            {
                line.Width = x;
                line.Align = align;
                _lines.Add(line);
                line = new Line();
                x = 0f;
            };
            foreach (var run in _runs)
            {
                align = run.Align;
                if (run.Break)
                {
                    if (line.Spans.Count == 0)
                        line.Height = GuiDraw.LineHeight(run.Style.Font, run.Style.Scale);
                    newLine();
                    continue;
                }
                // Words with the spaces behind them
                int i = 0;
                var text = run.Text;
                while (i < text.Length)
                {
                    int j = i;
                    while (j < text.Length && text[j] != ' ')
                        j++;
                    while (j < text.Length && text[j] == ' ')
                        j++;
                    var word = text.Substring(i, j - i);
                    i = j;
                    float w = GuiDraw.Width(run.Style.Font, word, run.Style.Scale);
                    float wTrim = GuiDraw.Width(run.Style.Font, word.TrimEnd(' '), run.Style.Scale);
                    if (wrap && x > 0f && x + wTrim > width)
                        newLine();
                    if (wrap && wTrim > width)
                    {
                        // Split a word that is longer than the line
                        var rest = word;
                        while (rest.Length > 0)
                        {
                            int n = FitChars(run.Style, rest, Math.Max(1f, width - x));
                            if (n <= 0)
                            {
                                if (x > 0f)
                                {
                                    newLine();
                                    continue;
                                }
                                n = 1;
                            }
                            var part = rest.Substring(0, n);
                            AddSpan(line, part, run.Style, ref x);
                            rest = rest.Substring(n);
                            if (rest.Length > 0)
                                newLine();
                        }
                        continue;
                    }
                    AddSpan(line, word, run.Style, ref x, w);
                }
            }
            if (line.Spans.Count > 0 || _lines.Count == 0)
            {
                if (line.Spans.Count == 0)
                    line.Height = GuiDraw.LineHeight(_font, _scale);
                newLine();
            }
            float y = 0f, max = 0f;
            foreach (var l in _lines)
            {
                if (l.Height <= 0f)
                    l.Height = GuiDraw.LineHeight(_font, _scale);
                l.Y = y;
                y += l.Height;
                // Trailing spaces do not count for alignment
                if (l.Spans.Count > 0)
                {
                    var last = l.Spans[l.Spans.Count - 1];
                    l.Width = last.X + GuiDraw.Width(last.Style.Font, last.Text.TrimEnd(' '), last.Style.Scale);
                }
                max = Math.Max(max, l.Width);
            }
            Height = y;
            Width = max;
        }

        private int FitChars(Style s, string text, float width)
        {
            int lo = 0, hi = text.Length;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (GuiDraw.Width(s.Font, text.Substring(0, mid), s.Scale) <= width)
                    lo = mid;
                else
                    hi = mid - 1;
            }
            return lo;
        }

        private static void AddSpan(Line line, string text, Style style, ref float x, float width = -1f)
        {
            if (width < 0f)
                width = GuiDraw.Width(style.Font, text, style.Scale);
            // Merge with the previous span of the same style (fewer draw calls)
            if (line.Spans.Count > 0)
            {
                var prev = line.Spans[line.Spans.Count - 1];
                if (SameStyle(prev.Style, style))
                {
                    prev.Text += text;
                    prev.Width += width;
                    x += width;
                    return;
                }
            }
            line.Spans.Add(new Span { Text = text, Style = style, X = x, Width = width });
            line.Height = Math.Max(line.Height, GuiDraw.LineHeight(style.Font, style.Scale));
            x += width;
        }

        private static bool SameStyle(Style a, Style b)
        {
            return a.Font == b.Font && a.Scale == b.Scale && a.Color == b.Color && a.Underline == b.Underline && a.Link == b.Link;
        }

        // ---------- drawing and hit testing ----------

        /// <summary>Draws the laid out text with its top left corner at pos (screen pixels) inside a width for alignment.</summary>
        public void Draw(Vector2 pos, float width, float colorFactor = 1f)
        {
            var clip = GuiDraw.Clip;
            bool clipped = clip.Width > 0f || clip.Height > 0f;
            foreach (var l in _lines)
            {
                float y = pos.Y + l.Y;
                if (clipped && (y > clip.Y + clip.Height || y + l.Height < clip.Y))
                    continue;
                float shift = l.Align == 1 ? (width - l.Width) / 2f : l.Align == 2 ? width - l.Width : 0f;
                foreach (var s in l.Spans)
                {
                    var c = s.Style.Color;
                    if (colorFactor < 1f)
                        c = new Color(c.ToVector4() * new Vector4(colorFactor, colorFactor, colorFactor, 1f));
                    bool hover = s.Style.Link != null && s.Style.Link == HoverLink;
                    if (hover)
                        c = Color.White;
                    float sy = y + l.Height - GuiDraw.LineHeight(s.Style.Font, s.Style.Scale);
                    GuiDraw.Text(s.Style.Font, s.Text, new Vector2(pos.X + shift + s.X, sy), s.Style.Scale, c);
                    if (s.Style.Underline || hover)
                    {
                        float w = GuiDraw.Width(s.Style.Font, s.Text.TrimEnd(' '), s.Style.Scale);
                        GuiDraw.Rect(new RectangleF(pos.X + shift + s.X, y + l.Height - Math.Max(1f, 2f * GuiDraw.K), w, Math.Max(1f, GuiDraw.K)), c);
                    }
                }
            }
        }

        /// <summary>Link target at a point (screen pixels, text drawn at pos with width), null if none.</summary>
        public string LinkAt(Vector2 pos, float width, Vector2 p)
        {
            foreach (var l in _lines)
            {
                float y = pos.Y + l.Y;
                if (p.Y < y || p.Y >= y + l.Height)
                    continue;
                float shift = l.Align == 1 ? (width - l.Width) / 2f : l.Align == 2 ? width - l.Width : 0f;
                foreach (var s in l.Spans)
                {
                    if (s.Style.Link == null)
                        continue;
                    float x = pos.X + shift + s.X;
                    if (p.X >= x && p.X < x + s.Width)
                        return s.Style.Link;
                }
            }
            return null;
        }

        /// <summary>True if the text has at least one link.</summary>
        public bool HasLinks
        {
            get
            {
                if (!_parsed)
                    Parse();
                foreach (var r in _runs)
                    if (r.Style.Link != null)
                        return true;
                return false;
            }
        }
    }
}
