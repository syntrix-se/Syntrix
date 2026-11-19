// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Entity.UseObject;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.ModAPI;
using VRage.Utils;

namespace Syntrix.Use
{
    /// <summary>
    /// Own F action for blocks (API 3). A block gets it in its definition: a UseObjectsComponent with
    /// UseObjectTypeOverride "syntrix" (see Examples). A docked mod registers what F does for a block type;
    /// K (terminal) opens the Vanilla terminal of the block as usual.
    /// </summary>
    public sealed class UseService
    {
        /// <summary>The running service; null before start and after Stop.</summary>
        public static UseService Instance { get; private set; }

        private struct Entry
        {
            /// <summary>Mod that registered the action.</summary>
            public string Mod;
            /// <summary>Hint text (language key or text) shown for the F key; null = "Use".</summary>
            public string Hint;
            /// <summary>Action run on F: (block entity id, action number).</summary>
            public Action<long, int> Handler;
        }

        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();

        /// <summary>Creates the service and makes it the instance.</summary>
        public UseService()
        {
            Instance = this;
        }

        /// <summary>Clears all registrations.</summary>
        public void Stop()
        {
            _entries.Clear();
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Registers the F action for a block type "TypeId/SubtypeId" (TypeId without MyObjectBuilder_).</summary>
        public void Register(string mod, string blockKey, string hint, Action<long, int> handler)
        {
            if (string.IsNullOrEmpty(blockKey) || handler == null)
                return;
            Entry old;
            if (_entries.TryGetValue(blockKey, out old) && old.Mod != mod)
                Log.Warn("F action of " + blockKey + " taken over by " + mod + " from " + old.Mod);
            _entries[blockKey] = new Entry { Mod = mod, Hint = hint, Handler = handler };
        }

        /// <summary>Removes the registration of a block type if it belongs to the mod.</summary>
        public void Unregister(string mod, string blockKey)
        {
            Entry old;
            if (_entries.TryGetValue(blockKey, out old) && old.Mod == mod)
                _entries.Remove(blockKey);
        }

        /// <summary>Key "TypeId/SubtypeId" of a cube block (TypeId without MyObjectBuilder_); null if the entity is no block.</summary>
        public static string KeyOf(IMyEntity block)
        {
            var b = block as IMyCubeBlock;
            if (b == null)
                return null;
            var id = b.BlockDefinition;
            return id.TypeId.ToString().Replace("MyObjectBuilder_", "") + "/" + id.SubtypeName;
        }

        /// <summary>True if an F action is registered for the block's type.</summary>
        public bool Has(IMyEntity block)
        {
            var key = KeyOf(block);
            return key != null && _entries.ContainsKey(key);
        }

        /// <summary>Hint text registered for the block's type; null if none.</summary>
        public string Hint(IMyEntity block)
        {
            Entry e;
            var key = KeyOf(block);
            return key != null && _entries.TryGetValue(key, out e) ? e.Hint : null;
        }

        /// <summary>Runs the F action of the block (local player only).</summary>
        public void Use(IMyEntity block, int action)
        {
            Entry e;
            var key = KeyOf(block);
            if (key == null || !_entries.TryGetValue(key, out e))
                return;
            try
            {
                e.Handler(block.EntityId, action);
            }
            catch (Exception x)
            {
                Log.Error("F action of " + key + " (" + e.Mod + ")", x);
            }
        }
    }
}
