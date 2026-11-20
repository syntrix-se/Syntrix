// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.ModAPI;

namespace Syntrix.Reputation
{
    /// <summary>
    /// Reputation of players with factions (API 5, Syntrix 1.4.0). One mod (the faction mod) defines the scale, the
    /// tiers with trade discount and goods level, and per faction the start value, the lowest and highest value, the
    /// lowest trading tier, a slow recovery and links to other factions. Syntrix applies the start value once per
    /// player and faction, keeps every value inside its limits (also changes made by the game or other mods), passes a
    /// share of a change on to linked factions, sends every value to the player's machine and shows a short notice.
    /// The values themselves live in the game (faction collection), only the "start value given" marks are saved.
    /// </summary>
    public sealed class ReputationService
    {
        /// <summary>Event raised on the server and on the player's machine: MyTuple&lt;long identity, string tag, int old, int new, string reason&gt;.</summary>
        public const string ChangedEvent = "Syntrix.Rep.Changed";
        /// <summary>Change flag: pass a share on to linked factions.</summary>
        public const int FlagPropagate = 1;
        /// <summary>Change flag: no notice of the amount (tier changes are still shown).</summary>
        public const int FlagQuiet = 2;

        private const string MarkFile = "Syntrix-Reputation-Start.txt";
        private const int PollTicks = 100;
        private const int NoticeTicks = 120;
        private const int RecoveryTicks = 3600;

        private sealed class Tier
        {
            public string Key;
            public int Min;
            public float Percent;
            public int Goods;
        }

        private sealed class FactionDef
        {
            public string Tag;
            public int Start = int.MinValue;
            public int Min = int.MinValue;
            public int Max = int.MaxValue;
            public int TradeFrom = -1;
            public int RecoveryPerHour;
            public int RecoveryUpTo;
            public readonly List<MyTuple<string, float>> Links = new List<MyTuple<string, float>>();
        }

        private struct Pending
        {
            public long Identity;
            public long FactionId;
            public int Old;
            public int Value;
            public string Reason;
        }

        private readonly SyntrixCore _core;
        private readonly List<Tier> _tiers = new List<Tier>();
        private readonly Dictionary<string, FactionDef> _defs = new Dictionary<string, FactionDef>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, int> _pairs = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<long, HashSet<string>> _marks = new Dictionary<long, HashSet<string>>();
        private readonly HashSet<long> _checked = new HashSet<long>();
        private readonly Dictionary<long, ulong> _online = new Dictionary<long, ulong>();
        private readonly List<IMyPlayer> _players = new List<IMyPlayer>();
        private readonly List<Pending> _pending = new List<Pending>();
        private readonly Dictionary<string, int> _notices = new Dictionary<string, int>();
        private readonly Dictionary<string, double> _recovery = new Dictionary<string, double>();
        private int _scaleMin = -1500, _scaleMax = 1500;
        private string _definer;
        private bool _guard, _defsDirty, _marksDirty, _hooked;
        private int _tick;

        /// <summary>Makes the service.</summary>
        public ReputationService(SyntrixCore core) { _core = core; }

        /// <summary>Starts: the server watches changes and reads the marks, a player asks for its values.</summary>
        public void Start()
        {
            if (_core.IsServer)
            {
                LoadMarks();
                MyAPIGateway.Session.Factions.ReputationChanged += OnGameChange;
                _hooked = true;
            }
            else
                _core.Net.RawToServer(new Packet { Kind = PacketKind.ReputationRequest });
        }

        /// <summary>Stops and forgets everything.</summary>
        public void Stop()
        {
            if (_hooked && MyAPIGateway.Session?.Factions != null)
                MyAPIGateway.Session.Factions.ReputationChanged -= OnGameChange;
            _hooked = false;
            _tiers.Clear();
            _defs.Clear();
            _pairs.Clear();
            _marks.Clear();
            _checked.Clear();
            _online.Clear();
            _pending.Clear();
            _notices.Clear();
            _recovery.Clear();
            _gateTypes.Clear();
        }

        /// <summary>Server: writes the marks with the world.</summary>
        public void Save()
        {
            if (!_core.IsServer || !_marksDirty)
                return;
            var sb = new StringBuilder();
            foreach (var pair in _marks)
            {
                if (pair.Value.Count == 0)
                    continue;
                sb.Append(pair.Key.ToString(CultureInfo.InvariantCulture)).Append(';');
                sb.Append(string.Join(",", pair.Value)).Append('\n');
            }
            try
            {
                using (var w = MyAPIGateway.Utilities.WriteFileInWorldStorage(MarkFile, typeof(ReputationService)))
                    w.Write(sb.ToString());
                _marksDirty = false;
            }
            catch (Exception e) { Log.Error("reputation marks", e); }
        }

