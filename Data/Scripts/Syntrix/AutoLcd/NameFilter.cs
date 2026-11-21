// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

namespace Syntrix.AutoLcd
{
    /// <summary>
    /// Block name filter of the AutoLCD notation: "*" all blocks, a text = blocks with it in the name (case
    /// sensitive), "G:" a block group (exact name), "T:" only the grid of the reference block, "C:" only its
    /// construct (rotors and pistons, not connectors); combinations like "TG:" and "CG:".
    /// </summary>
    internal class NameFilter
    {
        private NameFilter()
        {
        }

        /// <summary>Text to look for (name or group), null = all blocks.</summary>
        public string Name { get; private set; }

        public bool Group { get; private set; }

        public bool SameGrid { get; private set; }

        public bool SameConstruct { get; private set; }

        /// <summary>Original text, used as cache key.</summary>
        public string Text { get; private set; }

        public static NameFilter Parse(string text)
        {
            var filter = new NameFilter { Text = text ?? "*" };
            string name = text ?? "";
            int colon = name.IndexOf(':');
            if (colon > 0 && colon <= 3 && IsModifier(name.Substring(0, colon)))
            {
                string modifiers = name.Substring(0, colon).ToUpperInvariant();
                filter.Group = modifiers.IndexOf('G') >= 0;
                filter.SameGrid = modifiers.IndexOf('T') >= 0;
                filter.SameConstruct = modifiers.IndexOf('C') >= 0;
                name = name.Substring(colon + 1);
            }
            filter.Name = name.Length == 0 || name == "*" ? null : name;
            if (filter.Name == null)
                filter.Group = false;
            return filter;
        }

        private static bool IsModifier(string text)
        {
            foreach (char c in text)
                if ("TCGtcg".IndexOf(c) < 0)
                    return false;
            return true;
        }

        /// <summary>True if the block passes the name or grid part (group membership is checked by the source).</summary>
        public bool Matches(IMyTerminalBlock block, IMyTerminalBlock reference)
        {
            if (SameGrid && (reference == null || block.CubeGrid != reference.CubeGrid))
                return false;
            if (SameConstruct && (reference == null || !block.IsSameConstructAs(reference)))
                return false;
            if (!Group && Name != null && (block.CustomName == null || block.CustomName.IndexOf(Name, StringComparison.Ordinal) < 0))
                return false;
            return true;
        }

        /// <summary>Blocks of the source that pass the filter, optionally only of a type.</summary>
        public void Resolve(BlockSource source, List<IMyTerminalBlock> result, Func<IMyTerminalBlock, bool> extra = null)
        {
            result.Clear();
            var blocks = Group ? source.GroupBlocks(Name) : source.Blocks;
            foreach (var block in blocks)
            {
                if (block.Closed || !Matches(block, source.Reference))
                    continue;
                if (extra != null && !extra(block))
                    continue;
                result.Add(block);
            }
        }
    }
}
