// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System.Text;
using Syntrix.Gui;

namespace Syntrix.Wiki
{
    /// <summary>
    /// Turns wiki text into the markup of the window service: [[target|text]] becomes a link, **text** bright,
    /// "•" at the start of a line in the accent colour; a literal "[" is escaped.
    /// </summary>
    public static class WikiMarkup
    {
        private static readonly string Accent = GuiDraw.Hex(GuiStyle.Accent);

        /// <summary>Wiki text as markup. Links to unknown pages stay plain text.</summary>
        public static string ToGui(string text, WikiData data, bool german)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            var sb = new StringBuilder(text.Length + 32);
            bool bold = false, lineStart = true;
            int pos = 0;
            foreach (var link in WikiData.LinksIn(text))
            {
                Plain(sb, text, pos, link.Start, ref bold, ref lineStart);
                WikiPage target = null;
                bool known = data != null && link.Target.Length > 0 && data.Pages.TryGetValue(link.Target, out target);
                var display = link.Display ?? (known ? target.Title(german) : link.Target);
                if (known)
                    sb.Append("[link=").Append(target.Id).Append(']');
                Escape(sb, display);
                if (known)
                    sb.Append("[/link]");
                lineStart = false;
                pos = link.Start + link.Length;
            }
            Plain(sb, text, pos, text.Length, ref bold, ref lineStart);
            if (bold)
                sb.Append("[/b]");
            return sb.ToString();
        }

        /// <summary>Plain text without markup (escapes "[").</summary>
        public static string Escape(string text)
        {
            var sb = new StringBuilder();
            Escape(sb, text);
            return sb.ToString();
        }

        private static void Escape(StringBuilder sb, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;
            foreach (var c in text)
            {
                if (c == '[')
                    sb.Append("[[");
                else
                    sb.Append(c);
            }
        }

        private static void Plain(StringBuilder sb, string text, int start, int end, ref bool bold, ref bool lineStart)
        {
            for (int i = start; i < end; i++)
            {
                char c = text[i];
                if (c == '\n')
                {
                    sb.Append(c);
                    lineStart = true;
                    continue;
                }
                if (lineStart && c == '•')
                {
                    sb.Append("[color=").Append(Accent).Append("]•[/color]");
                    lineStart = false;
                    continue;
                }
                if (c == '*' && i + 1 < end && text[i + 1] == '*')
                {
                    sb.Append(bold ? "[/b]" : "[b]");
                    bold = !bold;
                    i++;
                    continue;
                }
                if (c == '[')
                    sb.Append("[[");
                else
                    sb.Append(c);
                if (c != ' ')
                    lineStart = false;
            }
        }
    }
}
