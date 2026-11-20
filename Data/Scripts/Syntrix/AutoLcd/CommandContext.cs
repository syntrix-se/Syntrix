// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

namespace Syntrix.AutoLcd
{
    /// <summary>What a command sees while it runs: its line, the screen, the canvas and the readable blocks.</summary>
    internal class CommandContext
    {
        private readonly List<List<IMyTerminalBlock>> _lists = new List<List<IMyTerminalBlock>>();
        private int _listsUsed;

        public CommandContext(ManagedScreen screen, BlockSource source, AutoLcdSettings settings)
        {
            Screen = screen;
            Source = source;
            Settings = settings;
        }

        public ManagedScreen Screen { get; private set; }

        public BlockSource Source { get; private set; }

        public AutoLcdSettings Settings { get; private set; }

        public LineCanvas Canvas { get; private set; }

        public CommandLine Line { get; private set; }

        /// <summary>Variant text after the base name, upper case ("X", "NNBX" ...).</summary>
        public string Suffix { get; private set; }

        /// <summary>Block that carries the screen.</summary>
        public IMyTerminalBlock ScreenBlock
        {
            get { return Screen.Block; }
        }

        /// <summary>Block for T:, C:, positions and ship values (programmable block or screen block).</summary>
        public IMyTerminalBlock Reference
        {
            get { return Source.Reference; }
        }

        public double Now
        {
            get { return AutoLcdCore.Now; }
        }

        /// <summary>Starts a command with a fresh canvas.</summary>
        public void Begin(CommandLine line, LineCanvas canvas)
        {
            Line = line;
            Canvas = canvas;
            Suffix = (line.Suffix ?? "").ToUpperInvariant();
            _listsUsed = 0;
        }

        /// <summary>Blocks of a name filter argument (default "*"), optionally only matching blocks.</summary>
        public List<IMyTerminalBlock> Blocks(string filterArg, Func<IMyTerminalBlock, bool> extra = null)
        {
            List<IMyTerminalBlock> list;
            if (_listsUsed < _lists.Count)
                list = _lists[_listsUsed];
            else
            {
                list = new List<IMyTerminalBlock>();
                _lists.Add(list);
            }
            _listsUsed++;
            NameFilter.Parse(string.IsNullOrEmpty(filterArg) ? "*" : filterArg).Resolve(Source, list, extra);
            return list;
        }

        /// <summary>True if the suffix is exactly one of the variants.</summary>
        public bool SuffixIs(params string[] variants)
        {
            foreach (string variant in variants)
                if (Suffix == variant.ToUpperInvariant())
                    return true;
            return false;
        }

        /// <summary>Screen text in the screen language.</summary>
        public string T(string key, string fallback)
        {
            return LcdText.Get(key, fallback);
        }

        /// <summary>Stat mode from the usual suffixes X, P, V, Bar.</summary>
        public StatMode Mode(string suffix)
        {
            switch ((suffix ?? "").ToUpperInvariant())
            {
                case "X":
                    return StatMode.Percent;
                case "P":
                    return StatMode.PercentBar;
                case "V":
                    return StatMode.Values;
                case "BAR":
                    return StatMode.Bar;
                default:
                    return StatMode.Full;
            }
        }
    }
}