        private void LoadMarks()
        {
            try
            {
                if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(MarkFile, typeof(ReputationService)))
                    return;
                string text;
                using (var r = MyAPIGateway.Utilities.ReadFileInWorldStorage(MarkFile, typeof(ReputationService)))
                    text = r.ReadToEnd();
                foreach (var line in text.Split('\n'))
                {
                    int i = line.IndexOf(';');
                    long id;
                    if (i <= 0 || !long.TryParse(line.Substring(0, i), NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
                        continue;
                    var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var tag in line.Substring(i + 1).Trim().Split(','))
                        if (tag.Length > 0)
                            set.Add(tag);
                    _marks[id] = set;
                }
            }
            catch (Exception e) { Log.Error("reputation marks", e); }
        }

        // ---------- definitions (server; players get them from the server) ----------

        private bool MayDefine(string mod)
        {
            if (!_core.IsServer)
                return false;
            if (_definer == null)
                _definer = mod;
            else if (_definer != mod)
            {
                Log.Warn("Reputation definitions of " + mod + " ignored, " + _definer + " defines them.");
                return false;
            }
            _defsDirty = true;
            return true;
        }

        /// <summary>Lowest and highest value of the scale (also the default limits of every faction).</summary>
        public void DefineScale(string mod, int min, int max)
        {
            if (max <= min || !MayDefine(mod))
                return;
            _scaleMin = min;
            _scaleMax = max;
        }

        /// <summary>Tiers: text key, lowest value, trade percent (discount on buying, markup on selling), goods level (0 = no trade).</summary>
        public void DefineTiers(string mod, List<MyTuple<string, int, float, int>> tiers)
        {
            if (tiers == null || !MayDefine(mod))
                return;
            _tiers.Clear();
            foreach (var t in tiers)
                _tiers.Add(new Tier { Key = t.Item1 ?? "", Min = t.Item2, Percent = t.Item3, Goods = Math.Max(0, t.Item4) });
            _tiers.Sort((a, b) => a.Min.CompareTo(b.Min));
        }

        /// <summary>A faction: start value (int.MinValue = none), lowest and highest value, lowest tier that trades (-1 = as the tier says).</summary>
        public void DefineFaction(string mod, string tag, int start, int min, int max, int tradeFrom)
        {
            if (string.IsNullOrWhiteSpace(tag) || !MayDefine(mod))
                return;
            var d = Def(tag, true);
            d.Start = start;
            d.Min = min;
            d.Max = max >= min ? max : min;
            d.TradeFrom = tradeFrom;
        }

        /// <summary>Slow recovery while the player is online: perHour points up to upTo.</summary>
        public void DefineRecovery(string mod, string tag, int perHour, int upTo)
        {
            if (string.IsNullOrWhiteSpace(tag) || !MayDefine(mod))
                return;
            var d = Def(tag, true);
            d.RecoveryPerHour = Math.Max(0, perHour);
            d.RecoveryUpTo = upTo;
        }

        /// <summary>A change at "from" also changes "to" by factor times the change (negative = the other way).</summary>
        public void DefineLink(string mod, string from, string to, float factor)
        {
            if (string.IsNullOrWhiteSpace(from) || string.IsNullOrWhiteSpace(to) || !MayDefine(mod))
                return;
            var d = Def(from, true);
            d.Links.RemoveAll(l => string.Equals(l.Item1, to, StringComparison.OrdinalIgnoreCase));
            if (factor != 0f)
                d.Links.Add(new MyTuple<string, float>(to, factor));
        }

        /// <summary>Forgets all definitions (before a mod defines them again, for example after reloading its file).</summary>
        public void ClearDefinitions(string mod)
        {
            if (!MayDefine(mod))
                return;
            _tiers.Clear();
            _defs.Clear();
            _scaleMin = -1500;
            _scaleMax = 1500;
        }

        private FactionDef Def(string tag, bool create)
        {
            FactionDef d;
            if (tag != null && _defs.TryGetValue(tag, out d))
                return d;
            if (!create)
                return null;
            d = new FactionDef { Tag = tag };
            _defs[tag] = d;
            return d;
        }

        // ---------- faction to faction ----------

        /// <summary>Server: reputation between two factions, both ways, also on every player's machine.</summary>
        public void SetBetween(string tagA, string tagB, int value)
        {
            if (!_core.IsServer)
                return;
            if (ApplyBetween(tagA, tagB, value))
            {
                _pairs[tagA + "|" + tagB] = value;
                _core.Net.RawToOthers(new Packet { Kind = PacketKind.Reputation, Channel = "f", Texts = new[] { tagA, tagB, Num(value) } });
            }
        }

