// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System.Collections.Generic;
using System.Text;

namespace Syntrix.AutoLcd
{
    /// <summary>
    /// One command of a screen: the command word, its arguments (words; {…} or "…" keep spaces) and the raw text
    /// after the word (for Echo, Time and the like). Holds the cached output and state of the command between updates.
    /// </summary>
    internal class CommandLine
    {
        public CommandLine(string word, string rest, int sourceLine)
        {
            Word = word;
            Rest = rest ?? "";
            SourceLine = sourceLine;
            Args = Split(Rest);
        }

        /// <summary>Command word as written, e.g. "InventoryX".</summary>
        public string Word { get; private set; }

        /// <summary>Text after the command word and one space, unchanged.</summary>
        public string Rest { get; private set; }

        /// <summary>Arguments.</summary>
        public List<string> Args { get; private set; }

        /// <summary>Line number in the custom data (1-based), for error messages.</summary>
        public int SourceLine { get; private set; }

        /// <summary>Resolved handler (set on first run).</summary>
        public CommandRegistry.Entry Handler;

        /// <summary>Variant text after the base name, e.g. "X" of "InventoryX".</summary>
        public string Suffix = "";

        /// <summary>Output of the last run, reused until the command is due again.</summary>
        public readonly List<string> Output = new List<string>();

        /// <summary>Game time of the last run.</summary>
        public double LastRun = double.MinValue;

        /// <summary>State a command keeps between runs (speed history, charge rates ...).</summary>
        public object State;

        /// <summary>Argument by index, or the default.</summary>
        public string Arg(int index, string def = null)
        {
            return index < Args.Count ? Args[index] : def;
        }

        /// <summary>Arguments from index on, joined with spaces.</summary>
        public string ArgsFrom(int index)
        {
            if (index >= Args.Count)
                return "";
            var text = new StringBuilder();
            for (int i = index; i < Args.Count; i++)
            {
                if (i > index)
                    text.Append(' ');
                text.Append(Args[i]);
            }
            return text.ToString();
        }

        /// <summary>Splits arguments at spaces; {…} and "…" group words (both forms stay valid).</summary>
        public static List<string> Split(string text)
        {
            var args = new List<string>();
            var current = new StringBuilder();
            char closing = '\0';
            bool hasToken = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (closing != '\0')
                {
                    if (c == closing)
                        closing = '\0';
                    else
                        current.Append(c);
                    continue;
                }
                if ((c == '{' || c == '"') && !hasToken)
                {
                    closing = c == '{' ? '}' : '"';
                    hasToken = true;
                    continue;
                }
                if (c == ' ' || c == '\t')
                {
                    if (hasToken)
                    {
                        args.Add(current.ToString());
                        current.Clear();
                        hasToken = false;
                    }
                    continue;
                }
                current.Append(c);
                hasToken = true;
            }
            if (hasToken)
                args.Add(current.ToString());
            return args;
        }
    }
}
