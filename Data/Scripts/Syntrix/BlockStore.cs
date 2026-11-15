// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.ModAPI;

namespace Syntrix
{
    /// <summary>
    /// Text values per block, per mod and key. Saved in the block's mod storage (travels with
    /// world and blueprint), changed only by the server, mirrored to every player.
    /// A player's change is a request: the server checks access to the block first.
    /// </summary>
    public class BlockStore
    {
        /// <summary>Storage GUID, registered in Data/EntityComponents_Syntrix.sbc.</summary>
        public static readonly Guid StorageGuid = new Guid("583FC123-E62D-4269-A84C-14B32976D720");

        const char FieldSep = '\u001F';
        const char EntrySep = '\u001E';

        readonly SyntrixCore _core;
        readonly Dictionary<long, Dictionary<string, string>> _cache = new Dictionary<long, Dictionary<string, string>>();
        readonly HashSet<long> _syncRequested = new HashSet<long>();
        readonly Dictionary<string, List<Action<long, string, string>>> _subs = new Dictionary<string, List<Action<long, string, string>>>();

        public BlockStore(SyntrixCore core) { _core = core; }

        public void Start() { MyAPIGateway.Entities.OnEntityRemove += OnEntityRemove; }

        public void Stop()
        {
            MyAPIGateway.Entities.OnEntityRemove -= OnEntityRemove;
            _cache.Clear();
            _subs.Clear();
        }

        void OnEntityRemove(IMyEntity e)
        {
            if (e == null)
                return;
            _cache.Remove(e.EntityId);
            _syncRequested.Remove(e.EntityId);
        }

        static string Key(string mod, string key) { return mod + FieldSep + key; }

        // ---------- reading ----------

        public string Get(long entityId, string mod, string key)
        {
            if (mod == null || key == null)
                return null;
            var values = Values(entityId, true);
            string v;
            return values != null && values.TryGetValue(Key(mod, key), out v) ? v : null;
        }

        Dictionary<string, string> Values(long entityId, bool requestSync)
        {
            Dictionary<string, string> values;
            if (_cache.TryGetValue(entityId, out values))
                return values;

            var entity = MyAPIGateway.Entities.GetEntityById(entityId);
            if (!(entity is IMyTerminalBlock))
                return null;

            values = Parse(ReadStorage(entity));
            _cache[entityId] = values;
            // Players got the storage with the block when they joined; it may be outdated.
            if (requestSync && !_core.IsServer && _syncRequested.Add(entityId))
                _core.Net.ToServer(new Packet { Kind = PacketKind.BlockSyncRequest, EntityId = entityId });
            return values;
        }

        public void RequestSync(long entityId)
        {
            if (_core.IsServer)
                return;
            _syncRequested.Add(entityId);
            _core.Net.ToServer(new Packet { Kind = PacketKind.BlockSyncRequest, EntityId = entityId });
        }

        // ---------- writing ----------

        public void Set(long entityId, string mod, string key, string value)
        {
            if (mod == null || key == null)
                return;
            if (value != null && value.Length > Protocol.MaxValueLength)
                value = value.Substring(0, Protocol.MaxValueLength);

            if (_core.IsServer)
            {
                Apply(entityId, mod, key, value);
                return;
            }
            // Player: show the new value at once, the server answers with the real state.
            var values = Values(entityId, false);
            if (values != null)
                Put(values, Key(mod, key), value);
            _core.Net.ToServer(new Packet { Kind = PacketKind.BlockSet, EntityId = entityId, Texts = new[] { mod, key, value ?? "" } });
        }

        static string Clean(string s)
        {
            if (s == null || (s.IndexOf(FieldSep) < 0 && s.IndexOf(EntrySep) < 0))
                return s;
            return s.Replace(FieldSep, ' ').Replace(EntrySep, ' ');
        }

        void Apply(long entityId, string mod, string key, string value)
        {
            mod = Clean(mod);
            key = Clean(key);
            value = Clean(value);
            if (string.IsNullOrEmpty(mod) || string.IsNullOrEmpty(key))
                return;
            var entity = MyAPIGateway.Entities.GetEntityById(entityId);
            if (!(entity is IMyTerminalBlock))
                return;
            var values = Values(entityId, false);
            if (values == null)
                return;
            string k = Key(mod, key);
            string old;
            values.TryGetValue(k, out old);
            if (old == value || (string.IsNullOrEmpty(value) && old == null))
                return;
            if (!values.ContainsKey(k) && values.Count >= Protocol.MaxValuesPerBlock)
            {
                Log.Warn("block " + entityId + " has too many values, '" + k + "' not saved.");
                return;
            }
            Put(values, k, value);
            WriteStorage(entity, values);
            Raise(entityId, mod, key, value);
            _core.Net.ToAll(new Packet { Kind = PacketKind.BlockValues, EntityId = entityId, Texts = Flatten(values) });
        }

        static void Put(Dictionary<string, string> values, string k, string value)
        {
            if (string.IsNullOrEmpty(value))
                values.Remove(k);
            else
                values[k] = value;
        }

        // ---------- network ----------

