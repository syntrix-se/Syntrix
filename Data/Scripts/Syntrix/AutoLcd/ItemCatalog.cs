// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;
using System.Collections.Generic;
using Sandbox.Definitions;

namespace Syntrix.AutoLcd
{
    /// <summary>
    /// All physical items of the game and of loaded mods, read from the definitions: main type, short names for the
    /// item filters (subtype in lower case, display name without spaces, a few established short forms), default
    /// quota and whether the item is listed with 0. Item settings of the server file add or change entries.
    /// </summary>
    internal static class ItemCatalog
    {
        /// <summary>Main types in display order.</summary>
        public static readonly string[] MainTypes =
        {
            "ore", "ingot", "component", "ammo", "tool", "oxygen", "gas", "seed", "consumable", "object",
        };

        // Established short forms of the item filters (game item subtypes; the rest follows from the names).
        private static readonly string[] _aliases =
        {
            "bpglass", "Component/BulletproofGlass", "gravgen", "Component/GravityGenerator",
            "radio", "Component/RadioCommunication", "thruster", "Component/Thrust",
            "gravel", "Ingot/Stone",
            "s-10", "PhysicalGunObject/SemiAutoPistolItem", "s-10e", "PhysicalGunObject/ElitePistolItem",
            "s-20a", "PhysicalGunObject/FullAutoPistolItem", "mr-20", "PhysicalGunObject/AutomaticRifleItem",
            "mr-8p", "PhysicalGunObject/PreciseAutomaticRifleItem", "mr-50a", "PhysicalGunObject/RapidFireAutomaticRifleItem",
            "mr-30e", "PhysicalGunObject/UltimateAutomaticRifleItem", "ro-1", "PhysicalGunObject/BasicHandHeldLauncherItem",
            "pro-1", "PhysicalGunObject/AdvancedHandHeldLauncherItem",
            "5.56x45mm", "AmmoMagazine/NATO_5p56x45mm", "s-10mag", "AmmoMagazine/SemiAutoPistolMagazine",
            "s-10emag", "AmmoMagazine/ElitePistolMagazine", "s-20amag", "AmmoMagazine/FullAutoPistolMagazine",
            "mr-20mag", "AmmoMagazine/AutomaticRifleGun_Mag_20rd", "mr-8pmag", "AmmoMagazine/PreciseAutomaticRifleGun_Mag_5rd",
            "mr-50amag", "AmmoMagazine/RapidFireAutomaticRifleGun_Mag_50rd", "mr-30emag", "AmmoMagazine/UltimateAutomaticRifleGun_Mag_30rd",
            "gatlingammo", "AmmoMagazine/NATO_25x184mm", "rockets", "AmmoMagazine/Missile200mm",
            "autocannonmag", "AmmoMagazine/AutocannonClip", "assaultcannonshell", "AmmoMagazine/MediumCalibreAmmo",
            "srailgunsabot", "AmmoMagazine/SmallRailgunAmmo", "lrailgunsabot", "AmmoMagazine/LargeRailgunAmmo",
            "artilleryshell", "AmmoMagazine/LargeCalibreAmmo",
            "fruitseeds", "SeedItem/Fruit", "grainseeds", "SeedItem/Grain", "mushroomspores", "SeedItem/Mushrooms",
            "vegetableseeds", "SeedItem/Vegetables", "unknownmeal", "ConsumableItem/MealPack_Unknown",
            "protoscrap", "Ingot/PrototechScrap", "protoframe", "Component/PrototechFrame",
            "protopanel", "Component/PrototechPanel", "protocapacitor", "Component/PrototechCapacitor",
            "protopropunit", "Component/PrototechPropulsionUnit", "protomachinery", "Component/PrototechMachinery",
            "protocircuitry", "Component/PrototechCircuitry", "protocoolunit", "Component/PrototechCoolingUnit",
        };