        private static bool ApplyBetween(string tagA, string tagB, int value)
        {
            var fs = MyAPIGateway.Session.Factions;
            var a = fs.TryGetFactionByTag(tagA);
            var b = fs.TryGetFactionByTag(tagB);
            if (a == null || b == null || a == b)
                return false;
            fs.SetReputation(a.FactionId, b.FactionId, value);
            fs.SetReputation(b.FactionId, a.FactionId, value);
            return true;
        }

        // ---------- queries (all machines) ----------

        /// <summary>Value of a player with a faction, int.MinValue if the faction does not exist.</summary>
        public int Get(long identity, string tag)
        {
            var f = MyAPIGateway.Session?.Factions?.TryGetFactionByTag(tag);
            return f != null ? MyAPIGateway.Session.Factions.GetReputationBetweenPlayerAndFaction(identity, f.FactionId) : int.MinValue;
        }

        /// <summary>Value, tier index (-1 none), trade percent and goods level (0 = no trade) of a player with a faction.</summary>
        public MyTuple<int, int, float, int> Info(long identity, string tag)
        {
            int value = Get(identity, tag);
            if (value == int.MinValue)
                return new MyTuple<int, int, float, int>(value, -1, 0f, 0);
            int tier = TierIndex(value);
            float percent = tier >= 0 ? _tiers[tier].Percent : 0f;
            int goods = tier >= 0 ? _tiers[tier].Goods : 1;
            var d = Def(tag, false);
            if (goods == 0 && d != null && d.TradeFrom >= 0 && tier >= d.TradeFrom)
            {
                goods = 1;
                percent = 0f;
            }
            return new MyTuple<int, int, float, int>(value, tier, percent, goods);
        }

        /// <summary>
        /// 1.4.1: true when the block belongs to a defined faction that does not trade with the player (goods level 0)
        /// and the player is no member. Use objects of Syntrix (F and K) then stay closed for that player.
        /// </summary>
        public bool Refuses(IMyCubeBlock block, long identity, out string factionName)
        {
            return Refuses(block, identity, MinGoods(block), out factionName);
        }

        /// <summary>
        /// 1.5.0: true when the block belongs to a defined faction, the player is no member and the player's goods level
        /// with that faction is below minGoods (1 = the faction must trade at all).
        /// </summary>
        public bool Refuses(IMyCubeBlock block, long identity, int minGoods, out string factionName)
        {
            factionName = null;
            if (block == null || identity == 0 || block.OwnerId == 0 || minGoods <= 0)
                return false;
            var f = MyAPIGateway.Session?.Factions?.TryGetPlayerFaction(block.OwnerId);
            if (f == null || f.IsMember(identity) || Def(f.Tag, false) == null)
                return false;
            var info = Info(identity, f.Tag);
            if (info.Item1 == int.MinValue || info.Item4 >= minGoods)
                return false;
            factionName = f.Name;
            return true;
        }

        /// <summary>API 6: true when the block refuses the player (see <see cref="Refuses(IMyCubeBlock, long, int, out string)"/>).</summary>
        public bool RefusesById(long blockId, long identity)
        {
            var block = MyAPIGateway.Entities.GetEntityById(blockId) as IMyCubeBlock;
            string name;
            return Refuses(block, identity, MinGoods(block), out name);
        }

        /// <summary>
        /// API 6: price for a player at a faction after the trade percent of the player's tier. playerBuys: the percent
        /// is a discount (rounded up); otherwise the faction pays and the percent is a markup (rounded down). A negative
        /// percent works the other way. Unknown faction or player: the price unchanged.
        /// </summary>
        public long Price(long identity, string tag, long price, bool playerBuys)
        {
            if (price <= 0)
                return price;
            var info = Info(identity, tag);
            if (info.Item1 == int.MinValue || info.Item3 == 0f)
                return price;
            double p = Math.Max(-100.0, Math.Min(100.0, info.Item3)) / 100.0;
            return playerBuys ? (long)Math.Ceiling(price * (1.0 - p)) : (long)Math.Floor(price * (1.0 + p));
        }

        // ---------- gate at any terminal block (API 6, 1.5.0) ----------

        /// <summary>Block setting key (mod "Syntrix") with the lowest goods level a block asks for.</summary>
        public const string GoodsKey = "rep.goods";

        private readonly Dictionary<string, int> _gateTypes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        private int _gateTick;
        private long _gateShown;