        public void OnPacket(Packet p, ulong sender)
        {
            switch (p.Kind)
            {
                case PacketKind.BlockSet:
                    if (p.Texts == null || p.Texts.Length != 3)
                        return;
                    if (!HasAccess(p.EntityId, sender))
                    {
                        // Send the real state back so the player's display is corrected.
                        SendValues(p.EntityId, sender);
                        return;
                    }
                    Apply(p.EntityId, p.Texts[0], p.Texts[1], p.Texts[2]);
                    break;
                case PacketKind.BlockSyncRequest:
                    SendValues(p.EntityId, sender);
                    break;
                case PacketKind.BlockValues:
                    if (_core.IsServer)
                        return; // the server's own copy is already current
                    var fresh = Unflatten(p.Texts);
                    Dictionary<string, string> old;
                    _cache.TryGetValue(p.EntityId, out old);
                    _cache[p.EntityId] = fresh;
                    RaiseDiff(p.EntityId, old, fresh);
                    break;
            }
        }

        void SendValues(long entityId, ulong steamId)
        {
            var values = Values(entityId, false);
            if (values != null)
                _core.Net.ToPlayer(new Packet { Kind = PacketKind.BlockValues, EntityId = entityId, Texts = Flatten(values) }, steamId);
        }

        bool HasAccess(long entityId, ulong steamId)
        {
            var block = MyAPIGateway.Entities.GetEntityById(entityId) as IMyTerminalBlock;
            if (block == null)
                return false;
            if (steamId == MyAPIGateway.Multiplayer.MyId && _core.IsPlayer)
                return true;
            var player = _core.FindPlayer(steamId);
            return player != null && block.HasPlayerAccess(player.IdentityId);
        }

        // ---------- change events ----------

        public void Subscribe(string mod, Action<long, string, string> handler)
        {
            if (mod == null || handler == null)
                return;
            List<Action<long, string, string>> list;
            if (!_subs.TryGetValue(mod, out list))
                _subs[mod] = list = new List<Action<long, string, string>>();
            if (!list.Contains(handler))
                list.Add(handler);
        }

        public void Unsubscribe(string mod, Action<long, string, string> handler)
        {
            List<Action<long, string, string>> list;
            if (mod != null && _subs.TryGetValue(mod, out list))
                list.Remove(handler);
        }

        void Raise(long entityId, string mod, string key, string value)
        {
            List<Action<long, string, string>> list;
            if (!_subs.TryGetValue(mod, out list))
                return;
            foreach (var h in list.ToArray())
            {
                try { h(entityId, key, value); }
                catch (Exception e) { Log.Error("block change handler of " + mod, e); }
            }
        }

        void RaiseDiff(long entityId, Dictionary<string, string> old, Dictionary<string, string> fresh)
        {
            foreach (var kv in fresh)
            {
                string o;
                if (old == null || !old.TryGetValue(kv.Key, out o) || o != kv.Value)
                    RaiseKey(entityId, kv.Key, kv.Value);
            }
            if (old == null)
                return;
            foreach (var kv in old)
                if (!fresh.ContainsKey(kv.Key))
                    RaiseKey(entityId, kv.Key, null);
        }

        void RaiseKey(long entityId, string compound, string value)
        {
            int i = compound.IndexOf(FieldSep);
            if (i > 0)
                Raise(entityId, compound.Substring(0, i), compound.Substring(i + 1), value);
        }

        // ---------- storage format ----------

        static string ReadStorage(IMyEntity entity)
        {
            string s;
            if (entity.Storage != null && entity.Storage.TryGetValue(StorageGuid, out s))
                return s;
            return null;
        }

        static void WriteStorage(IMyEntity entity, Dictionary<string, string> values)
        {
            if (entity.Storage == null)
                entity.Storage = new MyModStorageComponent();
            var sb = new StringBuilder();
            foreach (var kv in values)
            {
                if (sb.Length > 0)
                    sb.Append(EntrySep);
                sb.Append(kv.Key).Append(FieldSep).Append(kv.Value);
            }
            entity.Storage.SetValue(StorageGuid, sb.ToString());
        }

        /// <summary>Entry = mod FS key FS value; values must not contain the two separators.</summary>
        static Dictionary<string, string> Parse(string s)
        {
            var d = new Dictionary<string, string>();
            if (string.IsNullOrEmpty(s))
                return d;
            foreach (var entry in s.Split(EntrySep))
            {
                int a = entry.IndexOf(FieldSep);
                int b = a < 0 ? -1 : entry.IndexOf(FieldSep, a + 1);
                if (b > 0)
                    d[entry.Substring(0, b)] = entry.Substring(b + 1);
            }
            return d;
        }

        static string[] Flatten(Dictionary<string, string> values)
        {
            var list = new List<string>(values.Count * 2);
            foreach (var kv in values)
            {
                list.Add(kv.Key);
                list.Add(kv.Value);
            }
            return list.ToArray();
        }

        static Dictionary<string, string> Unflatten(string[] texts)
        {
            var d = new Dictionary<string, string>();
            if (texts == null)
                return d;
            for (int i = 0; i + 1 < texts.Length; i += 2)
                d[texts[i]] = texts[i + 1];
            return d;
        }
    }
}
