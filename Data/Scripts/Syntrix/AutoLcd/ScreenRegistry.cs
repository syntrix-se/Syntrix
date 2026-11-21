// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;
using IMyTextSurfaceProvider = Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;

namespace Syntrix.AutoLcd
{
    /// <summary>
    /// Server side bookkeeping: all blocks with screens and all programmable blocks of the world (from grid and
    /// block events), which of them carry a screen tag, their screens, link groups, and the fair, budgeted
    /// schedule: each tick the constructs take turns, one small step each, until the time budget is used up.
    /// </summary>
    internal static class ScreenRegistry
    {
        private const double FullScanSeconds = 3;
        private const double PlayerSeconds = 1;
        private const double StartBootSeconds = 60;
        private const double DataLogSeconds = 30;

        private static readonly HashSet<IMyCubeGrid> _grids = new HashSet<IMyCubeGrid>();
        private static readonly HashSet<IMyCubeGrid> _waitingGrids = new HashSet<IMyCubeGrid>();
        private static readonly Dictionary<long, Entry> _entries = new Dictionary<long, Entry>();
        private static readonly Dictionary<long, IMyProgrammableBlock> _programs = new Dictionary<long, IMyProgrammableBlock>();
        private static readonly Dictionary<long, ScriptContext> _contexts = new Dictionary<long, ScriptContext>();
        private static readonly List<long> _scan = new List<long>();
        private static readonly HashSet<long> _dirty = new HashSet<long>();
        private static readonly List<long> _dirtyNow = new List<long>();
        private static readonly Dictionary<long, Construct> _constructs = new Dictionary<long, Construct>();
        private static readonly List<Construct> _order = new List<Construct>();
        private static readonly List<Vector3D> _players = new List<Vector3D>();
        private static readonly List<IMyPlayer> _playerList = new List<IMyPlayer>();
        private static readonly List<IMyCubeGrid> _groupGrids = new List<IMyCubeGrid>();
        private static readonly Dictionary<long, long> _constructOfGrid = new Dictionary<long, long>();
        private static readonly Dictionary<long, double> _dataLogged = new Dictionary<long, double>();
        private static int _scanIndex;
        private static int _orderIndex;
        private static double _nextPlayers;
        private static double _scanStarted;
        private static double _startTime;
        private static bool _relink;
        private static bool _regroup;
        private static bool _running;

        private class Entry
        {
            public IMyTerminalBlock Block;

            public string Data;

            public CommandParser.Result Parsed;

            public readonly List<ManagedScreen> Screens = new List<ManagedScreen>();

            public Action<IMyTerminalBlock> Changed;

            public Action<IMyTerminalBlock> DataChanged;

            /// <summary>CustomDataChanged came since the last detection (for the log line in Detect).</summary>
            public bool DataEvent;
        }

        /// <summary>Screens of one construct and owner, scheduled together.</summary>
        public class Construct
        {
            public long Id;

            public readonly List<ManagedScreen> Screens = new List<ManagedScreen>();

            public int Index;

            public double Milliseconds;

            public string Name = "";
        }

        public static IEnumerable<Construct> Constructs
        {
            get { return _order; }
        }

        public static int ScreenCount { get; private set; }

        public static int BlockCount
        {
            get { return _entries.Count; }
        }

        public static int ProgramCount
        {
            get { return _contexts.Count; }
        }

        // ---------------------------------------------------------------- start and end

        public static void Start()
        {
            // Static fields survive loading another (or the same) world in the same game process, and
            // ElapsedPlayTime starts again from the saved value: forget all times and counters of the last world.
            ResetState();
            _running = true;
            _startTime = AutoLcdCore.Now;
            var entities = new HashSet<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(entities, e => e is IMyCubeGrid);
            foreach (var entity in entities)
                AddGrid(entity);
            MyAPIGateway.Entities.OnEntityAdd += AddGrid;
            MyAPIGateway.Entities.OnEntityRemove += RemoveGrid;
            AutoLcdCore.Log.Info("screen service running, " + _entries.Count + " blocks with screens, " + _programs.Count + " programmable blocks");
        }

