// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Syntrix.AutoLcd
{
    /// <summary>
    /// Reads the commands from a custom data text, in the AutoLCD notation:
    /// only the part below a line "---" when there is one (the part above belongs to other scripts);
    /// one command per line or several separated by ";"; "\" or "&amp;" at the end of a line continues it;
    /// lines starting with "//" are comments; "@n AutoLCD" or "#n AutoLCD" picks screen n of a block.
    /// </summary>
    internal static class CommandParser
    {
        private static readonly Regex _header = new Regex("^[@#](\\d+)\\s+AutoLCD\\s*$", RegexOptions.IgnoreCase);

        /// <summary>Commands of one custom data text.</summary>
        public class Result
        {
            /// <summary>Commands per screen index; key -1 = text without a screen header.</summary>
            public readonly Dictionary<int, List<CommandLine>> Screens = new Dictionary<int, List<CommandLine>>();

            /// <summary>True if the text has at least one screen header.</summary>
            public bool HasHeaders;

            /// <summary>Commands for screen index (header or, for screen 0, text without headers); null if none.</summary>
            public List<CommandLine> For(int index)
            {
                List<CommandLine> lines;
                if (Screens.TryGetValue(index, out lines))
                    return lines;
                if (index == 0 && !HasHeaders && Screens.TryGetValue(-1, out lines))
                    return lines;
                return null;
            }
        }

        /// <summary>Settings part above "---" (programmable block settings), or the whole text without it.</summary>
        public static string SettingsPart(string customData, out bool hasSeparator)
        {
            hasSeparator = false;
            if (string.IsNullOrEmpty(customData))
                return "";
            int index = FindSeparator(customData);
            if (index < 0)
                return customData;
            hasSeparator = true;
            return customData.Substring(0, index);
        }

        /// <summary>Parses a custom data text.</summary>
        public static Result Parse(string customData)
        {
            var result = new Result();
            if (string.IsNullOrEmpty(customData))
                return result;
            string text = customData;
            int separator = FindSeparator(text);
            if (separator >= 0)
            {
                int lineEnd = text.IndexOf('\n', separator);
                text = lineEnd < 0 ? "" : text.Substring(lineEnd + 1);
            }
            string[] rawLines = text.Replace("\r", "").Split('\n');
            int screen = -1;
            var joined = new StringBuilder();
            int joinedStart = 0;
            for (int i = 0; i < rawLines.Length; i++)
            {
                string line = rawLines[i];
                if (joined.Length == 0)
                    joinedStart = i + 1;
                if (line.Length > 0 && (line[line.Length - 1] == '\\' || line[line.Length - 1] == '&'))
                {
                    joined.Append(line, 0, line.Length - 1);
                    continue;
                }
                joined.Append(line);
                string full = joined.ToString();
                joined.Clear();
                string trimmed = full.Trim();
                if (trimmed.Length == 0 || trimmed.StartsWith("//", StringComparison.Ordinal))
                    continue;
                var match = _header.Match(trimmed);
                if (match.Success)
                {
                    int index;
                    if (int.TryParse(match.Groups[1].Value, out index))
                    {
                        screen = index;
                        result.HasHeaders = true;
                        if (!result.Screens.ContainsKey(index))
                            result.Screens[index] = new List<CommandLine>();
                    }
                    continue;
                }
                List<CommandLine> target;
                if (!result.Screens.TryGetValue(screen, out target))
                {
                    target = new List<CommandLine>();
                    result.Screens[screen] = target;
                }
                foreach (string part in SplitCommands(full))
                {
                    string command = part.TrimStart(' ', '\t');
                    if (command.Length == 0)
                        continue;
                    int space = command.IndexOf(' ');
                    string word = space < 0 ? command : command.Substring(0, space);
                    string rest = space < 0 ? "" : command.Substring(space + 1);
                    target.Add(new CommandLine(word, rest, joinedStart));
                }
            }
            return result;
        }

        private static int FindSeparator(string text)
        {
            int start = 0;
            while (start <= text.Length)
            {
                int end = text.IndexOf('\n', start);
                string line = end < 0 ? text.Substring(start) : text.Substring(start, end - start);
                if (line.Trim() == "---")
                    return start;
                if (end < 0)
                    break;
                start = end + 1;
            }
            return -1;
        }

        /// <summary>Splits a line at ";" outside of {…} and "…".</summary>
        private static List<string> SplitCommands(string line)
        {
            var parts = new List<string>();
            var current = new StringBuilder();
            char closing = '\0';
            foreach (char c in line)
            {
                if (closing != '\0')
                {
                    if (c == closing)
                        closing = '\0';
                    current.Append(c);
                    continue;
                }
                if (c == '{')
                    closing = '}';
                else if (c == '"')
                    closing = '"';
                if (c == ';')
                {
                    parts.Add(current.ToString());
                    current.Clear();
                    continue;
                }
                current.Append(c);
            }
            parts.Add(current.ToString());
            return parts;
        }
    }
}
