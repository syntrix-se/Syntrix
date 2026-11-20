// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI.Ingame;

namespace Syntrix.AutoLcd
{
    /// <summary>
    /// Inventory, InvList, Missing, MissingList. Item specification after the name filter: "+" adds, "-" removes;
    /// main types (ore, ingot, component, ammo, tool, oxygen, gas, seed, consumable, object), short names
    /// (steelplate, bpglass ...), "type/sub", lists with ",", quota with ":". Suffixes: NB no bars, NN no numbers,
    /// NNB neither, then X (no 0 lines, no default quotas), S (no ore in ingot lines), XS.
    /// </summary>
    internal static class InventoryCommands
    {
        private const int Chunk = 16;

        public static void Register()
        {
            CommandRegistry.Add("Inventory", CommandRegistry.Heavy, c => Run(c, true, false));
            CommandRegistry.Add("InvList", CommandRegistry.Heavy, c => Run(c, false, false));
            CommandRegistry.Add("Missing", CommandRegistry.Heavy, c => Run(c, true, true));
            CommandRegistry.Add("MissingList", CommandRegistry.Heavy, c => Run(c, false, true));
        }

        /// <summary>Item amounts of the blocks of a filter, shared between screens for a few seconds.</summary>
        public static IEnumerable<int> Sum(CommandContext context, string filterArg, Dictionary<string, double> result)
        {
            string key = "inv|" + (filterArg ?? "*");
            var shared = context.Source.Shared(key, 2) as Dictionary<string, double>;
            if (shared != null)
            {
                foreach (var kv in shared)
                    result[kv.Key] = kv.Value;
                yield break;
            }
            var items = new List<MyInventoryItem>();
            var blocks = new List<IMyTerminalBlock>(context.Blocks(filterArg, b => b.HasInventory));
            int count = 0;
            foreach (var block in blocks)
            {
                if (block.Closed)
                    continue;
                for (int i = 0; i < block.InventoryCount; i++)
                {
                    var inventory = block.GetInventory(i);
                    if (inventory == null)
                        continue;
                    items.Clear();
                    inventory.GetItems(items);
                    foreach (var item in items)
                    {
                        string itemKey = TypeName(item.Type.TypeId) + "/" + item.Type.SubtypeId;
                        double amount;
                        result.TryGetValue(itemKey, out amount);
                        result[itemKey] = amount + (double)item.Amount;
                    }
                }
                if (++count % Chunk == 0)
                    yield return 0;
            }
            context.Source.Share(key, new Dictionary<string, double>(result));
        }

        private static string TypeName(string typeId)
        {
            return typeId.StartsWith("MyObjectBuilder_", StringComparison.Ordinal) ? typeId.Substring(16) : typeId;
        }