        public static void Stop()
        {
            if (_running)
            {
                MyAPIGateway.Entities.OnEntityAdd -= AddGrid;
                MyAPIGateway.Entities.OnEntityRemove -= RemoveGrid;
                foreach (var grid in _grids)
                {
                    grid.OnBlockAdded -= AddBlock;
                    grid.OnBlockRemoved -= RemoveBlock;
                }
                foreach (var grid in _waitingGrids)
                    grid.OnPhysicsChanged -= OnGridPhysics;
                foreach (var entry in _entries.Values)
                    Unhook(entry);
                foreach (var program in _programs.Values)
                    program.CustomDataChanged -= OnProgramChanged;
            }
            _running = false;
            _grids.Clear();
            _waitingGrids.Clear();
            _entries.Clear();
            _programs.Clear();
            _contexts.Clear();
            _scan.Clear();
            _dirty.Clear();
            _constructs.Clear();
            _order.Clear();
            _constructOfGrid.Clear();
            ResetState();
        }

        // Times, indices and scratch lists; the tracked blocks stay.
        private static void ResetState()
        {
            _scanIndex = 0;
            _orderIndex = 0;
            _nextPlayers = 0;
            _scanStarted = double.MinValue;
            _startTime = 0;
            _relink = false;
            _regroup = false;
            ScreenCount = 0;
            _players.Clear();
            _playerList.Clear();
            _dirtyNow.Clear();
            _groupGrids.Clear();
            _dataLogged.Clear();
        }

        /// <summary>The game time went back (loaded save within the session): all due times start anew.</summary>
        public static void ResetTimes()
        {
            _nextPlayers = 0;
            _scanIndex = 0;
            _scanStarted = double.MinValue;
            _startTime = AutoLcdCore.Now;
            _dataLogged.Clear();
            foreach (var entry in _entries.Values)
            {
                _dirty.Add(entry.Block.EntityId);
                foreach (var screen in entry.Screens)
                    screen.ResetTimes();
            }
            foreach (long id in _programs.Keys)
                _dirty.Add(id);
        }

        // ---------------------------------------------------------------- tracking

        private static void AddGrid(IMyEntity entity)
        {
            try
            {
                var grid = entity as IMyCubeGrid;
                if (grid == null || _grids.Contains(grid))
                    return;
                if (grid.Physics == null)
                {
                    // Projections never get physics; they stay waiting until removed.
                    if (_waitingGrids.Add(grid))
                        grid.OnPhysicsChanged += OnGridPhysics;
                    return;
                }
                if (_waitingGrids.Remove(grid))
                    grid.OnPhysicsChanged -= OnGridPhysics;
                _grids.Add(grid);
                grid.OnBlockAdded += AddBlock;
                grid.OnBlockRemoved += RemoveBlock;
                var blocks = new List<IMySlimBlock>();
                grid.GetBlocks(blocks, s => s.FatBlock != null);
                foreach (var slim in blocks)
                    AddBlock(slim);
                Changed();
            }
            catch (Exception e)
            {
                AutoLcdCore.Log.Error("add grid", e);
            }
        }

        private static void RemoveGrid(IMyEntity entity)
        {
            try
            {
                var grid = entity as IMyCubeGrid;
                if (grid != null && _waitingGrids.Remove(grid))
                    grid.OnPhysicsChanged -= OnGridPhysics;
                if (grid == null || !_grids.Remove(grid))
                    return;
                grid.OnBlockAdded -= AddBlock;
                grid.OnBlockRemoved -= RemoveBlock;
                var blocks = new List<IMySlimBlock>();
                grid.GetBlocks(blocks, s => s.FatBlock != null);
                foreach (var slim in blocks)
                    RemoveBlock(slim);
                Changed();
            }
            catch (Exception e)
            {
                AutoLcdCore.Log.Error("remove grid", e);
            }
        }

        private static void OnGridPhysics(IMyEntity entity)
        {
            if (entity.Physics != null && !entity.MarkedForClose)
                AddGrid(entity);
        }

        private static void AddBlock(IMySlimBlock slim)
        {
            var block = slim.FatBlock as IMyTerminalBlock;
            if (block == null)
                return;
            Changed();
            var program = block as IMyProgrammableBlock;
            if (program != null && !_programs.ContainsKey(block.EntityId))
            {
                _programs[block.EntityId] = program;
                program.CustomDataChanged += OnProgramChanged;
                _dirty.Add(block.EntityId);
            }
            if (_entries.ContainsKey(block.EntityId) || !HasScreens(block))
                return;
            var entry = new Entry { Block = block };
            entry.Changed = b => _dirty.Add(b.EntityId);
            entry.DataChanged = b =>
            {
                entry.DataEvent = true;
                _dirty.Add(b.EntityId);
            };
            block.CustomNameChanged += entry.Changed;
            block.CustomDataChanged += entry.DataChanged;
            _entries[block.EntityId] = entry;
            _scan.Add(block.EntityId);
            _dirty.Add(block.EntityId);
        }