        // Game items listed only from 1 piece (spoilers and late game items).
        private static readonly HashSet<string> _onlyWhenPresent = new HashSet<string>
        {
            "ConsumableItem/MealPack_BananaBeef", "ConsumableItem/MealPack_ClangCrunchies",
            "ConsumableItem/MealPack_ExpiredSlop", "ConsumableItem/MealPack_FoodPaste",
            "ConsumableItem/MealPack_Hardtack", "ConsumableItem/MealPack_SynthLoaf", "ConsumableItem/MealPack_Unknown",
        };

        private static readonly List<ItemInfo> _items = new List<ItemInfo>();
        private static readonly Dictionary<string, ItemInfo> _byKey = new Dictionary<string, ItemInfo>();
        private static readonly Dictionary<string, List<ItemInfo>> _byShort = new Dictionary<string, List<ItemInfo>>();

        /// <summary>One item.</summary>
        public class ItemInfo
        {
            /// <summary>Type name without "MyObjectBuilder_", e.g. "Ore".</summary>
            public string TypeName;

            public string Subtype;

            /// <summary>"Ore/Iron".</summary>
            public string Key;

            /// <summary>Lower case main type ("ore", "ammo" ...).</summary>
            public string MainType;

            public string DisplayKey;

            public string DisplayName;

            /// <summary>Name set in the settings (wins over the definition).</summary>
            public string OwnName;

            public double Quota;

            public bool ShowZero;

            public float Volume;

            public float Mass;

            /// <summary>Name on the screen.</summary>
            public string Name
            {
                get { return !string.IsNullOrEmpty(OwnName) ? OwnName : LcdText.Name(DisplayKey, DisplayName); }
            }
        }

        /// <summary>All items in definition order.</summary>
        public static List<ItemInfo> Items
        {
            get { return _items; }
        }

        /// <summary>Reads the definitions and applies the settings (server, after the settings changed).</summary>
        public static void Build(AutoLcdSettings settings)
        {
            _items.Clear();
            _byKey.Clear();
            _byShort.Clear();
            foreach (var definition in MyDefinitionManager.Static.GetPhysicalItemDefinitions())
            {
                if (definition == null)
                    continue;
                string typeName = TypeNameOf(definition.Id.TypeId.ToString());
                string subtype = definition.Id.SubtypeName;
                if (typeName == "TreeObject")
                    continue;
                var item = new ItemInfo
                {
                    TypeName = typeName,
                    Subtype = subtype,
                    Key = typeName + "/" + subtype,
                    MainType = MainTypeOf(typeName),
                    DisplayKey = definition.DisplayNameEnum.HasValue ? definition.DisplayNameEnum.Value.String : null,
                    DisplayName = definition.DisplayNameText ?? subtype,
                    Volume = definition.Volume,
                    Mass = definition.Mass,
                };
                item.Quota = settings.QuotaOf(item.MainType);
                bool listedType = Array.IndexOf(MainTypes, item.MainType) >= 0;
                item.ShowZero = listedType && definition.Public && (settings.ShowZeroModItems || (definition.Context != null && definition.Context.IsBaseGame))
                    && !subtype.StartsWith("Prototech", StringComparison.Ordinal) && !_onlyWhenPresent.Contains(item.Key);
                Add(item);
            }
            for (int i = 0; i + 1 < _aliases.Length; i += 2)
            {
                ItemInfo item;
                if (_byKey.TryGetValue(_aliases[i + 1], out item))
                    AddShort(_aliases[i], item);
            }
            foreach (var entry in settings.Items)
                Apply(entry, settings);
        }

        /// <summary>Item of a type name ("Ore" or "MyObjectBuilder_Ore") and subtype; null if unknown.</summary>
        public static ItemInfo Find(string typeId, string subtype)
        {
            ItemInfo item;
            _byKey.TryGetValue(TypeNameOf(typeId) + "/" + subtype, out item);
            return item;
        }

