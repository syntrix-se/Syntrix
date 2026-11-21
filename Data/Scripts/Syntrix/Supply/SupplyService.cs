// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Syntrix.Supply
{
    /// <summary>
    /// Supply of grid groups (API 6, 1.5.0), server only. A mod watches a grid group through an anchor (a block or grid of
    /// the group) with a named profile; Syntrix then keeps its reactors on and fuelled and its weapons loaded, out of
    /// nothing, without conveyors:
    /// - Reactors: switched on, the first item fuel of the reactor definition is topped up to ReactorFuel (whole pieces
    ///   for integral items).
    /// - Weapons: filled up to WeaponMagazines of the magazines in their weapon definition, from the stock of the group.
    /// - Stock: StockPerWeapon default magazines per weapon, filled once on the first check, afterwards RefillPerWeapon
    ///   per weapon every RefillSeconds up to that target. It lies in the source blocks of the group (block types that a
    ///   mod registered as source, e.g. an ammo store) and is loot there; a group without source block keeps an
    ///   invisible stock that Syntrix saves with the world.
    /// With NpcOnly (default) only blocks owned by an NPC (identity without Steam account) are supplied.
    /// </summary>
    public sealed class SupplyService
    {
        /// <summary>The block lists of a group are rebuilt this often (building changes, destroyed blocks).</summary>
        private const double ListSeconds = 30.0;

        private const string StateFile = "Syntrix-Supply.txt";

        private sealed class Profile
        {
            public double CheckSeconds = 5;
            public bool NpcOnly = true;
            public bool Reactors = true;
            public int ReactorFuel = 10;
            public int WeaponMagazines = 4;
            public int StockPerWeapon = 10;
            public double RefillSeconds = 120;
            public int RefillPerWeapon = 1;
        }

        private sealed class Watch
        {
            public string Profile;
            public double NextCheck, ListUntil, NextRefill;
            public readonly List<IMyTerminalBlock> Weapons = new List<IMyTerminalBlock>();
            public readonly List<IMyTerminalBlock> Reactors = new List<IMyTerminalBlock>();
            public readonly List<IMyTerminalBlock> Sources = new List<IMyTerminalBlock>();
            /// <summary>Target stock per magazine type (default magazine of every weapon x stock per weapon).</summary>
            public readonly Dictionary<MyDefinitionId, int> Target = new Dictionary<MyDefinitionId, int>();
            /// <summary>Refill per cycle and magazine type.</summary>
            public readonly Dictionary<MyDefinitionId, int> Refill = new Dictionary<MyDefinitionId, int>();
        }

        private readonly SyntrixCore _core;
        private readonly Dictionary<string, Profile> _profiles = new Dictionary<string, Profile>();
        private readonly HashSet<MyDefinitionId> _sourceTypes = new HashSet<MyDefinitionId>();
        private readonly Dictionary<long, Watch> _watches = new Dictionary<long, Watch>();
        private readonly Dictionary<MyDefinitionId, MyDefinitionId[]> _magazines = new Dictionary<MyDefinitionId, MyDefinitionId[]>();
        private readonly Dictionary<MyDefinitionId, MyReactorDefinition.FuelInfo?> _fuel = new Dictionary<MyDefinitionId, MyReactorDefinition.FuelInfo?>();
        private readonly List<IMyCubeGrid> _group = new List<IMyCubeGrid>();
        private readonly List<IMySlimBlock> _slims = new List<IMySlimBlock>();
        private readonly List<long> _due = new List<long>();
        private static readonly MyDefinitionId[] None = new MyDefinitionId[0];

        // saved with the world
        private readonly HashSet<long> _filledSources = new HashSet<long>();
        private readonly Dictionary<long, Dictionary<MyDefinitionId, int>> _stock = new Dictionary<long, Dictionary<MyDefinitionId, int>>();
        private bool _dirty, _loaded;
        private int _tick;

        /// <summary>Makes the service.</summary>
        public SupplyService(SyntrixCore core)
        {
            _core = core;
        }

        /// <summary>Server: defines or replaces a profile. Settings are lines "key=value" (keys as the fields of the
        /// profile: CheckSeconds, NpcOnly, Reactors, ReactorFuel, WeaponMagazines, StockPerWeapon, RefillSeconds,
        /// RefillPerWeapon); missing keys keep their defaults.</summary>
        public void DefineProfile(string mod, string name, string settings)
        {
            if (!_core.IsServer || string.IsNullOrEmpty(name))
                return;
            var p = new Profile();
            foreach (var raw in (settings ?? "").Split('\n', ';'))
            {
                var line = raw.Trim();
                int i = line.IndexOf('=');
                if (i <= 0)
                    continue;
                string key = line.Substring(0, i).Trim(), value = line.Substring(i + 1).Trim();
                double d;
                bool number = double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out d);
                bool flag = string.Equals(value, "true", StringComparison.OrdinalIgnoreCase) || value == "1";
                switch (key)
                {
                    case "CheckSeconds": if (number) p.CheckSeconds = Math.Max(1, d); break;
                    case "NpcOnly": p.NpcOnly = flag; break;
                    case "Reactors": p.Reactors = flag; break;
                    case "ReactorFuel": if (number) p.ReactorFuel = Math.Max(0, (int)d); break;
                    case "WeaponMagazines": if (number) p.WeaponMagazines = Math.Max(0, (int)d); break;
                    case "StockPerWeapon": if (number) p.StockPerWeapon = Math.Max(0, (int)d); break;
                    case "RefillSeconds": if (number) p.RefillSeconds = Math.Max(1, d); break;
                    case "RefillPerWeapon": if (number) p.RefillPerWeapon = Math.Max(0, (int)d); break;
                    default: Log.Warn(mod + ": supply profile " + name + " has the unknown key " + key); break;
                }
            }
            _profiles[name] = p;
            Log.Info(mod + ": supply profile " + name + ": check " + p.CheckSeconds.ToString("0.#", CultureInfo.InvariantCulture)
                + " s, NPC only " + p.NpcOnly + ", reactors " + (p.Reactors ? "fuel " + p.ReactorFuel : "off") + ", weapons "
                + p.WeaponMagazines + " magazines, stock " + p.StockPerWeapon + " per weapon, refill " + p.RefillPerWeapon
                + " every " + p.RefillSeconds.ToString("0", CultureInfo.InvariantCulture) + " s");
        }

        /// <summary>Registers a block type "TypeId/SubtypeId" as source: the stock of a group lies in such blocks.</summary>
        public void RegisterSource(string mod, string blockKey)
        {
            MyDefinitionId id;
            if (string.IsNullOrEmpty(blockKey) || !MyDefinitionId.TryParse(blockKey.Contains("MyObjectBuilder_") ? blockKey : "MyObjectBuilder_" + blockKey, out id))
            {
                Log.Warn(mod + ": supply source " + blockKey + " not readable, ignored.");
                return;
            }
            _sourceTypes.Add(id);
        }

        /// <summary>Server: supplies the grid group of the anchor (block or grid entity id) with the profile until
        /// <see cref="Unwatch"/>. Calling it again with the same anchor only changes the profile.</summary>
        public void StartWatch(long anchor, string profile)
        {
            if (!_core.IsServer || anchor == 0)
                return;
            Load();
            Watch w;
            if (_watches.TryGetValue(anchor, out w))
            {
                w.Profile = profile;
                return;
            }
            _watches[anchor] = new Watch { Profile = profile };
        }

        /// <summary>Server: stops the supply of the anchor and forgets its stock (base dead, sold, given up).</summary>
        public void Unwatch(long anchor)
        {
            if (!_core.IsServer)
                return;
            Load();
            _watches.Remove(anchor);
            if (_stock.Remove(anchor))
                _dirty = true;
        }

        /// <summary>True if the anchor is watched.</summary>
        public bool IsWatched(long anchor) { return _watches.ContainsKey(anchor); }

        /// <summary>Server, every frame: checks the watches whose time has come (one pass per second at most).</summary>
        public void Tick()
        {
            if (!_core.IsServer || _watches.Count == 0 || ++_tick % 60 != 0)
                return;
            double now = MyAPIGateway.Session.ElapsedPlayTime.TotalSeconds;
            _due.Clear();
            foreach (var kv in _watches)
                if (now >= kv.Value.NextCheck)
                    _due.Add(kv.Key);
            foreach (var anchor in _due)
            {
                try { Check(anchor, _watches[anchor], now); }
                catch (Exception e) { Log.Error("supply", e); }
            }
        }

        /// <summary>Server: writes the stock with the world.</summary>
        public void Save()
        {
            if (!_core.IsServer || !_dirty)
                return;
            var sb = new StringBuilder();
            foreach (var id in _filledSources)
                sb.Append("S;").Append(id.ToString(CultureInfo.InvariantCulture)).Append('\n');
            foreach (var kv in _stock)
            {
                sb.Append("V;").Append(kv.Key.ToString(CultureInfo.InvariantCulture)).Append(';');
                bool first = true;
                foreach (var item in kv.Value)
                {
                    if (!first)
                        sb.Append(',');
                    first = false;
                    sb.Append(item.Key.ToString()).Append('=').Append(item.Value.ToString(CultureInfo.InvariantCulture));
                }
                sb.Append('\n');
            }
            try
            {
                using (var w = MyAPIGateway.Utilities.WriteFileInWorldStorage(StateFile, typeof(SupplyService)))
                    w.Write(sb.ToString());
                _dirty = false;
            }
            catch (Exception e) { Log.Error("supply state", e); }
        }

        /// <summary>Clears everything.</summary>
        public void Stop()
        {
            _profiles.Clear();
            _sourceTypes.Clear();
            _watches.Clear();
            _magazines.Clear();
            _fuel.Clear();
            _filledSources.Clear();
            _stock.Clear();
        }

        // ---------- state ----------

        private void Load()
        {
            if (_loaded)
                return;
            _loaded = true;
            try
            {
                if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(StateFile, typeof(SupplyService)))
                    return;
                string text;
                using (var r = MyAPIGateway.Utilities.ReadFileInWorldStorage(StateFile, typeof(SupplyService)))
                    text = r.ReadToEnd();
                foreach (var raw in text.Split('\n'))
                {
                    var parts = raw.Trim().Split(';');
                    long id;
                    if (parts.Length < 2 || !long.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
                        continue;
                    if (parts[0] == "S")
                        _filledSources.Add(id);
                    else if (parts[0] == "V")
                    {
                        var stock = new Dictionary<MyDefinitionId, int>();
                        if (parts.Length > 2)
                            foreach (var entry in parts[2].Split(','))
                            {
                                int i = entry.LastIndexOf('=');
                                MyDefinitionId item;
                                int count;
                                if (i > 0 && MyDefinitionId.TryParse(entry.Substring(0, i), out item)
                                    && int.TryParse(entry.Substring(i + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out count))
                                    stock[item] = count;
                            }
                        _stock[id] = stock;
                    }
                }
            }
            catch (Exception e) { Log.Error("supply state", e); }
        }

        // ---------- one group ----------

        private void Check(long anchor, Watch w, double now)
        {
            Profile p;
            if (!_profiles.TryGetValue(w.Profile ?? "", out p))
                p = new Profile();
            w.NextCheck = now + p.CheckSeconds;

            var entity = MyAPIGateway.Entities.GetEntityById(anchor);
            var grid = entity as IMyCubeGrid ?? (entity as IMyCubeBlock)?.CubeGrid;
            if (grid == null || grid.Physics == null || grid.MarkedForClose)
                return;   // not loaded right now (streaming): keep the watch and the stock

            if (now >= w.ListUntil)
                BuildLists(w, grid, p, now);

            if (w.Weapons.Count > 0)
            {
                if (w.Sources.Count > 0)
                {
                    FillSourcesFirstTime(w, grid.CustomName);
                    if (now >= w.NextRefill)
                    {
                        w.NextRefill = now + p.RefillSeconds;
                        RefillSources(w);
                    }
                    FillWeaponsFromSources(w, p);
                }
                else
                {
                    var stock = VirtualStock(anchor, w, grid.CustomName);
                    if (now >= w.NextRefill)
                    {
                        w.NextRefill = now + p.RefillSeconds;
                        RefillVirtual(w, stock);
                    }
                    FillWeaponsFromVirtual(w, p, stock);
                }
            }

            if (p.Reactors)
                SupplyReactors(w, p);
        }

        private void BuildLists(Watch w, IMyCubeGrid grid, Profile p, double now)
        {
            w.ListUntil = now + ListSeconds;
            if (w.NextRefill <= 0)
                w.NextRefill = now + p.RefillSeconds;
            w.Weapons.Clear();
            w.Reactors.Clear();
            w.Sources.Clear();
            w.Target.Clear();
            w.Refill.Clear();
            _group.Clear();
            MyAPIGateway.GridGroups.GetGroup(grid, GridLinkTypeEnum.Mechanical, _group);
            if (_group.Count == 0)
                _group.Add(grid);
            foreach (var g in _group)
            {
                _slims.Clear();
                g.GetBlocks(_slims, s => s.FatBlock is IMyTerminalBlock);
                foreach (var slim in _slims)
                {
                    var block = (IMyTerminalBlock)slim.FatBlock;
                    if (block.MarkedForClose || (p.NpcOnly && !IsNpc(block.OwnerId)))
                        continue;
                    if (block is IMyUserControllableGun)
                        w.Weapons.Add(block);
                    else if (block is IMyReactor)
                        w.Reactors.Add(block);
                    else if (_sourceTypes.Contains(block.BlockDefinition))
                        w.Sources.Add(block);
                }
            }
            _group.Clear();
            _slims.Clear();
            foreach (var weapon in w.Weapons)
            {
                var magazines = Magazines(weapon);
                if (magazines.Length == 0)
                    continue;
                AddCount(w.Target, magazines[0], p.StockPerWeapon);
                AddCount(w.Refill, magazines[0], p.RefillPerWeapon);
            }
        }

        /// <summary>NPC: owner without Steam account.</summary>
        private static bool IsNpc(long identityId) { return identityId != 0 && MyAPIGateway.Players.TryGetSteamId(identityId) == 0; }

        private static void AddCount(Dictionary<MyDefinitionId, int> counts, MyDefinitionId id, int amount)
        {
            int old;
            counts.TryGetValue(id, out old);
            counts[id] = old + amount;
        }

        /// <summary>Magazines a weapon accepts (weapon definition); the first is its default magazine.</summary>
        private MyDefinitionId[] Magazines(IMyTerminalBlock weapon)
        {
            MyDefinitionId[] magazines;
            var id = weapon.BlockDefinition;
            if (_magazines.TryGetValue(id, out magazines))
                return magazines;
            magazines = None;
            var blockDef = MyDefinitionManager.Static.GetCubeBlockDefinition(id) as MyWeaponBlockDefinition;
            MyWeaponDefinition weaponDef;
            if (blockDef != null && MyDefinitionManager.Static.TryGetWeaponDefinition(blockDef.WeaponDefinitionId, out weaponDef) && weaponDef.AmmoMagazinesId != null)
                magazines = weaponDef.AmmoMagazinesId;
            _magazines[id] = magazines;
            return magazines;
        }

        // ---------- stock in source blocks ----------

        private void FillSourcesFirstTime(Watch w, string name)
        {
            bool fresh = false;
            foreach (var source in w.Sources)
                if (_filledSources.Add(source.EntityId))
                    fresh = true;
            if (!fresh)
                return;
            _dirty = true;
            var list = new List<string>();
            foreach (var kv in w.Target)
            {
                int missing = kv.Value - SourceStock(w, kv.Key);
                int added = missing > 0 ? AddToSources(w, kv.Key, missing) : 0;
                if (added > 0)
                    list.Add(added + "x " + kv.Key.SubtypeName);
            }
            Log.Info("Supply " + name + ": stock filled (" + (list.Count > 0 ? string.Join(", ", list) : "nothing needed") + ")");
        }

        private void RefillSources(Watch w)
        {
            foreach (var kv in w.Target)
            {
                int missing = kv.Value - SourceStock(w, kv.Key), refill;
                w.Refill.TryGetValue(kv.Key, out refill);
                if (missing > 0 && refill > 0)
                    AddToSources(w, kv.Key, Math.Min(missing, refill));
            }
        }

        private static int SourceStock(Watch w, MyDefinitionId id)
        {
            MyFixedPoint sum = 0;
            foreach (var source in w.Sources)
            {
                var inventory = source.GetInventory(0);
                if (inventory != null)
                    sum += inventory.GetItemAmount(id);
            }
            return sum.ToIntSafe();
        }

        /// <summary>Puts up to the amount into the source blocks as far as there is room; returns what went in.</summary>
        private static int AddToSources(Watch w, MyDefinitionId id, int amount)
        {
            int added = 0;
            foreach (var source in w.Sources)
            {
                if (added >= amount)
                    break;
                added += AddItems(source.GetInventory(0), id, amount - added);
            }
            return added;
        }

        private static void FillWeaponsFromSources(Watch w, Profile p)
        {
            int target = p.WeaponMagazines;
            foreach (var weapon in w.Weapons)
            {
                if (weapon.MarkedForClose || !weapon.IsFunctional)
                    continue;
                var magazines = MagazinesOf(w, weapon);
                var inventory = weapon.GetInventory(0);
                if (magazines == null || inventory == null)
                    continue;
                int inside = Amount(inventory, magazines);
                foreach (var source in w.Sources)
                {
                    if (inside >= target)
                        break;
                    var from = source.GetInventory(0);
                    if (from == null)
                        continue;
                    for (int i = from.ItemCount - 1; i >= 0 && inside < target; i--)
                    {
                        var item = from.GetItemAt(i);
                        if (!item.HasValue || Array.IndexOf(magazines, (MyDefinitionId)item.Value.Type) < 0)
                            continue;
                        from.TransferItemTo(inventory, i, null, true, MyFixedPoint.Min(item.Value.Amount, target - inside), false);   // false: no conveyor check
                        int now = Amount(inventory, magazines);
                        if (now <= inside)
                            break;   // no room or filter
                        inside = now;
                    }
                }
            }
        }

        // ---------- invisible stock ----------

        private Dictionary<MyDefinitionId, int> VirtualStock(long anchor, Watch w, string name)
        {
            Dictionary<MyDefinitionId, int> stock;
            if (_stock.TryGetValue(anchor, out stock))
                return stock;
            stock = new Dictionary<MyDefinitionId, int>(w.Target);
            _stock[anchor] = stock;
            _dirty = true;
            Log.Info("Supply " + name + ": stock without source block filled (" + w.Target.Count + " magazine types)");
            return stock;
        }

        private void RefillVirtual(Watch w, Dictionary<MyDefinitionId, int> stock)
        {
            foreach (var kv in w.Target)
            {
                int have, refill;
                stock.TryGetValue(kv.Key, out have);
                w.Refill.TryGetValue(kv.Key, out refill);
                if (have >= kv.Value || refill <= 0)
                    continue;
                stock[kv.Key] = Math.Min(kv.Value, have + refill);
                _dirty = true;
            }
        }

        private void FillWeaponsFromVirtual(Watch w, Profile p, Dictionary<MyDefinitionId, int> stock)
        {
            int target = p.WeaponMagazines;
            foreach (var weapon in w.Weapons)
            {
                if (weapon.MarkedForClose || !weapon.IsFunctional)
                    continue;
                var magazines = MagazinesOf(w, weapon);
                var inventory = weapon.GetInventory(0);
                if (magazines == null || inventory == null)
                    continue;
                int missing = target - Amount(inventory, magazines);
                foreach (var magazine in magazines)
                {
                    int have;
                    if (missing <= 0 || !stock.TryGetValue(magazine, out have) || have <= 0)
                        continue;
                    int added = AddItems(inventory, magazine, Math.Min(missing, have));
                    if (added <= 0)
                        continue;
                    stock[magazine] = have - added;
                    missing -= added;
                    _dirty = true;
                }
            }
        }

        // ---------- shared ----------

        private MyDefinitionId[] MagazinesOf(Watch w, IMyTerminalBlock weapon)
        {
            var magazines = Magazines(weapon);
            return magazines.Length > 0 ? magazines : null;
        }

        private static int Amount(IMyInventory inventory, MyDefinitionId[] magazines)
        {
            MyFixedPoint sum = 0;
            foreach (var magazine in magazines)
                sum += inventory.GetItemAmount(magazine);
            return sum.ToIntSafe();
        }

        /// <summary>Adds whole pieces out of nothing as far as there is room; returns what went in.</summary>
        private static int AddItems(IMyInventory inventory, MyDefinitionId id, int amount)
        {
            var def = MyDefinitionManager.Static.GetPhysicalItemDefinition(id);
            if (inventory == null || def == null || amount <= 0)
                return 0;
            int fits = def.Volume > 0 ? (int)Math.Floor(((double)inventory.MaxVolume - (double)inventory.CurrentVolume) / def.Volume) : amount;
            int count = Math.Min(amount, fits);
            if (count <= 0 || !inventory.CanItemsBeAdded(count, id))
                return 0;
            inventory.AddItems(count, (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(id));
            return count;
        }

        private void SupplyReactors(Watch w, Profile p)
        {
            foreach (var reactor in w.Reactors)
            {
                if (reactor.MarkedForClose || !reactor.IsFunctional)
                    continue;
                var functional = (IMyFunctionalBlock)reactor;
                if (!functional.Enabled)
                    functional.Enabled = true;
                var fuel = Fuel(reactor);
                var inventory = reactor.GetInventory(0);
                if (!fuel.HasValue || p.ReactorFuel <= 0 || inventory == null)
                    continue;
                var id = fuel.Value.FuelId;
                var missing = (MyFixedPoint)p.ReactorFuel - inventory.GetItemAmount(id);
                float volume = fuel.Value.FuelDefinition != null ? fuel.Value.FuelDefinition.Volume : 0f;
                if (volume > 0)
                    missing = MyFixedPoint.Min(missing, (MyFixedPoint)(((double)inventory.MaxVolume - (double)inventory.CurrentVolume) / volume));
                var item = MyDefinitionManager.Static.GetPhysicalItemDefinition(id);
                if (item != null && item.HasIntegralAmounts)
                    missing = MyFixedPoint.Floor(missing);   // the reactor uses fuel rods piece by piece
                if (missing <= 0 || !inventory.CanItemsBeAdded(missing, id))
                    continue;
                inventory.AddItems(missing, fuel.Value.FuelItem ?? (MyObjectBuilder_PhysicalObject)MyObjectBuilderSerializer.CreateNewObject(id));
            }
        }

        /// <summary>First fuel of the reactor that is an item (no gas).</summary>
        private MyReactorDefinition.FuelInfo? Fuel(IMyTerminalBlock reactor)
        {
            MyReactorDefinition.FuelInfo? fuel;
            var id = reactor.BlockDefinition;
            if (_fuel.TryGetValue(id, out fuel))
                return fuel;
            fuel = null;
            var def = MyDefinitionManager.Static.GetCubeBlockDefinition(id) as MyReactorDefinition;
            if (def != null && def.FuelInfos != null)
                foreach (var info in def.FuelInfos)
                    if (info.FuelDefinition != null)
                    {
                        fuel = info;
                        break;
                    }
            _fuel[id] = fuel;
            if (!fuel.HasValue)
                Log.Warn("Supply: reactor " + id.SubtypeName + " without item fuel");
            return fuel;
        }
    }
}