        private static void RemoveBlock(IMySlimBlock slim)
        {
            var block = slim.FatBlock as IMyTerminalBlock;
            if (block == null)
                return;
            Changed();
            IMyProgrammableBlock program;
            if (_programs.TryGetValue(block.EntityId, out program))
            {
                program.CustomDataChanged -= OnProgramChanged;
                _programs.Remove(block.EntityId);
                _contexts.Remove(block.EntityId);
            }
            Entry entry;
            if (!_entries.TryGetValue(block.EntityId, out entry))
                return;
            Unhook(entry);
            RemoveScreens(entry);
            _entries.Remove(block.EntityId);
            _scan.Remove(block.EntityId);
        }

        private static void Unhook(Entry entry)
        {
            entry.Block.CustomNameChanged -= entry.Changed;
            entry.Block.CustomDataChanged -= entry.DataChanged;
        }

        // One log line per block and half minute: whether a new Custom Data came by CustomDataChanged or was only
        // found by the scan round (shows in tests whether the event arrives).
        private static void LogData(IMyTerminalBlock block, bool byEvent)
        {
            double now = AutoLcdCore.Now;
            double last;
            if (_dataLogged.TryGetValue(block.EntityId, out last) && now - last < DataLogSeconds && now >= last)
                return;
            _dataLogged[block.EntityId] = now;
            AutoLcdCore.Log.Info("custom data of \"" + block.CustomName + "\" changed, " + (byEvent ? "seen by CustomDataChanged" : "found without CustomDataChanged (scan round or name change)"));
        }

        private static void OnProgramChanged(IMyTerminalBlock block)
        {
            _dirty.Add(block.EntityId);
        }

        private static void Changed()
        {
            BlockSource.WorldChanges++;
            _constructOfGrid.Clear();
        }

        private static bool HasScreens(IMyTerminalBlock block)
        {
            if (block is IMyTextSurface)
                return true;
            var provider = block as IMyTextSurfaceProvider;
            return provider != null && provider.SurfaceCount > 0;
        }

        // ---------------------------------------------------------------- detection

        /// <summary>Marks every block for a new detection (after settings or language changed).</summary>
        public static void DetectAll()
        {
            foreach (long id in _programs.Keys)
                _dirty.Add(id);
            foreach (long id in _entries.Keys)
                _dirty.Add(id);
            foreach (var entry in _entries.Values)
            {
                entry.Data = null;
                foreach (var screen in entry.Screens)
                    screen.Invalidate();
            }
        }

        private static void DetectProgram(IMyProgrammableBlock program)
        {
            if (program.Closed)
                return;
            ScriptContext previous;
            _contexts.TryGetValue(program.EntityId, out previous);
            var context = AutoLcdCore.Settings.ReadProgrammableBlocks ? ScriptContext.Read(program, previous) : null;
            if (context == null)
            {
                if (previous != null)
                {
                    _contexts.Remove(program.EntityId);
                    foreach (long id in _entries.Keys)
                        _dirty.Add(id);
                }
                return;
            }
            if (previous == null)
            {
                _contexts[program.EntityId] = context;
                context.WasOff = context.SwitchedOff;
                foreach (long id in _entries.Keys)
                    _dirty.Add(id);
                return;
            }
            bool off = context.SwitchedOff;
            if (context.WasOff && !off && context.EnableBoot)
                ForScreensOf(context, s => s.Boot());
            context.WasOff = off;
        }

        private static void ForScreensOf(ScriptContext context, Action<ManagedScreen> action)
        {
            foreach (var entry in _entries.Values)
                foreach (var screen in entry.Screens)
                    if (screen.Context == context)
                        action(screen);
        }