        /// <summary>
        /// API 6, all machines: every block of this type (TypeId like "MyObjectBuilder_StoreBlock", or "TypeId/SubtypeId")
        /// closes its terminal and F screen for players whose goods level with the owning faction is below minGoods.
        /// 0 removes the type.
        /// </summary>
        public void GateType(string type, int minGoods)
        {
            if (string.IsNullOrWhiteSpace(type))
                return;
            if (minGoods <= 0)
                _gateTypes.Remove(type);
            else
                _gateTypes[type] = minGoods;
        }

        /// <summary>API 6, server: lowest goods level of one block (saved with the block, all machines see it); 0 = the type's default.</summary>
        public void GateBlock(long blockId, int minGoods)
        {
            if (!_core.IsServer)
                return;
            _core.Blocks.Set(blockId, "Syntrix", GoodsKey, minGoods > 0 ? minGoods.ToString(CultureInfo.InvariantCulture) : null);
        }

        /// <summary>Lowest goods level the block asks for: its own value, else its type's, else 1 (the faction must trade).</summary>
        public int MinGoods(IMyCubeBlock block)
        {
            int own = OwnGoods(block);
            if (own > 0)
                return own;
            int typed = TypeGoods(block);
            return typed > 0 ? typed : 1;
        }

        private int OwnGoods(IMyCubeBlock block)
        {
            if (block == null || _core.Blocks == null)
                return 0;
            return ToInt(_core.Blocks.Get(block.EntityId, "Syntrix", GoodsKey), 0);
        }

        private int TypeGoods(IMyCubeBlock block)
        {
            if (block == null || _gateTypes.Count == 0)
                return 0;
            var id = block.BlockDefinition;
            int v;
            if (_gateTypes.TryGetValue(id.TypeId.ToString() + "/" + id.SubtypeName, out v))
                return v;
            return _gateTypes.TryGetValue(id.TypeId.ToString(), out v) ? v : 0;
        }

        /// <summary>Player machine, every frame: closes the terminal while it shows a gated block that refuses the player.</summary>
        public void GateTick()
        {
            if (!_core.IsPlayer || ++_gateTick % 10 != 0)
                return;
            var gui = MyAPIGateway.Gui;
            if (gui == null || gui.GetCurrentScreen == MyTerminalPageEnum.None)
            {
                _gateShown = 0;
                return;
            }
            var block = gui.InteractedEntity as IMyCubeBlock;
            var player = MyAPIGateway.Session.Player;
            if (block == null || player == null || (OwnGoods(block) <= 0 && TypeGoods(block) <= 0))
                return;
            string name;
            if (!Refuses(block, player.IdentityId, MinGoods(block), out name))
                return;
            gui.CloseTerminalScreen();
            if (_gateShown == block.EntityId)
                return;
            _gateShown = block.EntityId;
            _core.Net.ShowLocal("Syntrix_Rep_Refused", "{0} refuses you access: your reputation is too low.", new[] { name }, 3000, "Red");
        }

        /// <summary>Tier of a value, -1 when no tiers are defined.</summary>
        public int TierIndex(int value)
        {
            int found = _tiers.Count > 0 ? 0 : -1;
            for (int i = 0; i < _tiers.Count; i++)
                if (value >= _tiers[i].Min)
                    found = i;
            return found;
        }

        /// <summary>Number of tiers.</summary>
        public int TierCount { get { return _tiers.Count; } }

        /// <summary>Tier: text key, lowest and highest value, trade percent, goods level.</summary>
        public MyTuple<string, int, int, float, int> TierAt(int index)
        {
            if (index < 0 || index >= _tiers.Count)
                return new MyTuple<string, int, int, float, int>(null, 0, 0, 0f, 0);
            var t = _tiers[index];
            int max = index + 1 < _tiers.Count ? _tiers[index + 1].Min - 1 : _scaleMax;
            return new MyTuple<string, int, int, float, int>(t.Key, t.Min, max, t.Percent, t.Goods);
        }

        /// <summary>Lowest and highest value of the scale.</summary>
        public MyTuple<int, int> Scale { get { return new MyTuple<int, int>(_scaleMin, _scaleMax); } }

        /// <summary>Tags of the defined factions.</summary>
        public string[] Factions()
        {
            var tags = new string[_defs.Count];
            _defs.Keys.CopyTo(tags, 0);
            Array.Sort(tags, StringComparer.OrdinalIgnoreCase);
            return tags;
        }

        /// <summary>Lowest and highest allowed value of a faction.</summary>
        public MyTuple<int, int> Limits(string tag)
        {
            var d = Def(tag, false);
            int min = d != null && d.Min != int.MinValue ? d.Min : _scaleMin;
            int max = d != null && d.Max != int.MaxValue ? d.Max : _scaleMax;
            return new MyTuple<int, int>(min, Math.Max(min, max));
        }

