// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;

namespace Syntrix.AutoLcd
{
    /// <summary>Details (X, NoN, NoNX), PropBool, PropsBool, PropFloat, PropsFloat.</summary>
    internal static class DetailCommands
    {
        private const int Chunk = 16;

        public static void Register()
        {
            CommandRegistry.Add("Details", CommandRegistry.Normal, Details);
            CommandRegistry.Add("PropBool", CommandRegistry.Normal, c => Bool(c, false));
            CommandRegistry.Add("PropsBool", CommandRegistry.Normal, c => Bool(c, true));
            CommandRegistry.Add("PropFloat", CommandRegistry.Normal, c => Float(c, false));
            CommandRegistry.Add("PropsFloat", CommandRegistry.Normal, c => Float(c, true));
        }

        private static IEnumerable<int> Details(CommandContext context)
        {
            if (context.Line.Args.Count == 0)
                yield break;
            string suffix = context.Suffix;
            bool noNames = suffix.StartsWith("NON", StringComparison.Ordinal);
            if (noNames)
                suffix = suffix.Substring(3);
            bool skipFirst = suffix == "X";
            string search = context.Line.Arg(1, "");
            string endArg = context.Line.Arg(2, "");
            double lineLimit;
            bool byCount = Formatting.TryNumber(endArg, out lineLimit);
            int n = 0;
            foreach (var block in context.Blocks(context.Line.Arg(0)))
            {
                if (++n % Chunk == 0)
                    yield return 0;
                string text = (block.DetailedInfo ?? "").TrimEnd();
                string custom = (block.CustomInfo ?? "").TrimEnd();
                if (custom.Length > 0)
                    text = text.Length > 0 ? text + "\n" + custom : custom;
                var lines = new List<string>(text.Replace("\r", "").Split('\n'));
                if (skipFirst && lines.Count > 0)
                    lines.RemoveAt(0);
                int start = 0;
                if (search.Length > 0)
                {
                    start = -1;
                    for (int i = 0; i < lines.Count; i++)
                    {
                        if (lines[i].IndexOf(search, StringComparison.Ordinal) >= 0)
                        {
                            start = i;
                            break;
                        }
                    }
                    if (start < 0)
                        continue;
                }
                if (!noNames)
                    context.Canvas.Text(block.CustomName);
                int shown = 0;
                for (int i = start; i < lines.Count; i++)
                {
                    if (byCount && shown >= (int)lineLimit)
                        break;
                    if (!byCount && endArg.Length > 0 && i > start && lines[i].IndexOf(endArg, StringComparison.Ordinal) >= 0)
                        break;
                    context.Canvas.Text("  " + lines[i]);
                    shown++;
                }
            }
        }

        private static IEnumerable<int> Bool(CommandContext context, bool many)
        {
            string property = context.Line.Arg(1, "");
            int textArg = many ? 2 : 3;
            string trueText = context.Line.Arg(textArg, context.T("SyxAutoLcd_True", "yes"));
            string falseText = context.Line.Arg(textArg + 1, context.T("SyxAutoLcd_False", "no"));
            int n = 0;
            foreach (var block in Targets(context, many))
            {
                if (++n % Chunk == 0)
                    yield return 0;
                var value = Property<bool>(block, property);
                string text = value == null ? "-" : value.GetValue(block) ? trueText : falseText;
                context.Canvas.Pair(many ? block.CustomName : context.Line.Arg(2, block.CustomName), text);
            }
        }

        private static IEnumerable<int> Float(CommandContext context, bool many)
        {
            string property = context.Line.Arg(1, "");
            string unit = context.Line.Arg(many ? 2 : 3, "");
            int n = 0;
            foreach (var block in Targets(context, many))
            {
                if (++n % Chunk == 0)
                    yield return 0;
                var value = Property<float>(block, property);
                string text = value == null ? "-" : Formatting.Number(value.GetValue(block));
                if (unit.Length > 0)
                    text += " " + unit;
                context.Canvas.Pair(many ? block.CustomName : context.Line.Arg(2, block.CustomName), text);
            }
        }

        private static List<IMyTerminalBlock> Targets(CommandContext context, bool many)
        {
            var blocks = context.Blocks(context.Line.Arg(0, "*"));
            if (!many && blocks.Count > 1)
                blocks.RemoveRange(1, blocks.Count - 1);
            return blocks;
        }

        // A terminal property of the type, also of mods; null if the block has none.
        private static ITerminalProperty<T> Property<T>(IMyTerminalBlock block, string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            try
            {
                var property = block.GetProperty(name);
                return property == null ? null : property.As<T>();
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
