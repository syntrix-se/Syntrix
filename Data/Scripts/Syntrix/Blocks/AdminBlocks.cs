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
    /// Blocks for admins only (API 4). A mod registers block types on every machine; every player machine shows them in
    /// the G menu only while the local player has admin rank and, in survival, creative tools on (Alt+F10). The rank
    /// alone is not enough: in single player and for the host of a listen server the game always gives rank Owner.
    /// GuiVisible hides them in the categories, Public also in the G menu search; both apply to the own machine only. A
    /// machine without a player (dedicated server) keeps Public on; the game checks Public on building nowhere.
    /// Since 1.5 the server no longer razes such blocks; the notice arguments of Register stay for the dock file and
    /// have no effect.
    /// Block variant groups: the game shows only the first block of a group and reaches the others as variants. Such
    /// variant blocks stay hidden for admins too, so the group shows as one entry.
    /// </summary>
    public sealed class AdminBlocks
    {
        private struct Entry
        {
            public string Mod;
            public MyCubeBlockDefinition Definition;
            public bool Variant;
        }

        private readonly SyntrixCore _core;
        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();
        private bool _visible;
        private int _tick;

        /// <summary>Makes the service.</summary>
        public AdminBlocks(SyntrixCore core)
        {
            _core = core;
        }

        /// <summary>Registers a block type "TypeId/SubtypeId" as admin only.</summary>
        /// <param name="mod">Registering mod.</param>
        /// <param name="blockKey">Block type.</param>
        /// <param name="noticeKey">No effect since 1.5 (was the notice when the server razed the block).</param>
        /// <param name="fallback">No effect since 1.5.</param>
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
            definition.Public = !_core.IsPlayer || _visible;
            definition.GuiVisible = _core.IsPlayer && _visible && !variant;
            _entries[blockKey] = new Entry { Mod = mod, Definition = definition, Variant = variant };
            UpdateVisibility(true);
        }

        /// <summary>Removes the rule of a block type (only the mod that registered it); the block is visible again.</summary>
        public void Unregister(string mod, string blockKey)
        {
            Entry old;
            if (!_entries.TryGetValue(blockKey, out old) || old.Mod != mod)
                return;
            old.Definition.Public = true;
            old.Definition.GuiVisible = !old.Variant;
            _entries.Remove(blockKey);
        }

        /// <summary>True if the block type is registered.</summary>
        public bool Has(string blockKey) { return blockKey != null && _entries.ContainsKey(blockKey); }

        /// <summary>Every frame on players: follows admin rank and creative tools (every 60 frames).</summary>
        public void Tick()
        {
            if (_core.IsPlayer && ++_tick % 60 == 0)
                UpdateVisibility(false);
        }

        /// <summary>Clears the registrations and makes the blocks visible again.</summary>
        public void Stop()
        {
            foreach (var e in _entries.Values)
            {
                e.Definition.Public = true;
                e.Definition.GuiVisible = !e.Variant;
            }
            _entries.Clear();
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
            var session = MyAPIGateway.Session;
            // EnableCopyPaste is in survival true only while creative tools are on (MySession.IsCopyPastingEnabled)
            bool admin = session.PromoteLevel >= MyPromoteLevel.Admin && (session.CreativeMode || session.EnableCopyPaste);
            if (admin == _visible && !force)
                return;
            _visible = admin;
            foreach (var e in _entries.Values)
            {
                e.Definition.Public = admin;
                e.Definition.GuiVisible = admin && !e.Variant;
            }
        }
    }
}
