// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using IngameBlock = Sandbox.ModAPI.Ingame.IMyTerminalBlock;
using IngameGroup = Sandbox.ModAPI.Ingame.IMyBlockGroup;

namespace Syntrix.AutoLcd
{
    /// <summary>
    /// The blocks a screen may read: the terminal system of the reference grid (grids over connectors, rotors and
    /// pistons, like a programmable block), limited to blocks the owner of the screen has access to (otherwise
    /// CustomData, TextLCD or Details could read foreign blocks). Cached per reference grid and owner; renewed when
    /// blocks were added or removed in the world, at the latest after a few seconds (names, groups, connectors).
    /// Also holds results that several screens share within a short time (e.g. inventory sums).
    /// </summary>
    internal class BlockSource
    {
        private const double MaxAge = 10;
        private const double MinAge = 1;

        private static readonly Dictionary<string, BlockSource> _sources = new Dictionary<string, BlockSource>();
        private static readonly List<string> _drop = new List<string>();

        /// <summary>Counts block additions and removals in the world (set by the screen registry).</summary>
        public static int WorldChanges;

        private readonly List<IMyTerminalBlock> _blocks = new List<IMyTerminalBlock>();
        private readonly Dictionary<string, List<IMyTerminalBlock>> _groups = new Dictionary<string, List<IMyTerminalBlock>>();
        private readonly Dictionary<string, SharedResult> _shared = new Dictionary<string, SharedResult>();
        private readonly List<IngameBlock> _ingame = new List<IngameBlock>();
        private double _built = double.MinValue;
        private double _used;
        private int _builtChanges = -1;
        private Sandbox.ModAPI.Ingame.IMyGridTerminalSystem _system;

        private BlockSource(IMyTerminalBlock reference, long owner)
        {
            Reference = reference;
            Owner = owner;
        }

        /// <summary>Block whose grid and construct count for T: and C:.</summary>
        public IMyTerminalBlock Reference { get; private set; }

        /// <summary>Identity whose access rights count.</summary>
        public long Owner { get; private set; }

        /// <summary>All accessible terminal blocks.</summary>
        public List<IMyTerminalBlock> Blocks
        {
            get
            {
                Refresh();
                return _blocks;
            }
        }

        /// <summary>Source for a reference block and owner (shared by all screens with the same pair).</summary>
        public static BlockSource For(IMyTerminalBlock reference, long owner)
        {
            string key = reference.CubeGrid.EntityId + "/" + reference.EntityId + "/" + owner;
            BlockSource source;
            if (!_sources.TryGetValue(key, out source) || source.Reference.Closed)
            {
                source = new BlockSource(reference, owner);
                _sources[key] = source;
            }
            source._used = AutoLcdCore.Now;
            return source;
        }

        /// <summary>Forgets sources no screen used for a minute.</summary>
        public static void Sweep()
        {
            double now = AutoLcdCore.Now;
            _drop.Clear();
            foreach (var kv in _sources)
                if (now - kv.Value._used > 60 || kv.Value.Reference.Closed)
                    _drop.Add(kv.Key);
            foreach (var key in _drop)
                _sources.Remove(key);
        }

        public static void Clear()
        {
            _sources.Clear();
        }

        /// <summary>Accessible blocks of a group (exact name); empty if there is none.</summary>
        public List<IMyTerminalBlock> GroupBlocks(string name)
        {
            Refresh();
            List<IMyTerminalBlock> list;
            if (_groups.TryGetValue(name ?? "", out list))
                return list;
            list = new List<IMyTerminalBlock>();
            _groups[name ?? ""] = list;
            if (_system == null || name == null)
                return list;
            IngameGroup group = _system.GetBlockGroupWithName(name);
            if (group == null)
                return list;
            _ingame.Clear();
            group.GetBlocks(_ingame);
            foreach (var block in _ingame)
            {
                var terminal = block as IMyTerminalBlock;
                if (terminal != null && MayRead(terminal))
                    list.Add(terminal);
            }
            return list;
        }

        /// <summary>First accessible block with the text in its name (or the group's first block).</summary>
        public IMyTerminalBlock First(string filterText)
        {
            var filter = NameFilter.Parse(filterText);
            var list = filter.Group ? GroupBlocks(filter.Name) : Blocks;
            foreach (var block in list)
                if (!block.Closed && filter.Matches(block, Reference))
                    return block;
            return null;
        }

        /// <summary>A shared result younger than maxAge seconds, or null.</summary>
        public object Shared(string key, double maxAge)
        {
            SharedResult result;
            if (_shared.TryGetValue(key, out result) && AutoLcdCore.Now - result.Time <= maxAge)
                return result.Value;
            return null;
        }

        /// <summary>Stores a shared result.</summary>
        public void Share(string key, object value)
        {
            _shared[key] = new SharedResult { Time = AutoLcdCore.Now, Value = value };
        }

        /// <summary>True if the owner of the screen may read the block (like the ownership check of the block).</summary>
        public bool MayRead(IMyTerminalBlock block)
        {
            return block.HasPlayerAccess(Owner);
        }

        private void Refresh()
        {
            double now = AutoLcdCore.Now;
            double age = now - _built;
            if (age < MinAge || (age < MaxAge && _builtChanges == WorldChanges))
                return;
            _built = now;
            _builtChanges = WorldChanges;
            _blocks.Clear();
            _groups.Clear();
            _shared.Clear();
            _system = null;
            var grid = Reference.CubeGrid as IMyCubeGrid;
            if (grid == null)
                return;
            _system = MyAPIGateway.TerminalActionsHelper.GetTerminalSystemForGrid(grid);
            if (_system == null)
                return;
            _ingame.Clear();
            _system.GetBlocks(_ingame);
            foreach (var block in _ingame)
            {
                var terminal = block as IMyTerminalBlock;
                if (terminal != null && !terminal.Closed && MayRead(terminal))
                    _blocks.Add(terminal);
            }
        }

        private class SharedResult
        {
            public double Time;

            public object Value;
        }
    }
}