        private int Clamp(string tag, int value)
        {
            var l = Limits(tag);
            return value < l.Item1 ? l.Item1 : value > l.Item2 ? l.Item2 : value;
        }

        // ---------- changes (server) ----------

        /// <summary>Server: changes a value inside the limits; with FlagPropagate linked factions change too. Returns the applied change.</summary>
        public int Change(long identity, string tag, int delta, string reason, int flags)
        {
            if (!_core.IsServer || delta == 0)
                return 0;
            var f = MyAPIGateway.Session.Factions.TryGetFactionByTag(tag);
            if (f == null)
                return 0;
            int old = MyAPIGateway.Session.Factions.GetReputationBetweenPlayerAndFaction(identity, f.FactionId);
            long sum = (long)old + delta;
            int value = Clamp(f.Tag, sum > int.MaxValue ? int.MaxValue : sum < int.MinValue ? int.MinValue : (int)sum);
            int applied = value - old;
            if (applied != 0)
                Store(identity, f, old, value, reason, (flags & FlagQuiet) != 0);
            if ((flags & FlagPropagate) != 0 && applied != 0)
            {
                var d = Def(f.Tag, false);
                if (d != null)
                    foreach (var link in d.Links)
                    {
                        int share = (int)Math.Round(applied * link.Item2, MidpointRounding.AwayFromZero);
                        if (share != 0)
                            Change(identity, link.Item1, share, reason, flags & ~FlagPropagate);
                    }
            }
            return applied;
        }

        /// <summary>Server: sets a value inside the limits.</summary>
        public void Set(long identity, string tag, int value, string reason)
        {
            if (!_core.IsServer)
                return;
            var f = MyAPIGateway.Session.Factions.TryGetFactionByTag(tag);
            if (f == null)
                return;
            int old = MyAPIGateway.Session.Factions.GetReputationBetweenPlayerAndFaction(identity, f.FactionId);
            value = Clamp(f.Tag, value);
            if (value != old)
                Store(identity, f, old, value, reason, true);
        }

        /// <summary>Server: gives a player the start values again (all defined factions).</summary>
        public void ResetStart(long identity)
        {
            if (!_core.IsServer)
                return;
            _marks.Remove(identity);
            _marksDirty = true;
            _checked.Remove(identity);
            CheckPlayer(identity);
        }

        private void Store(long identity, IMyFaction f, int old, int value, string reason, bool quiet)
        {
            _guard = true;
            try { MyAPIGateway.Session.Factions.SetReputationBetweenPlayerAndFaction(identity, f.FactionId, value); }
            finally { _guard = false; }
            Announce(identity, f, old, value, reason, quiet);
        }

        private void Announce(long identity, IMyFaction f, int old, int value, string reason, bool quiet)
        {
            ulong steamId = SteamId(identity);
            if (steamId != 0)
                _core.Net.ToPlayer(new Packet { Kind = PacketKind.Reputation, Channel = "v", EntityId = identity, Texts = new[] { f.Tag, Num(value), Num(old), reason ?? "" } }, steamId);
            if (!_core.IsPlayer || steamId != MyAPIGateway.Multiplayer.MyId)
                Publish(identity, f.Tag, old, value, reason);     // a local player gets it with the packet
            if (steamId == 0)
                return;
            if (!quiet)
            {
                string key = identity.ToString(CultureInfo.InvariantCulture) + "|" + f.Tag;
                int sum;
                _notices.TryGetValue(key, out sum);
                _notices[key] = sum + (value - old);
            }
            int t0 = TierIndex(old), t1 = TierIndex(value);
            if (t0 != t1 && t1 >= 0)
                _core.Net.SendNotify(steamId, t1 > t0 ? "Syntrix_Rep_TierUp" : "Syntrix_Rep_TierDown", "{0}: {1}",
                    new[] { f.Name, Protocol.TextArgPrefix + _tiers[t1].Key }, 5000, t1 > t0 ? "Green" : "Red");
        }

        private void Publish(long identity, string tag, int old, int value, string reason)
        {
            _core.Events.Publish(ChangedEvent, new MyTuple<long, string, int, int, string>(identity, tag, old, value, reason ?? ""));
        }

        // A change by the game or another mod. It fires before the value is stored: check it in the next frame.
        private void OnGameChange(long identity, long factionId, int value, ReputationChangeReason reason)
        {
            if (_guard)
                return;
            int old = MyAPIGateway.Session.Factions.GetReputationBetweenPlayerAndFaction(identity, factionId);
            _pending.Add(new Pending { Identity = identity, FactionId = factionId, Old = old, Value = value, Reason = "Game." + reason });
        }