        private static void Detect(Entry entry)
        {
            var block = entry.Block;
            if (block.Closed)
                return;
            ScriptContext context = null;
            foreach (var candidate in _contexts.Values)
            {
                if (candidate.Covers(block))
                {
                    context = candidate;
                    break;
                }
            }
            bool tagged = context != null;
            if (!tagged)
            {
                foreach (string tag in AutoLcdCore.Settings.Tags)
                {
                    if (!string.IsNullOrWhiteSpace(tag) && TagMatcher.Matches(tag, block, block, block.OwnerId))
                    {
                        tagged = true;
                        break;
                    }
                }
            }
            if (!tagged || (context != null && context.RunsItself))
            {
                RemoveScreens(entry);
                return;
            }

            string data = block.CustomData ?? "";
            bool parsed = false;
            bool byEvent = entry.DataEvent;
            entry.DataEvent = false;
            if (entry.Parsed == null || entry.Data != data)
            {
                if (entry.Data != null && entry.Data != data)
                    LogData(block, byEvent);
                entry.Data = data;
                entry.Parsed = CommandParser.Parse(data);
                parsed = true;
            }
            var wanted = new List<int>();
            var provider = block as IMyTextSurfaceProvider;
            bool textPanel = block is IMyTextPanel;
            if (textPanel || !entry.Parsed.HasHeaders)
                wanted.Add(0);
            else
            {
                int count = provider != null ? provider.SurfaceCount : 1;
                foreach (int index in entry.Parsed.Screens.Keys)
                    if (index >= 0 && index < count)
                        wanted.Add(index);
            }

            long constructId = ConstructOf(block.CubeGrid);
            string name = block.CustomName ?? "";
            for (int i = entry.Screens.Count - 1; i >= 0; i--)
            {
                if (!wanted.Contains(entry.Screens[i].SurfaceIndex))
                {
                    Forget(entry.Screens[i]);
                    entry.Screens.RemoveAt(i);
                }
            }
            foreach (int index in wanted)
            {
                ManagedScreen screen = null;
                foreach (var existing in entry.Screens)
                    if (existing.SurfaceIndex == index)
                        screen = existing;
                if (screen == null)
                {
                    var surface = block as IMyTextSurface ?? (provider != null ? provider.GetSurface(index) : null);
                    if (surface == null)
                        continue;
                    Construct construct;
                    if (_constructs.TryGetValue(constructId, out construct) && construct.Screens.Count >= AutoLcdCore.Settings.MaxScreensPerConstruct)
                        continue;
                    screen = new ManagedScreen(block, index, surface);
                    entry.Screens.Add(screen);
                    screen.Context = context;
                    if (screen.BootEnabled && AutoLcdCore.Now - _startTime < StartBootSeconds)
                        screen.Boot();
                    parsed = true;
                    _regroup = true;
                }
                if (screen.Context != context)
                {
                    screen.Context = context;
                    screen.Invalidate();
                }
                if (parsed)
                {
                    screen.Commands = entry.Parsed.For(index);
                    screen.Invalidate();
                }
                if (screen.ConstructId != constructId)
                {
                    screen.ConstructId = constructId;
                    _regroup = true;
                }
                ReadSwitches(screen, textPanel ? name : "");
            }
        }

        // Name switches of text panels: !LINK:group n, !MARGIN:n, !NOSCROLL.
        private static void ReadSwitches(ManagedScreen screen, string name)
        {
            string group = null;
            int index = 0, margin = 0;
            int link = name.IndexOf("!LINK:", StringComparison.Ordinal);
            if (link >= 0)
            {
                var parts = name.Substring(link + 6).Split(' ');
                if (parts.Length >= 2 && int.TryParse(parts[1], out index) && parts[0].Length > 0)
                    group = parts[0];
            }
            int marginAt = name.IndexOf("!MARGIN:", StringComparison.Ordinal);
            if (marginAt >= 0)
            {
                string rest = name.Substring(marginAt + 8);
                int end = 0;
                while (end < rest.Length && char.IsDigit(rest[end]))
                    end++;
                int.TryParse(rest.Substring(0, end), out margin);
            }
            bool noScroll = name.IndexOf("!NOSCROLL", StringComparison.Ordinal) >= 0;
            if (screen.LinkGroup != group || screen.LinkIndex != index)
                _relink = true;
            if (screen.Margin != margin)
                screen.Invalidate();
            screen.LinkGroup = group;
            screen.LinkIndex = index;
            screen.Margin = Math.Min(margin, 40);
            screen.NoScroll = noScroll;
        }

