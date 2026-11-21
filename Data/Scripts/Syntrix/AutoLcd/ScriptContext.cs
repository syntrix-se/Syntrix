// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Sandbox.ModAPI;

namespace Syntrix.AutoLcd
{
    /// <summary>
    /// Settings of a programmable block that holds the original script (its program text names "Automatic LCDs") or
    /// AutoLCD settings in its custom data: LCD_TAG, SlowMode, SCROLL_LINES, SKIP_CONTENT_TYPE, ENABLE_BOOT, read from
    /// the program text and overridden by the custom data above "---". They count at once, without recompiling.
    /// Screens matching its tag use the block as reference (T:, C:) and its owner for access. While the block really
    /// runs the script (scripts on, block working) its screens are left alone; a block that is switched off freezes
    /// its screens, switching it on again boots them.
    /// </summary>
    internal class ScriptContext
    {
        private static readonly Regex _codeTag = new Regex("LCD_TAG\\s*=\\s*\"([^\"]*)\"");
        private static readonly Regex _codeSlow = new Regex("SlowMode\\s*=\\s*(true|false)", RegexOptions.IgnoreCase);
        private static readonly Regex _codeScroll = new Regex("SCROLL_LINES\\s*=\\s*(\\d+)");
        private static readonly Regex _codeSkip = new Regex("SKIP_CONTENT_TYPE\\s*=\\s*(true|false)", RegexOptions.IgnoreCase);
        private static readonly Regex _codeBoot = new Regex("ENABLE_BOOT\\s*=\\s*(true|false)", RegexOptions.IgnoreCase);
        private static readonly Regex _dataLine = new Regex("^\\s*(LCD_TAG|SlowMode|SCROLL_LINES|SKIP_CONTENT_TYPE|ENABLE_BOOT)\\s*[=:]\\s*(.*?)\\s*;?\\s*$",
            RegexOptions.IgnoreCase | RegexOptions.Multiline);

        private string _program;
        private string _customData;

        private ScriptContext(IMyProgrammableBlock block)
        {
            Block = block;
        }

        public IMyProgrammableBlock Block { get; private set; }

        /// <summary>Screen tag as name filter ("T:[LCD]" is the default of the original).</summary>
        public string Tag { get; private set; }

        public bool SlowMode { get; private set; }

        public int ScrollLines { get; private set; }

        public bool SkipContentType { get; private set; }

        public bool EnableBoot { get; private set; }

        /// <summary>True if the program text is the original script.</summary>
        public bool IsScript { get; private set; }

        /// <summary>True if the real script runs on the block (its screens are not touched).</summary>
        public bool RunsItself
        {
            get { return IsScript && AutoLcdCore.ScriptsEnabled && Block.IsWorking; }
        }

        /// <summary>True if the block is switched off (its screens freeze).</summary>
        public bool SwitchedOff
        {
            get { return !Block.Enabled; }
        }

        /// <summary>Last seen state of SwitchedOff, for the boot after switching on.</summary>
        public bool WasOff;