        private static IEnumerable<int> Run(CommandContext context, bool headings, bool missing)
        {
            string suffix = context.Suffix;
            bool noBars = false, noNumbers = false;
            if (suffix.StartsWith("NNB", StringComparison.Ordinal))
            {
                noBars = noNumbers = true;
                suffix = suffix.Substring(3);
            }
            else if (suffix.StartsWith("NN", StringComparison.Ordinal))
            {
                noNumbers = true;
                suffix = suffix.Substring(2);
            }
            else if (suffix.StartsWith("NB", StringComparison.Ordinal))
            {
                noBars = true;
                suffix = suffix.Substring(2);
            }
            bool noZero = !missing && suffix.IndexOf('X') >= 0;
            bool noOre = !missing && suffix.IndexOf('S') >= 0;

            string filterArg = context.Line.Arg(0, "*");
            var selection = ItemSelection.Parse(context.Line, 1);
            var amounts = new Dictionary<string, double>();
            foreach (int step in Sum(context, filterArg, amounts))
                yield return step;

            var shown = new List<Row>();
            var seen = new HashSet<string>();
            foreach (var kv in amounts)
            {
                var item = ItemCatalog.Find(kv.Key.Substring(0, kv.Key.IndexOf('/')), kv.Key.Substring(kv.Key.IndexOf('/') + 1))
                    ?? ItemCatalog.Unknown(kv.Key.Substring(0, kv.Key.IndexOf('/')), kv.Key.Substring(kv.Key.IndexOf('/') + 1));
                seen.Add(item.Key);
                AddRow(shown, item, kv.Value, selection, missing, noZero);
            }
            if (!noZero)
            {
                foreach (var item in ItemCatalog.Items)
                    if (item.ShowZero && !seen.Contains(item.Key))
                        AddRow(shown, item, 0, selection, missing, noZero);
            }
            yield return 0;

            var canvas = context.Canvas;
            foreach (string mainType in TypeOrder(shown))
            {
                bool first = true;
                foreach (var row in shown)
                {
                    if (row.Item.MainType != mainType)
                        continue;
                    if (first && headings)
                        canvas.Text(Heading(mainType));
                    first = false;
                    string name = row.Item.Name;
                    if (!noOre && row.Item.MainType == "ingot")
                    {
                        double ore;
                        if (amounts.TryGetValue("Ore/" + row.Item.Subtype, out ore) && ore > 0)
                            name += " (" + Formatting.Amount(ore) + ")";
                    }
                    string value = noNumbers ? "" : Formatting.Amount(row.Amount);
                    if (!noBars && row.Quota > 0)
                        canvas.PairBar(name, row.Amount / row.Quota, value);
                    else
                        canvas.Pair(name, value);
                }
            }
        }

        private static void AddRow(List<Row> rows, ItemCatalog.ItemInfo item, double amount, ItemSelection selection, bool missing, bool noDefaultQuota)
        {
            double quota;
            if (!selection.Decide(item, out quota))
                return;
            if (double.IsNaN(quota))
                quota = missing ? 1 : (noDefaultQuota ? 0 : item.Quota);
            if (missing && amount >= quota)
                return;
            if (!missing && amount <= 0 && noDefaultQuota)
                return;
            rows.Add(new Row { Item = item, Amount = amount, Quota = quota });
        }

        private static IEnumerable<string> TypeOrder(List<Row> rows)
        {
            var order = new List<string>(ItemCatalog.MainTypes);
            foreach (var row in rows)
                if (!order.Contains(row.Item.MainType))
                    order.Add(row.Item.MainType);
            rows.Sort((a, b) =>
            {
                int t = order.IndexOf(a.Item.MainType).CompareTo(order.IndexOf(b.Item.MainType));
                return t != 0 ? t : string.Compare(a.Item.Name, b.Item.Name, StringComparison.CurrentCultureIgnoreCase);
            });
            return order;
        }

        private static string Heading(string mainType)
        {
            switch (mainType)
            {
                case "ore": return LcdText.Get("SyxAutoLcd_TypeOre", "Ores");
                case "ingot": return LcdText.Get("SyxAutoLcd_TypeIngot", "Ingots");
                case "component": return LcdText.Get("SyxAutoLcd_TypeComponent", "Components");
                case "ammo": return LcdText.Get("SyxAutoLcd_TypeAmmo", "Ammunition");
                case "tool": return LcdText.Get("SyxAutoLcd_TypeTool", "Tools");
                case "oxygen": return LcdText.Get("SyxAutoLcd_TypeOxygen", "Oxygen bottles");
                case "gas": return LcdText.Get("SyxAutoLcd_TypeGas", "Gas bottles");
                case "seed": return LcdText.Get("SyxAutoLcd_TypeSeed", "Seeds");
                case "consumable": return LcdText.Get("SyxAutoLcd_TypeConsumable", "Consumables");
                case "object": return LcdText.Get("SyxAutoLcd_TypeObject", "Objects");
                default: return mainType;
            }
        }

        private class Row
        {
            public ItemCatalog.ItemInfo Item;

            public double Amount;

            public double Quota;
        }
    }
}