        private static void RemoveScreens(Entry entry)
        {
            if (entry.Screens.Count == 0)
                return;
            foreach (var screen in entry.Screens)
                Forget(screen);
            entry.Screens.Clear();
            entry.Parsed = null;
            entry.Data = null;
        }

        private static void Forget(ManagedScreen screen)
        {
            if (screen.LinkGroup != null || screen.Master != null || screen.Linked.Count > 0)
                _relink = true;
            _regroup = true;
        }

        // Id of the mechanical group (construct) of a grid: the smallest grid id in it.
        private static long ConstructOf(IMyCubeGrid grid)
        {
            long id;
            if (_constructOfGrid.TryGetValue(grid.EntityId, out id))
                return id;
            id = grid.EntityId;
            _groupGrids.Clear();
            var group = grid.GetGridGroup(GridLinkTypeEnum.Mechanical);
            if (group != null)
                group.GetGrids(_groupGrids);
            foreach (var member in _groupGrids)
                id = Math.Min(id, member.EntityId);
            foreach (var member in _groupGrids)
                _constructOfGrid[member.EntityId] = id;
            _constructOfGrid[grid.EntityId] = id;
            return id;
        }

        private static void Relink()
        {
            _relink = false;
            var groups = new Dictionary<string, List<ManagedScreen>>();
            foreach (var entry in _entries.Values)
            {
                foreach (var screen in entry.Screens)
                {
                    screen.Master = null;
                    screen.Linked.Clear();
                    if (screen.LinkGroup == null || !(screen.Block is IMyTextPanel))
                        continue;
                    string key = screen.ConstructId + "|" + (screen.Context != null ? screen.Context.Block.EntityId : 0) + "|" + screen.LinkGroup;
                    List<ManagedScreen> list;
                    if (!groups.TryGetValue(key, out list))
                    {
                        list = new List<ManagedScreen>();
                        groups[key] = list;
                    }
                    list.Add(screen);
                }
            }
            foreach (var list in groups.Values)
            {
                list.Sort((a, b) => a.LinkIndex.CompareTo(b.LinkIndex));
                var master = list[0];
                for (int i = 1; i < list.Count; i++)
                {
                    list[i].Master = master;
                    master.Linked.Add(list[i]);
                }
                master.Invalidate();
            }
        }

        private static void Regroup()
        {
            _regroup = false;
            var old = new Dictionary<long, Construct>(_constructs);
            _constructs.Clear();
            _order.Clear();
            int count = 0;
            foreach (var entry in _entries.Values)
            {
                foreach (var screen in entry.Screens)
                {
                    Construct construct;
                    if (!_constructs.TryGetValue(screen.ConstructId, out construct))
                    {
                        if (!old.TryGetValue(screen.ConstructId, out construct))
                            construct = new Construct { Id = screen.ConstructId };
                        construct.Screens.Clear();
                        construct.Index = 0;
                        construct.Name = screen.Block.CubeGrid.CustomName;
                        _constructs[screen.ConstructId] = construct;
                        _order.Add(construct);
                    }
                    construct.Screens.Add(screen);
                    count++;
                }
            }
            ScreenCount = count;
            _orderIndex = 0;
        }

        // ---------------------------------------------------------------- schedule