        /// <summary>Reads the settings of a block; null if the block is no AutoLCD block.</summary>
        public static ScriptContext Read(IMyProgrammableBlock block, ScriptContext previous)
        {
            string program = "";
            try
            {
                program = block.ProgramData ?? "";
            }
            catch (Exception)
            {
                // Program text not readable: only the custom data counts.
            }
            string customData = block.CustomData ?? "";
            if (previous != null && previous._program == program && previous._customData == customData)
                return previous;
            bool hasSeparator;
            string settingsPart = CommandParser.SettingsPart(customData, out hasSeparator);
            bool isScript = program.IndexOf("Automatic LCDs", StringComparison.OrdinalIgnoreCase) >= 0;
            var dataMatches = _dataLine.Matches(hasSeparator ? settingsPart : customData);
            if (!isScript && dataMatches.Count == 0)
                return null;

            var context = previous ?? new ScriptContext(block);
            context._program = program;
            context._customData = customData;
            context.IsScript = isScript;
            var settings = AutoLcdCore.Settings;
            context.Tag = "T:[LCD]";
            context.SlowMode = settings.SlowMode;
            context.ScrollLines = settings.ScrollLines;
            context.SkipContentType = settings.SkipContentType;
            context.EnableBoot = settings.EnableBoot;
            if (isScript)
            {
                var match = _codeTag.Match(program);
                if (match.Success)
                    context.Tag = match.Groups[1].Value;
                match = _codeSlow.Match(program);
                if (match.Success)
                    context.SlowMode = IsTrue(match.Groups[1].Value);
                match = _codeScroll.Match(program);
                int lines;
                if (match.Success && int.TryParse(match.Groups[1].Value, out lines))
                    context.ScrollLines = Math.Max(1, lines);
                match = _codeSkip.Match(program);
                if (match.Success)
                    context.SkipContentType = IsTrue(match.Groups[1].Value);
                match = _codeBoot.Match(program);
                if (match.Success)
                    context.EnableBoot = IsTrue(match.Groups[1].Value);
            }
            foreach (Match match in dataMatches)
            {
                string key = match.Groups[1].Value.ToUpperInvariant();
                string value = match.Groups[2].Value.Trim().Trim('"');
                switch (key)
                {
                    case "LCD_TAG":
                        context.Tag = value;
                        break;
                    case "SLOWMODE":
                        context.SlowMode = IsTrue(value);
                        break;
                    case "SCROLL_LINES":
                        int lines;
                        if (int.TryParse(value, out lines))
                            context.ScrollLines = Math.Max(1, lines);
                        break;
                    case "SKIP_CONTENT_TYPE":
                        context.SkipContentType = IsTrue(value);
                        break;
                    case "ENABLE_BOOT":
                        context.EnableBoot = IsTrue(value);
                        break;
                }
            }
            if (string.IsNullOrWhiteSpace(context.Tag))
                context.Tag = "T:[LCD]";
            return context;
        }

        private static bool IsTrue(string value)
        {
            return string.Equals(value.Trim(), "true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>True if the screen block carries the tag of this block and the owner of this block may read it.</summary>
        public bool Covers(IMyTerminalBlock screenBlock)
        {
            if (Block.Closed || screenBlock.Closed)
                return false;
            if (!screenBlock.HasPlayerAccess(Block.OwnerId))
                return false;
            return TagMatcher.Matches(Tag, screenBlock, Block, Block.OwnerId);
        }
    }

    /// <summary>Tag filters of screens: name filter rules; a tag "[X]" also matches "!X!" (typed without AltGr).</summary>
    internal static class TagMatcher
    {
        private static readonly Dictionary<string, NameFilter[]> _filters = new Dictionary<string, NameFilter[]>();

        public static bool Matches(string tag, IMyTerminalBlock screenBlock, IMyTerminalBlock reference, long owner)
        {
            NameFilter[] filters;
            if (!_filters.TryGetValue(tag, out filters))
            {
                var main = NameFilter.Parse(tag);
                var list = new List<NameFilter> { main };
                if (main.Name != null && !main.Group && main.Name.Length > 2 && main.Name[0] == '[' && main.Name[main.Name.Length - 1] == ']')
                {
                    string prefix = tag.Substring(0, tag.Length - main.Name.Length);
                    list.Add(NameFilter.Parse(prefix + "!" + main.Name.Substring(1, main.Name.Length - 2) + "!"));
                }
                filters = list.ToArray();
                _filters[tag] = filters;
            }
            foreach (var filter in filters)
            {
                if (!filter.Matches(screenBlock, reference))
                    continue;
                if (!filter.Group)
                    return true;
                if (BlockSource.For(reference, owner).GroupBlocks(filter.Name).Contains(screenBlock))
                    return true;
            }
            return false;
        }

        public static void Clear()
        {
            _filters.Clear();
        }
    }
}
