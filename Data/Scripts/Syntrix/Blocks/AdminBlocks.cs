// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System.Collections.Generic;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;

namespace Syntrix.Blocks
{
    /// <summary>
    /// Blocks for admins only (API 4). A mod registers block types on every machine:
    /// - G menu: every player machine shows the blocks only while the local player has admin rank (GuiVisible applies
    ///   to the own machine only; Public stays on so that building does not fail because of it).
    /// - Server: a block of such a type that a player without admin rank starts to build or welds from a projection is
    ///   razed again, the player gets the mod's notice. Blocks placed finished (creative tools, prefabs, pasted grids)
    ///   and blocks of NPCs stay.
    /// - Block variant groups: the game shows only the first block of a group and reaches the others as variants
    ///   (BlockStages). Such variant blocks stay hidden for admins too, so the group shows as one entry.
    /// </summary>
    public sealed class AdminBlocks
    {
        private struct Entry
        {
            public string Mod;
            public string Notice;
            public string Fallback;
            public MyCubeBlockDefinition Definition;
            public bool Variant;
        }

        private readonly SyntrixCore _core;
        private readonly BlockWatch _watch;
        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();
        private readonly List<IMySlimBlock> _raze = new List<IMySlimBlock>();
        private bool _watching, _visible;
        private int _tick;

        /// <summary>Makes the service.</summary>
        public AdminBlocks(SyntrixCore core, BlockWatch watch)
        {
            _core = core;
            _watch = watch;
        }

        /// <summary>Registers a block type "TypeId/SubtypeId" as admin only.</summary>
        /// <param name="mod">Registering mod.</param>
        /// <param name="blockKey">Block type.</param>
        /// <param name="noticeKey">Text key of the notice for a player whose block was razed; null = Syntrix text.</param>
        /// <param name="fallback">Text if the key is missing.</param>
        public void Register(string mod, string blockKey, string noticeKey, string fallback)
        {
            if (string.IsNullOrEmpty(blockKey))
                return;
            MyDefinitionId id;
            MyCubeBlockDefinition definition = null;
            if (!MyDefinitionId.TryParse(blockKey.Contains("MyObjectBuilder_") ? blockKey : "MyObjectBuilder_" + blockKey, out id)
                || !MyDefinitionManager.Static.TryGetCubeBlockDefinition(id, out definition) || definition == null)
            {
                Log.Warn(mod + ": admin block " + blockKey + " has no block definition, ignored.");
                return;
            }
            Entry old;
            if (_entries.TryGetValue(blockKey, out old) && old.Mod != mod)
                Log.Warn("Admin block " + blockKey + " taken over by " + mod + " from " + old.Mod);
            bool variant = IsVariant(definition);
            definition.Public = true;
            definition.GuiVisible = _core.IsPlayer && _visible && !variant;
            _entries[blockKey] = new Entry { Mod = mod, Notice = noticeKey, Fallback = fallback, Definition = definition, Variant = variant };
            if (_core.IsServer && !_watching)
            {
                _watching = true;
                _watch.OnAdded(Added);
            }
            UpdateVisibility(true);
        }

        /// <summary>Removes the rule of a block type (only the mod that registered it); the block is visible again.</summary>
        public void Unregister(string mod, string blockKey)
        {
            Entry old;
            if (!_entries.TryGetValue(blockKey, out old) || old.Mod != mod)
                return;
            old.Definition.GuiVisible = !old.Variant;
            _entries.Remove(blockKey);
        }

        /// <summary>True if the block type is registered.</summary>
        public bool Has(string blockKey) { return blockKey != null && _entries.ContainsKey(blockKey); }

        /// <summary>Every frame: razes queued blocks (server), follows the admin rank (players, every 300 frames).</summary>
        public void Tick()
        {
            if (_raze.Count > 0)
                RazeQueued();
            if (_core.IsPlayer && ++_tick % 300 == 0)
                UpdateVisibility(false);
        }

        /// <summary>Clears the registrations and makes the blocks visible again.</summary>
        public void Stop()
        {
            foreach (var e in _entries.Values)
                e.Definition.GuiVisible = !e.Variant;
            _entries.Clear();
            _raze.Clear();
        }

        /// <summary>True if the block is in a block variant group but is not the group's G menu block; the game hides
        /// such blocks and reaches them as variants (MyBlockVariantGroup.Postprocess).</summary>
        private static bool IsVariant(MyCubeBlockDefinition definition)
        {
            var group = definition.BlockVariantsGroup;
            return group != null && group.PrimaryGUIBlock != null && group.PrimaryGUIBlock != definition;
        }

        private void UpdateVisibility(bool force)
        {
            if (!_core.IsPlayer || MyAPIGateway.Session == null)
                return;
            bool admin = MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Admin;
            if (admin == _visible && !force)
                return;
            _visible = admin;
            foreach (var e in _entries.Values)
                e.Definition.GuiVisible = admin && !e.Variant;
        }

        private void Added(IMySlimBlock block)
        {
            if (_entries.Count == 0 || block.IsFullIntegrity || !_entries.ContainsKey(BlockWatch.KeyOf(block)))
                return;
            _raze.Add(block);   // not inside the grid's add event
        }

        private void RazeQueued()
        {
            var list = _raze.ToArray();
            _raze.Clear();
            foreach (var block in list)
            {
                var grid = block.CubeGrid;
                if (grid == null || grid.Closed || grid.GetCubeBlock(block.Position) != block)
                    continue;
                ulong steamId = BuiltWithoutRank(block);
                if (steamId == 0)
                    continue;
                Entry e;
                _entries.TryGetValue(BlockWatch.KeyOf(block), out e);
                Log.Info(BlockWatch.KeyOf(block) + " built without admin rank by " + steamId + ", razed");
                grid.RazeBlock(block.Position);
                _core.Net.SendNotify(steamId, e.Notice ?? "Syntrix_AdminBlock",
                    e.Fallback ?? "Only admins may build this block.", null, 5000, "Red");
            }
        }

        /// <summary>Steam id of the player without admin rank who builds the block (not finished yet), otherwise 0.</summary>
        private static ulong BuiltWithoutRank(IMySlimBlock block)
        {
            if (block.IsFullIntegrity)
                return 0;
            long builder = block.BuiltBy != 0 ? block.BuiltBy : block.OwnerId;
            if (builder == 0)
                return 0;
            ulong steamId = MyAPIGateway.Players.TryGetSteamId(builder);
            if (steamId == 0)
                return 0;   // NPC or unknown
            return MyAPIGateway.Session.GetUserPromoteLevel(steamId) < MyPromoteLevel.Admin ? steamId : 0;
        }
    }
}