        private void HandlePending()
        {
            if (_pending.Count == 0)
                return;
            var list = _pending.ToArray();
            _pending.Clear();
            foreach (var p in list)
            {
                var f = MyAPIGateway.Session.Factions.TryGetFactionById(p.FactionId);
                if (f == null)
                    continue;
                int now = MyAPIGateway.Session.Factions.GetReputationBetweenPlayerAndFaction(p.Identity, p.FactionId);
                int value = Clamp(f.Tag, now);
                if (value != now)
                {
                    _guard = true;
                    try { MyAPIGateway.Session.Factions.SetReputationBetweenPlayerAndFaction(p.Identity, p.FactionId, value); }
                    finally { _guard = false; }
                }
                if (value != p.Old)
                    Announce(p.Identity, f, p.Old, value, p.Reason, true);
            }
        }

        // ---------- players ----------

        private ulong SteamId(long identity)
        {
            ulong id;
            return _online.TryGetValue(identity, out id) ? id : 0;
        }

        private void RefreshOnline()
        {
            _online.Clear();
            _players.Clear();
            MyAPIGateway.Players.GetPlayers(_players);
            foreach (var p in _players)
                if (p != null && !p.IsBot && p.IdentityId != 0)
                    _online[p.IdentityId] = p.SteamUserId;
            _players.Clear();
        }

        // Start values once per player and faction, then the limits (they may have changed since the last session).
        private void CheckPlayer(long identity)
        {
            if (_defs.Count == 0)
                return;
            HashSet<string> marks;
            if (!_marks.TryGetValue(identity, out marks))
                _marks[identity] = marks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var fs = MyAPIGateway.Session.Factions;
            foreach (var d in _defs.Values)
            {
                var f = fs.TryGetFactionByTag(d.Tag);
                if (f == null)
                    continue;
                int old = fs.GetReputationBetweenPlayerAndFaction(identity, f.FactionId);
                int value = old;
                if (d.Start != int.MinValue && !marks.Contains(d.Tag))
                {
                    value = d.Start;
                    marks.Add(d.Tag);
                    _marksDirty = true;
                }
                value = Clamp(d.Tag, value);
                if (value != old)
                    Store(identity, f, old, value, "Syntrix.Start", true);
            }
            _checked.Add(identity);
        }

        private void Recover()
        {
            var fs = MyAPIGateway.Session.Factions;
            foreach (var d in _defs.Values)
            {
                if (d.RecoveryPerHour <= 0)
                    continue;
                var f = fs.TryGetFactionByTag(d.Tag);
                if (f == null)
                    continue;
                foreach (var identity in _online.Keys)
                {
                    int value = fs.GetReputationBetweenPlayerAndFaction(identity, f.FactionId);
                    string key = identity.ToString(CultureInfo.InvariantCulture) + "|" + d.Tag;
                    if (value >= d.RecoveryUpTo)
                    {
                        _recovery.Remove(key);
                        continue;
                    }
                    double acc;
                    _recovery.TryGetValue(key, out acc);
                    acc += d.RecoveryPerHour * (RecoveryTicks / 60.0) / 3600.0;
                    int whole = (int)acc;
                    _recovery[key] = acc - whole;
                    if (whole > 0)
                        Change(identity, d.Tag, Math.Min(whole, d.RecoveryUpTo - value), "Syntrix.Recovery", FlagQuiet);
                }
            }
        }

        private void FlushNotices()
        {
            if (_notices.Count == 0)
                return;
            foreach (var pair in _notices)
            {
                if (pair.Value == 0)
                    continue;
                int i = pair.Key.IndexOf('|');
                long identity;
                if (!long.TryParse(pair.Key.Substring(0, i), NumberStyles.Integer, CultureInfo.InvariantCulture, out identity))
                    continue;
                ulong steamId = SteamId(identity);
                var f = MyAPIGateway.Session.Factions.TryGetFactionByTag(pair.Key.Substring(i + 1));
                if (steamId == 0 || f == null)
                    continue;
                string amount = (pair.Value > 0 ? "+" : "") + Num(pair.Value);
                _core.Net.SendNotify(steamId, "Syntrix_Rep_Change", "{0}: {1}", new[] { f.Name, amount }, 3000, pair.Value > 0 ? "Green" : "Red");
            }
            _notices.Clear();
        }

