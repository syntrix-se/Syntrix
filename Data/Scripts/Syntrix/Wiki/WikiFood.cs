// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using System.Globalization;
using Sandbox.Definitions;
using VRage.Game;

namespace Syntrix.Wiki
{
    /// <summary>
    /// Food and seeds from the ConsumableItem, SeedItem and blueprint definitions; values as in the game's tooltip
    /// (value × time). Lists Ingredients, Meals, Spoiled, Seeds; table Recipes.
    /// </summary>
    public static class WikiFood
    {
        private static List<MyConsumableItemDefinition> Edibles()
        {
            var list = new List<MyConsumableItemDefinition>();
            foreach (var p in MyDefinitionManager.Static.GetPhysicalItemDefinitions())
            {
                var c = p as MyConsumableItemDefinition;
                if (c == null || c.Stats == null)
                    continue;
                foreach (var stat in c.Stats)
                    if (stat.Name == "Food")
                    {
                        list.Add(c);
                        break;
                    }
            }
            // By satiation, equal ones by name
            list.Sort((x, y) =>
            {
                int v = Satiation(x).CompareTo(Satiation(y));
                return v != 0 ? v : string.Compare(x.DisplayNameText, y.DisplayNameText, StringComparison.CurrentCultureIgnoreCase);
            });
            return list;
        }

        private static double Satiation(MyConsumableItemDefinition c)
        {
            foreach (var stat in c.Stats)
                if (stat.Name == "Food")
                    return stat.Value * stat.Time;
            return 0;
        }

        private static string Percent(double value, bool german, bool sign)
        {
            var s = WikiAuto.Number(Math.Abs(value) * 100, "0", german) + " %";
            return value < 0 ? "−" + s : (sign ? "+" + s : s);
        }

        private static string Effect(MyConsumableItemDefinition c, bool german)
        {
            // Satiation first, then health, then everything else
            var list = new List<string> { (german ? "Sättigung " : "Food ") + Percent(Satiation(c), german, false) };
            foreach (var stat in c.Stats)
                if (stat.Name == "Health")
                    list.Add((german ? "Gesundheit " : "Health ") + Percent(stat.Value * stat.Time, german, true));
            foreach (var stat in c.Stats)
                if (stat.Name != "Food" && stat.Name != "Health")
                    list.Add(stat.Name + " " + Percent(stat.Value * stat.Time, german, true));
            return string.Join(", ", list);
        }

        private static string Name(MyPhysicalItemDefinition p) => string.IsNullOrEmpty(p.DisplayNameText) ? p.Id.SubtypeName : p.DisplayNameText;

        /// <summary>
        /// Lists Ingredients (edible, not MealPack_…), Meals (MealPack_… shown in lists), Spoiled (MealPack_… not public:
        /// finds only, named as a group by the wiki file) and Seeds. False = not responsible.
        /// </summary>
        public static bool FillList(WikiData data, WikiBlock block)
        {
            var kind = block.Auto.ToLowerInvariant();
            if (kind == "seeds")
            {
                var seeds = new List<MyPhysicalItemDefinition>();
                foreach (var p in MyDefinitionManager.Static.GetPhysicalItemDefinitions())
                    if (p != null && p.Id.TypeId.ToString() == "MyObjectBuilder_SeedItem" && WikiAuto.Visible(data, p.Id, p.Public))
                        seeds.Add(p);
                seeds.Sort((x, y) => string.Compare(Name(x), Name(y), StringComparison.CurrentCultureIgnoreCase));
                foreach (var p in seeds)
                    block.Entries.Add(new WikiEntry { De = Name(p), En = Name(p), Icons = WikiAuto.Icon(p.Id) });
                return true;
            }
            if (kind != "ingredients" && kind != "meals" && kind != "spoiled")
                return false;
            foreach (var c in Edibles())
            {
                bool meal = c.Id.SubtypeName.StartsWith("MealPack_", StringComparison.Ordinal);
                bool visible = WikiAuto.Visible(data, c.Id, c.Public);
                string group = !meal ? "ingredients" : (visible ? "meals" : "spoiled");
                if (group != kind || (!meal && !visible))
                    continue;
                if (group == "spoiled" && WikiData.Names(data.Hidden, WikiAuto.Key(c.Id)))
                    continue;
                block.Entries.Add(new WikiEntry
                {
                    De = Name(c),
                    En = Name(c),
                    ExtraDe = Effect(c, true),
                    ExtraEn = Effect(c, false),
                    Icons = WikiAuto.Icon(c.Id),
                });
            }
            return true;
        }

        /// <summary>Table Recipes: all blueprints whose result is edible and shown, with ingredients and effect. False = not responsible.</summary>
        public static bool FillTable(WikiData data, WikiBlock block)
        {
            if (!string.Equals(block.Auto, "Recipes", StringComparison.OrdinalIgnoreCase))
                return false;
            block.HeaderDe = new[] { "Gericht", "Zutaten", "Wirkung" };
            block.HeaderEn = new[] { "Dish", "Ingredients", "Effect" };
            if (block.Widths == null)
                block.Widths = new[] { 2f, 3f, 3f };
            var edible = new Dictionary<MyDefinitionId, MyConsumableItemDefinition>();
            foreach (var c in Edibles())
                if (WikiAuto.Visible(data, c.Id, c.Public))
                    edible[c.Id] = c;
            var rows = new List<KeyValuePair<string, MyBlueprintDefinitionBase>>();
            foreach (var bp in MyDefinitionManager.Static.GetBlueprintDefinitions())
            {
                if (bp == null || !bp.Public || bp.Results == null || bp.Results.Length != 1)
                    continue;
                MyConsumableItemDefinition c;
                if (!edible.TryGetValue(bp.Results[0].Id, out c))
                    continue;
                rows.Add(new KeyValuePair<string, MyBlueprintDefinitionBase>(Name(c), bp));
            }
            rows.Sort((x, y) => string.Compare(x.Key, y.Key, StringComparison.CurrentCultureIgnoreCase));
            var seen = new HashSet<string>();
            foreach (var row in rows)
            {
                var bp = row.Value;
                var c = edible[bp.Results[0].Id];
                var parts = new List<string>();
                foreach (var p in bp.Prerequisites)
                    parts.Add(((double)p.Amount).ToString("0.##", CultureInfo.InvariantCulture) + " " + WikiAuto.ItemName(p.Id));
                var ingredients = string.Join(", ", parts);
                if (!seen.Add(row.Key + "|" + ingredients))
                    continue;
                block.RowsDe.Add(new[] { row.Key, ingredients, Effect(c, true) });
                block.RowsEn.Add(new[] { row.Key, ingredients, Effect(c, false) });
            }
            return true;
        }
    }
}