        /// <summary>Items with a short name (lower case); null if none.</summary>
        public static List<ItemInfo> ByShortName(string shortName)
        {
            List<ItemInfo> list;
            _byShort.TryGetValue(shortName.ToLowerInvariant(), out list);
            return list;
        }

        /// <summary>Item for an unknown type (mod items added at runtime); added to the catalog.</summary>
        public static ItemInfo Unknown(string typeId, string subtype)
        {
            string typeName = TypeNameOf(typeId);
            var item = new ItemInfo
            {
                TypeName = typeName,
                Subtype = subtype,
                Key = typeName + "/" + subtype,
                MainType = MainTypeOf(typeName),
                DisplayName = subtype,
            };
            Add(item);
            return item;
        }

        /// <summary>Lower case main type of a type name; also accepts the main type names themselves.</summary>
        public static string MainTypeOf(string typeName)
        {
            switch (typeName)
            {
                case "Ore": return "ore";
                case "Ingot": return "ingot";
                case "Component": return "component";
                case "AmmoMagazine": return "ammo";
                case "PhysicalGunObject": return "tool";
                case "OxygenContainerObject": return "oxygen";
                case "GasContainerObject": return "gas";
                case "SeedItem": return "seed";
                case "ConsumableItem": return "consumable";
                case "PhysicalObject": return "object";
                default: return typeName.ToLowerInvariant();
            }
        }

        /// <summary>True if the text names a main type (ore, ingot ... or a type name of a mod).</summary>
        public static bool IsMainType(string text)
        {
            string lower = text.ToLowerInvariant();
            if (Array.IndexOf(MainTypes, lower) >= 0)
                return true;
            foreach (var item in _items)
                if (item.MainType == lower)
                    return true;
            return false;
        }

        private static string TypeNameOf(string typeId)
        {
            const string prefix = "MyObjectBuilder_";
            return typeId.StartsWith(prefix, StringComparison.Ordinal) ? typeId.Substring(prefix.Length) : typeId;
        }

        private static void Add(ItemInfo item)
        {
            if (_byKey.ContainsKey(item.Key))
                return;
            _items.Add(item);
            _byKey[item.Key] = item;
            AddShort(item.Subtype, item);
            if (item.Subtype.StartsWith("MealPack_", StringComparison.Ordinal))
                AddShort(item.Subtype.Substring(9), item);
            if (!string.IsNullOrEmpty(item.DisplayName))
                AddShort(item.DisplayName.Replace(" ", ""), item);
        }

        private static void AddShort(string name, ItemInfo item)
        {
            if (string.IsNullOrEmpty(name))
                return;
            string key = name.ToLowerInvariant();
            List<ItemInfo> list;
            if (!_byShort.TryGetValue(key, out list))
            {
                list = new List<ItemInfo>();
                _byShort[key] = list;
            }
            if (!list.Contains(item))
                list.Add(item);
        }

        private static void Apply(AutoLcdSettings.ItemSetting entry, AutoLcdSettings settings)
        {
            if (string.IsNullOrEmpty(entry.Subtype))
                return;
            string typeName = TypeNameOf(string.IsNullOrEmpty(entry.Type) ? "Component" : entry.Type);
            ItemInfo item;
            if (!_byKey.TryGetValue(typeName + "/" + entry.Subtype, out item))
            {
                item = Unknown(typeName, entry.Subtype);
                item.Quota = settings.QuotaOf(item.MainType);
            }
            double quota;
            if (Formatting.TryNumber(entry.Quota, out quota))
                item.Quota = quota;
            if (!string.IsNullOrEmpty(entry.Name))
                item.OwnName = entry.Name;
            AddShort(entry.Short, item);
            AddShort(entry.Short2, item);
            if (string.Equals(entry.ShowZero, "true", StringComparison.OrdinalIgnoreCase))
                item.ShowZero = true;
            else if (string.Equals(entry.ShowZero, "false", StringComparison.OrdinalIgnoreCase))
                item.ShowZero = false;
        }
    }
}
