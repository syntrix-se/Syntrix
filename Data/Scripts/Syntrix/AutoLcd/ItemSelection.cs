// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;
using System.Collections.Generic;

namespace Syntrix.AutoLcd
{
    /// <summary>
    /// Item specification of the inventory commands, e.g. "+ingot/iron,gold:5000 -scrap +component:1000".
    /// Rules apply in order; a later matching rule wins (so "+component:1000 +steelplate:10000" gives steel plates
    /// their own quota). Without any "+" rule everything is included.
    /// </summary>
    internal class ItemSelection
    {
        private readonly List<Rule> _rules = new List<Rule>();
        private bool _hasInclude;

        /// <summary>Reads the "+"/"-" arguments from the given index on.</summary>
        public static ItemSelection Parse(CommandLine line, int firstArg)
        {
            var selection = new ItemSelection();
            for (int i = firstArg; i < line.Args.Count; i++)
            {
                string arg = line.Args[i];
                if (arg.Length < 2 || (arg[0] != '+' && arg[0] != '-'))
                    continue;
                bool include = arg[0] == '+';
                if (include)
                    selection._hasInclude = true;
                string currentType = null;
                foreach (string element in arg.Substring(1).Split(','))
                {
                    if (element.Length == 0)
                        continue;
                    string name = element;
                    double quota = double.NaN;
                    int colon = element.LastIndexOf(':');
                    if (colon > 0)
                    {
                        double value;
                        if (Formatting.TryNumber(element.Substring(colon + 1), out value))
                            quota = value;
                        name = element.Substring(0, colon);
                    }
                    var rule = new Rule { Include = include, Quota = quota };
                    int slash = name.IndexOf('/');
                    if (slash >= 0)
                    {
                        currentType = ItemCatalog.MainTypeOf(name.Substring(0, slash));
                        rule.MainType = currentType;
                        rule.Short = name.Substring(slash + 1).ToLowerInvariant();
                    }
                    else if (string.Equals(name, "all", StringComparison.OrdinalIgnoreCase))
                        rule.All = true;
                    else if (currentType == null && ItemCatalog.IsMainType(ItemCatalog.MainTypeOf(name)))
                        rule.MainType = ItemCatalog.MainTypeOf(name);
                    else
                    {
                        rule.MainType = currentType;
                        rule.Short = name.ToLowerInvariant();
                    }
                    selection._rules.Add(rule);
                }
            }
            return selection;
        }

        /// <summary>True if the item is shown; quota is NaN when no rule set one.</summary>
        public bool Decide(ItemCatalog.ItemInfo item, out double quota)
        {
            bool included = !_hasInclude;
            quota = double.NaN;
            foreach (var rule in _rules)
            {
                if (!rule.Matches(item))
                    continue;
                included = rule.Include;
                if (rule.Include && !double.IsNaN(rule.Quota))
                    quota = rule.Quota;
            }
            return included;
        }

        private class Rule
        {
            public bool Include;

            public bool All;

            public string MainType;

            public string Short;

            public double Quota = double.NaN;

            public bool Matches(ItemCatalog.ItemInfo item)
            {
                if (All)
                    return true;
                if (MainType != null && !string.Equals(item.MainType, MainType, StringComparison.OrdinalIgnoreCase))
                    return false;
                if (Short == null)
                    return true;
                if (string.Equals(item.Subtype, Short, StringComparison.OrdinalIgnoreCase))
                    return true;
                var list = ItemCatalog.ByShortName(Short);
                return list != null && list.Contains(item);
            }
        }
    }
}