        /// <summary>Every frame.</summary>
        public void Tick()
        {
            if (!_core.IsServer)
                return;
            _tick++;
            HandlePending();
            if (_defsDirty)
            {
                _defsDirty = false;
                _checked.Clear();
                _core.Net.RawToOthers(DefsPacket());
            }
            if (_tick % PollTicks == 0)
            {
                RefreshOnline();
                foreach (var identity in _online.Keys)
                    if (!_checked.Contains(identity))
                        CheckPlayer(identity);
            }
            if (_tick % NoticeTicks == 0)
                FlushNotices();
            if (_tick % RecoveryTicks == 0)
                Recover();
        }

        // ---------- network ----------

        private static string Num(int v) { return v.ToString(CultureInfo.InvariantCulture); }

        private static int ToInt(string s, int fallback)
        {
            int v;
            return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out v) ? v : fallback;
        }

        private static float ToFloat(string s)
        {
            float v;
            return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v) ? v : 0f;
        }

        private Packet DefsPacket()
        {
            var t = new List<string> { "S", Num(_scaleMin), Num(_scaleMax) };
            foreach (var tier in _tiers)
                t.AddRange(new[] { "T", tier.Key, Num(tier.Min), tier.Percent.ToString("R", CultureInfo.InvariantCulture), Num(tier.Goods) });
            foreach (var d in _defs.Values)
            {
                t.AddRange(new[] { "F", d.Tag, Num(d.Start), Num(d.Min), Num(d.Max), Num(d.TradeFrom), Num(d.RecoveryPerHour), Num(d.RecoveryUpTo) });
                foreach (var l in d.Links)
                    t.AddRange(new[] { "L", d.Tag, l.Item1, l.Item2.ToString("R", CultureInfo.InvariantCulture) });
            }
            return new Packet { Kind = PacketKind.Reputation, Channel = "d", Texts = t.ToArray() };
        }

        private void ReadDefs(string[] t)
        {
            _tiers.Clear();
            _defs.Clear();
            int i = 0;
            while (i < t.Length)
            {
                switch (t[i])
                {
                    case "S":
                        if (i + 2 >= t.Length) return;
                        _scaleMin = ToInt(t[i + 1], -1500);
                        _scaleMax = ToInt(t[i + 2], 1500);
                        i += 3;
                        break;
                    case "T":
                        if (i + 4 >= t.Length) return;
                        _tiers.Add(new Tier { Key = t[i + 1], Min = ToInt(t[i + 2], 0), Percent = ToFloat(t[i + 3]), Goods = ToInt(t[i + 4], 0) });
                        i += 5;
                        break;
                    case "F":
                        if (i + 7 >= t.Length) return;
                        var d = Def(t[i + 1], true);
                        d.Start = ToInt(t[i + 2], int.MinValue);
                        d.Min = ToInt(t[i + 3], int.MinValue);
                        d.Max = ToInt(t[i + 4], int.MaxValue);
                        d.TradeFrom = ToInt(t[i + 5], -1);
                        d.RecoveryPerHour = ToInt(t[i + 6], 0);
                        d.RecoveryUpTo = ToInt(t[i + 7], 0);
                        i += 8;
                        break;
                    case "L":
                        if (i + 3 >= t.Length) return;
                        Def(t[i + 1], true).Links.Add(new MyTuple<string, float>(t[i + 2], ToFloat(t[i + 3])));
                        i += 4;
                        break;
                    default:
                        return;
                }
            }
        }

        /// <summary>Network packet: Reputation to players, ReputationRequest to the server.</summary>
        public void OnPacket(Packet p, ulong sender)
        {
            if (p.Kind == PacketKind.ReputationRequest)
            {
                if (!_core.IsServer)
                    return;
                _core.Net.RawToPlayer(DefsPacket(), sender);
                foreach (var pair in _pairs)
                {
                    int i = pair.Key.IndexOf('|');
                    _core.Net.RawToPlayer(new Packet { Kind = PacketKind.Reputation, Channel = "f", Texts = new[] { pair.Key.Substring(0, i), pair.Key.Substring(i + 1), Num(pair.Value) } }, sender);
                }
                var player = _core.FindPlayer(sender);
                if (player != null)
                {
                    var fs = MyAPIGateway.Session.Factions;
                    var texts = new List<string>();
                    foreach (var d in _defs.Values)
                    {
                        var f = fs.TryGetFactionByTag(d.Tag);
                        if (f != null)
                            texts.AddRange(new[] { f.Tag, Num(fs.GetReputationBetweenPlayerAndFaction(player.IdentityId, f.FactionId)) });
                    }
                    if (texts.Count > 0)
                        _core.Net.RawToPlayer(new Packet { Kind = PacketKind.Reputation, Channel = "a", EntityId = player.IdentityId, Texts = texts.ToArray() }, sender);
                }
                return;
            }
            if (p.Texts == null)
                return;
            switch (p.Channel)
            {
                case "d":
                    if (!_core.IsServer)
                        ReadDefs(p.Texts);
                    break;
                case "f":
                    if (!_core.IsServer && p.Texts.Length >= 3)
                        ApplyBetween(p.Texts[0], p.Texts[1], ToInt(p.Texts[2], 0));
                    break;
                case "a":
                    if (!_core.IsServer)
                        for (int i = 0; i + 1 < p.Texts.Length; i += 2)
                            ApplyLocal(p.EntityId, p.Texts[i], ToInt(p.Texts[i + 1], 0), null);
                    break;
                case "v":
                    if (p.Texts.Length >= 4)
                    {
                        if (_core.IsServer)
                            Publish(p.EntityId, p.Texts[0], ToInt(p.Texts[2], 0), ToInt(p.Texts[1], 0), p.Texts[3]);   // hosting player
                        else
                            ApplyLocal(p.EntityId, p.Texts[0], ToInt(p.Texts[1], 0), p.Texts[3]);
                    }
                    break;
            }
        }

        // Player machine: the value from the server into the local faction collection.
        private void ApplyLocal(long identity, string tag, int value, string reason)
        {
            var fs = MyAPIGateway.Session.Factions;
            var f = fs.TryGetFactionByTag(tag);
            if (f == null)
                return;
            int old = fs.GetReputationBetweenPlayerAndFaction(identity, f.FactionId);
            if (old != value)
                fs.SetReputationBetweenPlayerAndFaction(identity, f.FactionId, value);
            if (reason != null || old != value)
                Publish(identity, f.Tag, old, value, reason ?? "Syntrix.Sync");
        }

        // ---------- admin chat ----------

        /// <summary>Chat command /syxrep (server, admins).</summary>
        public string OnChat(ulong sender, string[] args)
        {
            string cmd = args.Length > 0 ? args[0].ToLowerInvariant() : "list";
            // "set <TAG> <value>" / "add <TAG> <amount>" without a player: the sender himself
            if ((cmd == "set" || cmd == "add") && args.Length == 3 && MyAPIGateway.Session.Factions.TryGetFactionByTag(args[1]) != null)
                args = new[] { args[0], "", args[1], args[2] };
            IMyPlayer target = args.Length > 1 && args[1].Length == 0 ? _core.FindPlayer(sender) : null;
            if (target == null)
                target = args.Length > 1 ? FindByName(args[1]) : _core.FindPlayer(sender);
            if (target == null && (cmd != "list" || args.Length > 1))
                return "Player not found.";
            if (target == null)
                return "Usage: /syxrep list [player] | set [player] <TAG> <value> | add [player] <TAG> <amount> | reset <player>";
            switch (cmd)
            {
                case "list":
                    var sb = new StringBuilder(target.DisplayName).Append(':');
                    foreach (var tag in Factions())
                    {
                        var info = Info(target.IdentityId, tag);
                        if (info.Item1 == int.MinValue)
                            continue;
                        sb.Append('\n').Append(tag).Append(' ').Append(Num(info.Item1));
                        if (info.Item2 >= 0)
                            sb.Append(" (").Append(Network.Resolve(_tiers[info.Item2].Key, _tiers[info.Item2].Key)).Append(')');
                    }
                    return sb.ToString();
                case "set":
                case "add":
                    if (args.Length < 4 || MyAPIGateway.Session.Factions.TryGetFactionByTag(args[2]) == null)
                        return "Usage: /syxrep " + cmd + " [player] <TAG> <number>";
                    int n = ToInt(args[3], int.MinValue);
                    if (n == int.MinValue)
                        return "Not a number: " + args[3];
                    if (cmd == "set")
                        Set(target.IdentityId, args[2], n, "Syntrix.Admin");
                    else
                        Change(target.IdentityId, args[2], n, "Syntrix.Admin", FlagQuiet);
                    return target.DisplayName + " " + args[2].ToUpperInvariant() + ": " + Num(Get(target.IdentityId, args[2]));
                case "reset":
                    ResetStart(target.IdentityId);
                    return target.DisplayName + ": start values given again.";
            }
            return "Usage: /syxrep list [player] | set [player] <TAG> <value> | add [player] <TAG> <amount> | reset <player>";
        }

        private IMyPlayer FindByName(string name)
        {
            _players.Clear();
            MyAPIGateway.Players.GetPlayers(_players);
            IMyPlayer found = null;
            foreach (var p in _players)
                if (p != null && !p.IsBot && p.DisplayName != null && p.DisplayName.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    found = p;
                    if (string.Equals(p.DisplayName, name, StringComparison.OrdinalIgnoreCase))
                        break;
                }
            _players.Clear();
            return found;
        }
    }
}