        /// <summary>One tick of work within the budget.</summary>
        public static void Update(double now, Stopwatch watch, long budgetTicks)
        {
            // A time from before a jump back would hold the player list and the scan round until it is reached again.
            if (_nextPlayers - now > PlayerSeconds)
                _nextPlayers = 0;
            if (_scanStarted > now)
                _scanStarted = double.MinValue;
            if (now >= _nextPlayers)
            {
                _nextPlayers = now + PlayerSeconds;
                Players();
                BlockSource.Sweep();
            }

            // Detection: changed blocks first, then a slow round over all.
            long detectBudget = budgetTicks / 2;
            if (_dirty.Count > 0)
            {
                _dirtyNow.Clear();
                _dirtyNow.AddRange(_dirty);
                foreach (long id in _dirtyNow)
                {
                    if (watch.ElapsedTicks > detectBudget)
                        break;
                    _dirty.Remove(id);
                    IMyProgrammableBlock program;
                    if (_programs.TryGetValue(id, out program))
                        DetectProgram(program);
                    Entry entry;
                    if (_entries.TryGetValue(id, out entry))
                        Detect(entry);
                }
            }
            if (_scanIndex == 0 && now - _scanStarted < FullScanSeconds)
            {
                // Round finished early; wait for the next one.
            }
            else
            {
                if (_scanIndex == 0)
                {
                    _scanStarted = now;
                    foreach (var program in _programs.Values)
                        DetectProgram(program);
                }
                while (_scanIndex < _scan.Count && watch.ElapsedTicks <= detectBudget)
                {
                    Entry entry;
                    if (_entries.TryGetValue(_scan[_scanIndex], out entry))
                        Detect(entry);
                    _scanIndex++;
                }
                if (_scanIndex >= _scan.Count)
                    _scanIndex = 0;
            }
            if (_relink)
                Relink();
            if (_regroup)
                Regroup();

            // Screens: the constructs take turns, one step each.
            int idle = 0;
            while (_order.Count > 0 && idle < _order.Count && watch.ElapsedTicks < budgetTicks)
            {
                if (_orderIndex >= _order.Count)
                    _orderIndex = 0;
                var construct = _order[_orderIndex++];
                long before = watch.ElapsedTicks;
                bool did = StepConstruct(construct, now);
                double ms = (watch.ElapsedTicks - before) * 1000.0 / Stopwatch.Frequency;
                construct.Milliseconds += ms;
                if (did)
                    idle = 0;
                else
                    idle++;
            }
        }

        private static bool StepConstruct(Construct construct, double now)
        {
            int count = construct.Screens.Count;
            for (int k = 0; k < count; k++)
            {
                if (construct.Index >= count)
                    construct.Index = 0;
                var screen = construct.Screens[construct.Index];
                bool frozen = screen.Context != null && (screen.Context.SwitchedOff || screen.Context.RunsItself);
                if (!frozen && !screen.Block.Closed && screen.Step(now, Visible(screen.Block)))
                    return true;
                construct.Index++;
            }
            return false;
        }

        private static void Players()
        {
            _players.Clear();
            _playerList.Clear();
            MyAPIGateway.Players.GetPlayers(_playerList);
            foreach (var player in _playerList)
            {
                var controlled = player.Controller != null ? player.Controller.ControlledEntity : null;
                if (controlled != null && controlled.Entity != null)
                    _players.Add(controlled.Entity.GetPosition());
                else if (player.Character != null)
                    _players.Add(player.Character.GetPosition());
            }
            if (_players.Count == 0 && !MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session.Camera != null)
                _players.Add(MyAPIGateway.Session.Camera.Position);
        }

        private static bool Visible(IMyTerminalBlock block)
        {
            double range = AutoLcdCore.Settings.ViewDistance;
            double rangeSq = range * range;
            Vector3D position = block.GetPosition();
            foreach (var player in _players)
                if (Vector3D.DistanceSquared(player, position) <= rangeSq)
                    return true;
            return false;
        }

        // ---------------------------------------------------------------- lookups for actions, chat and info

        /// <summary>Screens of a block (empty if none).</summary>
        public static List<ManagedScreen> ScreensOf(long entityId)
        {
            Entry entry;
            return _entries.TryGetValue(entityId, out entry) ? entry.Screens : new List<ManagedScreen>();
        }

        /// <summary>Settings of a programmable block, null if it is no AutoLCD block.</summary>
        public static ScriptContext ContextOf(long entityId)
        {
            ScriptContext context;
            return _contexts.TryGetValue(entityId, out context) ? context : null;
        }

        /// <summary>All screens covered by a programmable block.</summary>
        public static List<ManagedScreen> ScreensOfContext(ScriptContext context)
        {
            var list = new List<ManagedScreen>();
            ForScreensOf(context, list.Add);
            return list;
        }

        /// <summary>Screens whose block name contains the text, seen from a reference block and owner.</summary>
        public static List<ManagedScreen> FindScreens(string filterText, IMyTerminalBlock reference, long owner)
        {
            var list = new List<ManagedScreen>();
            var blocks = new List<IMyTerminalBlock>();
            NameFilter.Parse(filterText).Resolve(BlockSource.For(reference, owner), blocks);
            foreach (var block in blocks)
                list.AddRange(ScreensOf(block.EntityId));
            return list;
        }

        public static void ResetStatistics()
        {
            foreach (var construct in _order)
                construct.Milliseconds = 0;
        }
    }
}
