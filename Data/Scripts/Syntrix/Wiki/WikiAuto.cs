// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using System.Globalization;
using Sandbox.Definitions;
using Syntrix.Gui;
using VRage.Game;

namespace Syntrix.Wiki
{
    /// <summary>
    /// Pages and tables that write themselves from the definitions of the loaded mods (refinery and assembler
    /// blueprints, PlanetGeneratorDefinitions), so the wiki works with Vanilla and with any mod and stays correct after
    /// every balancing change. Generated lists show public definitions only (plus &lt;Show&gt;, minus &lt;Hide&gt;).
    /// A hand-written page with the same id stays as written and gets the generated blocks appended.
    /// Coordinates are never shown.
    /// </summary>
    public static class WikiAuto
    {
        /// <summary>Id of the system page for public planets that no system list names.</summary>
        public const string CollectionId = "planets";

        private static Dictionary<string, MyPlanetGeneratorDefinition> _planets;
        private static Catalog _catalog;

        // ---------- pages for systems and planets ----------

        /// <summary>
        /// Adds the system "planets" with every public planet definition that no system list names (once per data).
        /// </summary>
        public static void AddCollection(WikiData data)
        {
            foreach (var s in data.Systems)
                if (string.Equals(s.Id, CollectionId, StringComparison.OrdinalIgnoreCase))
                    return;
            var listed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in data.Planets.Values)
                listed.Add(p.Definition);
            var found = new List<MyPlanetGeneratorDefinition>();
            foreach (var g in MyDefinitionManager.Static.GetPlanetsGeneratorsDefinitions())
                if (g != null && !listed.Contains(g.Id.SubtypeName) && Visible(data, g.Id, g.Public))
                    found.Add(g);
            if (found.Count == 0)
                return;
            found.Sort((x, y) => string.Compare(PlanetName(x), PlanetName(y), StringComparison.CurrentCultureIgnoreCase));
            var system = new WikiSystem
            {
                Id = CollectionId,
                Parent = data.Pages.ContainsKey("solar-systems") ? "solar-systems" : "",
                Order = 900,
                De = "Weitere Planeten",
                En = "Other planets",
                Source = "Syntrix",
            };
            int order = 0;
            foreach (var g in found)
            {
                var id = "planet-" + g.Id.SubtypeName.ToLowerInvariant();
                if (data.Planets.ContainsKey(id))
                    continue;
                var name = PlanetName(g);
                var planet = new WikiPlanet { Id = id, Definition = g.Id.SubtypeName, Parent = "", De = name, En = name, SystemId = CollectionId, Order = order += 10 };
                system.Planets.Add(planet);
                data.Planets[id] = planet;
            }
            data.Systems.Add(system);
        }

        private static string PlanetName(MyPlanetGeneratorDefinition g) =>
            string.IsNullOrEmpty(g.DisplayNameText) ? g.Id.SubtypeName : g.DisplayNameText;

        /// <summary>
        /// Creates a page for every system and planet and the ore and component pages (after all files and sources,
        /// before the additions with Extend="true").
        /// </summary>
        public static void CreatePages(WikiData data)
        {
            AddCollection(data);
            foreach (var system in data.Systems)
            {
                var page = PageFor(data, system.Id, system.Parent, system.Order,
                    string.IsNullOrEmpty(system.De) ? system.Id : system.De, string.IsNullOrEmpty(system.En) ? system.Id : system.En, system.Source);
                if (string.IsNullOrEmpty(page.Symbol))
                    page.Symbol = "planet";
                page.Text("Alle Planeten und Monde dieses Systems. Die Werte stammen direkt aus den Planetendefinitionen. Koordinaten verrät das Wiki nicht.",
                          "All planets and moons of this system. The values come straight from the planet definitions. The wiki does not reveal coordinates.");
                page.Blocks.Add(WikiData.AutoTable("Planets", system.Id));
                page.Heading("Erze je Planet", "Ores by planet");
                page.Blocks.Add(WikiData.AutoTable("OresPerPlanet", system.Id));

                foreach (var planet in system.Planets)
                {
                    bool moon = !string.IsNullOrEmpty(planet.Parent);
                    page = PageFor(data, planet.Id, moon ? planet.Parent : system.Id, planet.Order, planet.Name(true), planet.Name(false), system.Source);
                    if (planet.Image != null || !string.IsNullOrEmpty(planet.FactsDe) || !string.IsNullOrEmpty(planet.FactsEn))
                    {
                        var image = planet.Image;
                        if (image != null && !GuiDraw.Exists(image))
                        {
                            data.Errors.Add($"Planet '{planet.Id}': image material '{image}' does not exist");
                            image = null;
                        }
                        page.Blocks.Add(new WikiBlock(WikiBlockKind.FactSheet, planet.FactsDe, planet.FactsEn)
                        {
                            Material = image,
                            ImageSource = planet.ImageSource,
                            Width = 240f,
                            Height = 240f,
                        });
                    }
                    page.Blocks.Add(WikiData.AutoTable("PlanetData", null, planet.Id));
                    page.Heading("Erze", "Ores");
                    page.Blocks.Add(WikiData.AutoList("OresPerPlanet", planet.Id));
                }
            }

            _planets = ReadPlanets(data);
            _catalog = BuildCatalog(data, _planets);
            CreateOrePages(data);
            CreateComponentPages(data);
        }

        /// <summary>The page with this id: the hand-written one (generated blocks are appended) or a new one.</summary>
        private static WikiPage PageFor(WikiData data, string id, string parent, int order, string titleDe, string titleEn, string source)
        {
            WikiPage page;
            if (data.Pages.TryGetValue(id, out page))
                return page;
            page = new WikiPage { Id = id, Parent = parent ?? "", Order = order, TitleDe = titleDe, TitleEn = titleEn, Source = source ?? "Syntrix" };
            data.Pages[id] = page;
            return page;
        }

        /// <summary>Public definitions, plus &lt;Show&gt;, minus &lt;Hide&gt;.</summary>
        public static bool Visible(WikiData data, MyDefinitionId id, bool isPublic)
        {
            var key = Key(id);
            if (data.Shown.Contains(key))
                return true;
            return isPublic && !data.Hidden.Contains(key);
        }

        /// <summary>"Type/Subtype" without "MyObjectBuilder_" (as in &lt;Hide&gt; and &lt;Show&gt;).</summary>
        public static string Key(MyDefinitionId id)
        {
            var type = id.TypeId.ToString();
            if (type.StartsWith("MyObjectBuilder_", StringComparison.Ordinal))
                type = type.Substring("MyObjectBuilder_".Length);
            return type + "/" + id.SubtypeName;
        }

        /// <summary>Icon of a definition as the window draws it (definition id), null if the item does not exist.</summary>
        public static string[] Icon(MyDefinitionId id) => ItemExists(id) ? new[] { id.ToString() } : null;

        // ---------- filling tables ----------

        /// <summary>
        /// Fills one automatic table or list. False = the Auto name is not one of Syntrix (a mod may provide it);
        /// errors go to data.Errors. An empty result stays empty, the window shows "No entries".
        /// </summary>
        public static bool FillOne(WikiData data, WikiPage page, WikiBlock block)
        {
            if (_planets == null)
                _planets = ReadPlanets(data);
            if (_catalog == null)
                _catalog = BuildCatalog(data, _planets);
            bool list = block.Kind == WikiBlockKind.List;
            try
            {
                if (list ? FillList(data, page, block) : FillTable(data, page, block, _planets))
                    return true;
                return list ? WikiFood.FillList(data, block) : WikiFood.FillTable(data, block);
            }
            catch (Exception e)
            {
                data.Errors.Add($"Page '{page.Id}': {(list ? "List" : "Table")} Auto=\"{block.Auto}\" not generated: {e.Message}");
                return true;
            }
        }

        /// <summary>Forgets the planets and the catalog (next build reads the definitions again).</summary>
        public static void Reset()
        {
            _planets = null;
            _catalog = null;
        }

        private static bool FillTable(WikiData data, WikiPage page, WikiBlock block, Dictionary<string, MyPlanetGeneratorDefinition> planets)
        {
            switch (block.Auto.ToLowerInvariant())
            {
                case "ores":
                    OreTable(data, block);
                    break;
                case "components":
                    ComponentTable(data, block);
                    break;
                case "planets":
                    PlanetTable(data, block, planets);
                    break;
                case "oresperplanet":
                    if (!string.IsNullOrEmpty(block.AutoPlanet))
                        OresOfPlanet(data, page, block, planets);
                    else
                        OresBySystem(data, block, planets);
                    break;
                case "planetdata":
                    PlanetDataTable(data, page, block, planets);
                    break;
                default:
                    return false;
            }
            return true;
        }

        private static void Header(WikiBlock block, string de, string en, string widths = null)
        {
            block.HeaderDe = de.Split('|');
            block.HeaderEn = en.Split('|');
            if (block.Widths == null && widths != null)
            {
                var parts = widths.Split('|');
                block.Widths = new float[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                    block.Widths[i] = float.Parse(parts[i], CultureInfo.InvariantCulture);
            }
        }

        private static void Row(WikiBlock block, string[] de, string[] en)
        {
            block.RowsDe.Add(de);
            block.RowsEn.Add(en);
        }

        /// <summary>InvariantCulture only (safe in Keen's script check); for German the comma is set by hand.</summary>
        public static string Number(double value, string format, bool german)
        {
            var s = value.ToString(format, CultureInfo.InvariantCulture);
            return german ? s.Replace('.', ',') : s;
        }

        // ---------- names ----------

        /// <summary>Display name of an item, else its subtype.</summary>
        public static string ItemName(MyDefinitionId id)
        {
            try
            {
                var item = MyDefinitionManager.Static.GetPhysicalItemDefinition(id);
                if (item != null && !string.IsNullOrEmpty(item.DisplayNameText))
                    return item.DisplayNameText;
            }
            catch (Exception)
            {
            }
            return id.SubtypeName;
        }

        private static bool IsType(MyDefinitionId id, string type) =>
            id.TypeId.ToString().EndsWith("_" + type, StringComparison.Ordinal);

        private static MyPhysicalItemDefinition Item(MyDefinitionId id)
        {
            try
            {
                return MyDefinitionManager.Static.GetPhysicalItemDefinition(id);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static bool ItemExists(MyDefinitionId id) => Item(id) != null;

        /// <summary>The item exists and the generated lists show it.</summary>
        private static bool ItemVisible(WikiData data, MyDefinitionId id)
        {
            var item = Item(id);
            return item != null && Visible(data, id, item.Public);
        }

        // ---------- ores (refinery) ----------

        private class OreRow
        {
            public string Ore;
            public readonly List<string> ResultDe = new List<string>();
            public readonly List<string> ResultEn = new List<string>();
            public readonly List<string> Blocks = new List<string>();
        }

        private static void OreTable(WikiData data, WikiBlock block)
        {
            Header(block, "Erz|Ergebnis je 1 kg Erz|Wo", "Ore|Result per 1 kg of ore|Where", "2|3|3");
            // Blueprints of the refineries (and of all production blocks that turn ore into ingots)
            var rows = new Dictionary<string, OreRow>();
            var known = new HashSet<string>();
            foreach (var def in MyDefinitionManager.Static.GetAllDefinitions())
            {
                var production = def as MyProductionBlockDefinition;
                if (production == null || !production.Public || production.BlueprintClasses == null)
                    continue;
                foreach (var blueprintClass in production.BlueprintClasses)
                    foreach (var bp in blueprintClass)
                    {
                        if (bp == null || !bp.Public || bp.Prerequisites == null || bp.Results == null
                            || bp.Prerequisites.Length != 1 || !IsType(bp.Prerequisites[0].Id, "Ore") || bp.Results.Length == 0)
                            continue;
                        bool ingots = true;
                        foreach (var r in bp.Results)
                            ingots &= IsType(r.Id, "Ingot");
                        var oreId = bp.Prerequisites[0].Id;
                        if (!ingots || !ItemVisible(data, oreId))
                            continue;
                        var oreKey = oreId.SubtypeName;
                        OreRow row;
                        if (!rows.TryGetValue(oreKey, out row))
                            rows[oreKey] = row = new OreRow { Ore = ItemName(oreId) };
                        var blockName = production.DisplayNameText ?? production.Id.SubtypeName;
                        if (!row.Blocks.Contains(blockName))
                            row.Blocks.Add(blockName);
                        // Enter each blueprint only once, with the yield of the best refinery
                        var key = bp.Id.SubtypeName;
                        if (!known.Add(key))
                            continue;
                        float amount = (float)bp.Prerequisites[0].Amount;
                        if (amount <= 0f)
                            continue;
                        float factor = bp.IgnoreEfficiencyMultiplier ? 1f : BestEfficiency(bp);
                        var de = new List<string>();
                        var en = new List<string>();
                        foreach (var r in bp.Results)
                        {
                            double kg = (float)r.Amount / amount * factor;
                            de.Add($"{Number(kg, "0.###", true)} kg {ItemName(r.Id)}");
                            en.Add($"{Number(kg, "0.###", false)} kg {ItemName(r.Id)}");
                        }
                        row.ResultDe.Add(string.Join(", ", de));
                        row.ResultEn.Add(string.Join(", ", en));
                    }
            }
            var list = new List<OreRow>(rows.Values);
            list.Sort((x, y) => string.Compare(x.Ore, y.Ore, StringComparison.CurrentCultureIgnoreCase));
            foreach (var row in list)
            {
                row.Blocks.Sort((x, y) => string.Compare(x, y, StringComparison.CurrentCultureIgnoreCase));
                var where = string.Join(", ", row.Blocks);
                Row(block, new[] { row.Ore, string.Join(", ", row.ResultDe), where }, new[] { row.Ore, string.Join(", ", row.ResultEn), where });
            }
        }

        /// <summary>Highest material efficiency among the refineries that can run this blueprint.</summary>
        private static float BestEfficiency(MyBlueprintDefinitionBase bp)
        {
            float best = 0f;
            foreach (var def in MyDefinitionManager.Static.GetAllDefinitions())
            {
                var production = def as MyProductionBlockDefinition;
                if (production == null || !production.Public || production.BlueprintClasses == null)
                    continue;
                foreach (var blueprintClass in production.BlueprintClasses)
                    if (blueprintClass.ContainsBlueprint(bp))
                    {
                        var refinery = production as MyRefineryDefinition;
                        best = Math.Max(best, refinery != null ? refinery.MaterialEfficiency : 1f);
                        break;
                    }
            }
            return best > 0f ? best : 1f;
        }

        // ---------- components (assembler) ----------

        private static void ComponentTable(WikiData data, WikiBlock block)
        {
            Header(block, "Bauteil|Kosten für 1 Stück", "Component|Cost for 1 piece", "2|5");
            var done = new Dictionary<string, string[]>();
            var names = new Dictionary<string, string>();
            foreach (var def in MyDefinitionManager.Static.GetAllDefinitions())
            {
                var assembler = def as MyAssemblerDefinition;
                if (assembler == null || !assembler.Public || assembler.BlueprintClasses == null)
                    continue;
                foreach (var blueprintClass in assembler.BlueprintClasses)
                    foreach (var bp in blueprintClass)
                    {
                        if (bp == null || !bp.Public || bp.Results == null || bp.Results.Length != 1 || bp.Prerequisites == null
                            || !IsType(bp.Results[0].Id, "Component"))
                            continue;
                        var id = bp.Results[0].Id;
                        if (done.ContainsKey(id.SubtypeName) || !ItemVisible(data, id))
                            continue;
                        float pieces = (float)bp.Results[0].Amount;
                        if (pieces <= 0f)
                            continue;
                        var de = new List<string>();
                        var en = new List<string>();
                        foreach (var p in bp.Prerequisites)
                        {
                            double amount = (float)p.Amount / pieces;
                            bool kg = IsType(p.Id, "Ingot") || IsType(p.Id, "Ore");
                            var name = ItemName(p.Id);
                            de.Add(kg ? $"{Number(amount, "0.##", true)} kg {name}" : $"{Number(amount, "0.##", true)}× {name}");
                            en.Add(kg ? $"{Number(amount, "0.##", false)} kg {name}" : $"{Number(amount, "0.##", false)}× {name}");
                        }
                        done[id.SubtypeName] = new[] { string.Join(", ", de), string.Join(", ", en) };
                        names[id.SubtypeName] = ItemName(id);
                    }
            }
            var keys = new List<string>(done.Keys);
            keys.Sort((x, y) => string.Compare(names[x], names[y], StringComparison.CurrentCultureIgnoreCase));
            foreach (var k in keys)
                Row(block, new[] { names[k], done[k][0] }, new[] { names[k], done[k][1] });
        }

        // ---------- planets ----------

        private static Dictionary<string, MyPlanetGeneratorDefinition> ReadPlanets(WikiData data)
        {
            var all = new Dictionary<string, MyPlanetGeneratorDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var g in MyDefinitionManager.Static.GetPlanetsGeneratorsDefinitions())
                if (g != null)
                    all[g.Id.SubtypeName] = g;
            var result = new Dictionary<string, MyPlanetGeneratorDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in data.Planets.Values)
            {
                MyPlanetGeneratorDefinition g;
                if (all.TryGetValue(p.Definition, out g))
                    result[p.Id] = g;
                else
                    data.Errors.Add($"Planet '{p.Id}': definition '{p.Definition}' not found (mod not loaded?)");
            }
            return result;
        }

        private static WikiSystem FindSystem(WikiData data, string id)
        {
            foreach (var s in data.Systems)
                if (string.Equals(s.Id, id, StringComparison.OrdinalIgnoreCase))
                    return s;
            return null;
        }

        private static string Temperature(MyTemperatureLevel t, bool german)
        {
            switch (t)
            {
                case MyTemperatureLevel.ExtremeFreeze:
                    return german ? "eisig" : "freezing";
                case MyTemperatureLevel.Freeze:
                    return german ? "kalt" : "cold";
                case MyTemperatureLevel.Cozy:
                    return german ? "gemäßigt" : "temperate";
                case MyTemperatureLevel.Hot:
                    return german ? "heiß" : "hot";
                case MyTemperatureLevel.ExtremeHot:
                    return german ? "glühend heiß" : "scorching";
            }
            return "–";
        }

        private static string Atmosphere(MyPlanetGeneratorDefinition g, bool german)
        {
            if (!g.HasAtmosphere)
                return german ? "keine" : "none";
            return g.Atmosphere.Breathable ? (german ? "atembar" : "breathable") : (german ? "nicht atembar" : "not breathable");
        }

        private static string Gravity(MyPlanetGeneratorDefinition g, bool german) => Number(g.SurfaceGravity, "0.##", german) + " g";

        /// <summary>
        /// Wiki planet for a part of the definition subtype (e.g. "Earth" finds "Sol-Earth"), plus the definition from the
        /// game. Exact name before part of the name. null if there is no matching planet.
        /// </summary>
        public static WikiPlanet PlanetForPart(WikiData data, string part, out MyPlanetGeneratorDefinition generator)
        {
            generator = null;
            if (data == null || string.IsNullOrWhiteSpace(part))
                return null;
            WikiPlanet match = null;
            foreach (var p in data.Planets.Values)
            {
                if (string.IsNullOrEmpty(p.Definition))
                    continue;
                if (string.Equals(p.Definition, part, StringComparison.OrdinalIgnoreCase))
                {
                    match = p;
                    break;
                }
                if (match == null && p.Definition.IndexOf(part, StringComparison.OrdinalIgnoreCase) >= 0)
                    match = p;
            }
            if (match == null)
                return null;
            try
            {
                foreach (var x in MyDefinitionManager.Static.GetPlanetsGeneratorsDefinitions())
                    if (x != null && string.Equals(x.Id.SubtypeName, match.Definition, StringComparison.OrdinalIgnoreCase))
                        generator = x;
            }
            catch (Exception)
            {
                generator = null;
            }
            return match;
        }

        /// <summary>Short line: gravity, atmosphere, temperature ("" without a definition).</summary>
        public static string PlanetSummary(MyPlanetGeneratorDefinition g, bool german)
        {
            if (g == null)
                return "";
            return (german ? "Schwerkraft " : "Gravity ") + Gravity(g, german)
                + "   ·   " + (german ? "Atmosphäre " : "Atmosphere ") + Atmosphere(g, german)
                + "   ·   " + (german ? "Temperatur " : "Temperature ") + Temperature(g.DefaultSurfaceTemperature, german);
        }

        /// <summary>Planet name as a link to its page (moons indented).</summary>
        private static string PlanetLink(WikiPlanet p, bool german) =>
            (string.IsNullOrEmpty(p.Parent) ? "" : "   ") + "[[" + p.Id + "|" + p.Name(german) + "]]";

        private static void PlanetTable(WikiData data, WikiBlock block, Dictionary<string, MyPlanetGeneratorDefinition> planets)
        {
            Header(block, "Körper|Schwerkraft|Atmosphäre|Temperatur", "Body|Gravity|Atmosphere|Temperature", "3|2|2|2");
            var system = FindSystem(data, block.AutoSystem);
            if (system == null)
            {
                data.Errors.Add($"Table Planets: unknown system '{block.AutoSystem}'");
                return;
            }
            foreach (var p in InOrder(system))
            {
                MyPlanetGeneratorDefinition g;
                if (!planets.TryGetValue(p.Id, out g))
                    continue;
                Row(block,
                    new[] { PlanetLink(p, true), Gravity(g, true), Atmosphere(g, true), Temperature(g.DefaultSurfaceTemperature, true) },
                    new[] { PlanetLink(p, false), Gravity(g, false), Atmosphere(g, false), Temperature(g.DefaultSurfaceTemperature, false) });
            }
        }

        /// <summary>Planets in list order, every moon directly below its planet.</summary>
        private static List<WikiPlanet> InOrder(WikiSystem system)
        {
            var list = new List<WikiPlanet>();
            foreach (var p in system.Planets)
            {
                if (!string.IsNullOrEmpty(p.Parent))
                    continue;
                list.Add(p);
                foreach (var moon in system.Planets)
                    if (string.Equals(moon.Parent, p.Id, StringComparison.OrdinalIgnoreCase))
                        list.Add(moon);
            }
            // Moons whose planet is missing go to the end
            foreach (var p in system.Planets)
                if (!list.Contains(p))
                    list.Add(p);
            return list;
        }

        private class OreDeposit
        {
            public string Subtype;
            public string Name;
            public float From = float.MaxValue;
            public float To;
        }

        /// <summary>Ores of a planet from the OreMappings: voxel material to mined ore, shallowest depth.</summary>
        private static List<OreDeposit> Ores(MyPlanetGeneratorDefinition g)
        {
            var byOre = new Dictionary<string, OreDeposit>();
            if (g.OreMappings != null)
                foreach (var m in g.OreMappings)
                {
                    if (m == null || string.IsNullOrEmpty(m.Type))
                        continue;
                    MyVoxelMaterialDefinition voxel = null;
                    try
                    {
                        voxel = MyDefinitionManager.Static.GetVoxelMaterialDefinition(m.Type);
                    }
                    catch (Exception)
                    {
                    }
                    var ore = voxel != null && !string.IsNullOrEmpty(voxel.MinedOre) ? voxel.MinedOre : m.Type;
                    OreDeposit deposit;
                    if (!byOre.TryGetValue(ore, out deposit))
                        byOre[ore] = deposit = new OreDeposit { Subtype = ore, Name = ItemName(new MyDefinitionId(typeof(MyObjectBuilder_Ore), ore)) };
                    deposit.From = Math.Min(deposit.From, m.Start);
                    deposit.To = Math.Max(deposit.To, m.Start + m.Depth);
                }
            var list = new List<OreDeposit>(byOre.Values);
            list.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.CurrentCultureIgnoreCase));
            return list;
        }

        private static string OreLink(WikiData data, OreDeposit e) =>
            data.Pages.ContainsKey(OrePage(e.Subtype)) ? "[[" + OrePage(e.Subtype) + "|" + e.Name + "]]" : e.Name;

        private static void OresBySystem(WikiData data, WikiBlock block, Dictionary<string, MyPlanetGeneratorDefinition> planets)
        {
            Header(block, "Körper|Erze", "Body|Ores", "2|6");
            var system = FindSystem(data, block.AutoSystem);
            if (system == null)
            {
                data.Errors.Add($"Table OresPerPlanet: unknown system '{block.AutoSystem}'");
                return;
            }
            foreach (var p in InOrder(system))
            {
                MyPlanetGeneratorDefinition g;
                if (!planets.TryGetValue(p.Id, out g))
                    continue;
                var names = new List<string>();
                foreach (var e in Ores(g))
                    names.Add(OreLink(data, e));
                var list = names.Count > 0 ? string.Join(", ", names) : "–";
                Row(block, new[] { PlanetLink(p, true), list }, new[] { PlanetLink(p, false), list });
            }
        }

        private static void OresOfPlanet(WikiData data, WikiPage page, WikiBlock block, Dictionary<string, MyPlanetGeneratorDefinition> planets)
        {
            Header(block, "Erz|Tiefe unter der Oberfläche", "Ore|Depth below the surface", "3|3");
            MyPlanetGeneratorDefinition g;
            if (!planets.TryGetValue(block.AutoPlanet, out g))
            {
                if (!data.Planets.ContainsKey(block.AutoPlanet))
                    data.Errors.Add($"Page '{page.Id}': planet '{block.AutoPlanet}' is not in any system list");
                return;
            }
            foreach (var e in Ores(g))
                Row(block,
                    new[] { e.Name, $"{Number(e.From, "0", true)} bis {Number(e.To, "0", true)} m" },
                    new[] { e.Name, $"{Number(e.From, "0", false)} to {Number(e.To, "0", false)} m" });
        }

        private static void PlanetDataTable(WikiData data, WikiPage page, WikiBlock block, Dictionary<string, MyPlanetGeneratorDefinition> planets)
        {
            Header(block, "Eigenschaft|Wert", "Property|Value", "2|3");
            MyPlanetGeneratorDefinition g;
            if (!planets.TryGetValue(block.AutoPlanet, out g))
            {
                if (!data.Planets.ContainsKey(block.AutoPlanet))
                    data.Errors.Add($"Page '{page.Id}': planet '{block.AutoPlanet}' is not in any system list");
                return;
            }
            Row(block, new[] { "Schwerkraft an der Oberfläche", Gravity(g, true) }, new[] { "Surface gravity", Gravity(g, false) });
            Row(block, new[] { "Atmosphäre", Atmosphere(g, true) }, new[] { "Atmosphere", Atmosphere(g, false) });
            if (g.HasAtmosphere && g.Atmosphere.MaxWindSpeed > 0f)
                Row(block,
                    new[] { "Wind bis", Number(g.Atmosphere.MaxWindSpeed, "0", true) + " m/s" },
                    new[] { "Wind up to", Number(g.Atmosphere.MaxWindSpeed, "0", false) + " m/s" });
            Row(block, new[] { "Temperatur", Temperature(g.DefaultSurfaceTemperature, true) }, new[] { "Temperature", Temperature(g.DefaultSurfaceTemperature, false) });
            WikiPlanet p;
            if (data.Planets.TryGetValue(block.AutoPlanet, out p))
            {
                var moonsDe = new List<string>();
                var moonsEn = new List<string>();
                foreach (var m in data.Planets.Values)
                    if (string.Equals(m.Parent, p.Id, StringComparison.OrdinalIgnoreCase))
                    {
                        moonsDe.Add("[[" + m.Id + "|" + m.Name(true) + "]]");
                        moonsEn.Add("[[" + m.Id + "|" + m.Name(false) + "]]");
                    }
                if (moonsDe.Count > 0)
                    Row(block, new[] { "Monde", string.Join(", ", moonsDe) }, new[] { "Moons", string.Join(", ", moonsEn) });
                if (!string.IsNullOrEmpty(p.Parent))
                {
                    WikiPlanet orbited;
                    if (data.Planets.TryGetValue(p.Parent, out orbited))
                        Row(block,
                            new[] { "Kreist um", "[[" + orbited.Id + "|" + orbited.Name(true) + "]]" },
                            new[] { "Orbits", "[[" + orbited.Id + "|" + orbited.Name(false) + "]]" });
                }
            }
        }

        // ---------- catalog: ores, ingots, components and who needs what ----------

        /// <summary>Page id of an ore.</summary>
        public static string OrePage(string subtype) => "ore-" + subtype.ToLowerInvariant();

        /// <summary>Page id of a component.</summary>
        public static string ComponentPage(string subtype) => "component-" + subtype.ToLowerInvariant();

        private struct Amount
        {
            public MyDefinitionId Id;
            public double Value;
        }

        private class OreInfo
        {
            public string Subtype;
            public string Name;
            public string[] Icons;
            public readonly List<Amount> Ingots = new List<Amount>();
            public readonly List<string> Blocks = new List<string>();
            public readonly HashSet<string> UsedFor = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        private class ComponentInfo
        {
            public string Subtype;
            public string Name;
            public string[] Icons;
            public readonly List<Amount> Cost = new List<Amount>();
            public readonly List<string> Blocks = new List<string>();
            public readonly HashSet<string> UsedFor = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        private class Catalog
        {
            public WikiData Data;
            public readonly Dictionary<string, OreInfo> Ores = new Dictionary<string, OreInfo>(StringComparer.OrdinalIgnoreCase);
            public readonly Dictionary<string, ComponentInfo> Components = new Dictionary<string, ComponentInfo>(StringComparer.OrdinalIgnoreCase);

            /// <summary>Ingot subtype to the ores the refinery makes it from.</summary>
            public readonly Dictionary<string, List<string>> IngotFrom = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

            /// <summary>The ore if it exists and the generated lists show it, else null.</summary>
            public OreInfo Ore(string subtype)
            {
                OreInfo ore;
                if (Ores.TryGetValue(subtype, out ore))
                    return ore;
                var id = new MyDefinitionId(typeof(MyObjectBuilder_Ore), subtype);
                if (!ItemVisible(Data, id))
                    return null;
                return Ores[subtype] = new OreInfo { Subtype = subtype, Name = ItemName(id), Icons = Icon(id) };
            }

            /// <summary>Ore whose page an ingot links to: same name preferred (iron before LQ iron).</summary>
            public string MainSource(string ingot)
            {
                List<string> ores;
                if (!IngotFrom.TryGetValue(ingot, out ores) || ores.Count == 0)
                    return null;
                foreach (var ore in ores)
                    if (string.Equals(ore, ingot, StringComparison.OrdinalIgnoreCase))
                        return ore;
                return ores[0];
            }
        }

        private static void AddOnce(List<string> list, string s)
        {
            if (!string.IsNullOrEmpty(s) && !list.Contains(s))
                list.Add(s);
        }

        private static Catalog BuildCatalog(WikiData data, Dictionary<string, MyPlanetGeneratorDefinition> planets)
        {
            var catalog = new Catalog { Data = data };
            foreach (var def in MyDefinitionManager.Static.GetAllDefinitions())
            {
                var production = def as MyProductionBlockDefinition;
                if (production == null || !production.Public || production.BlueprintClasses == null)
                    continue;
                var blockName = production.DisplayNameText ?? production.Id.SubtypeName;
                bool isAssembler = production is MyAssemblerDefinition;
                foreach (var blueprintClass in production.BlueprintClasses)
                    foreach (var bp in blueprintClass)
                    {
                        if (bp == null || !bp.Public || bp.Prerequisites == null || bp.Results == null || bp.Results.Length == 0)
                            continue;
                        if (!isAssembler && bp.Prerequisites.Length == 1 && IsType(bp.Prerequisites[0].Id, "Ore"))
                        {
                            bool ingots = true;
                            foreach (var r in bp.Results)
                                ingots &= IsType(r.Id, "Ingot");
                            float amount = (float)bp.Prerequisites[0].Amount;
                            var ore = ingots && amount > 0f ? catalog.Ore(bp.Prerequisites[0].Id.SubtypeName) : null;
                            if (ore == null)
                                continue;
                            AddOnce(ore.Blocks, blockName);
                            float factor = bp.IgnoreEfficiencyMultiplier ? 1f : BestEfficiency(bp);
                            foreach (var r in bp.Results)
                            {
                                double kg = (float)r.Amount / amount * factor;
                                int i = ore.Ingots.FindIndex(m => m.Id == r.Id);
                                if (i < 0)
                                    ore.Ingots.Add(new Amount { Id = r.Id, Value = kg });
                                else if (kg > ore.Ingots[i].Value)
                                    ore.Ingots[i] = new Amount { Id = r.Id, Value = kg };
                                List<string> from;
                                if (!catalog.IngotFrom.TryGetValue(r.Id.SubtypeName, out from))
                                    catalog.IngotFrom[r.Id.SubtypeName] = from = new List<string>();
                                AddOnce(from, ore.Subtype);
                            }
                            continue;
                        }
                        if (isAssembler && bp.Results.Length == 1 && IsType(bp.Results[0].Id, "Component"))
                        {
                            var id = bp.Results[0].Id;
                            float pieces = (float)bp.Results[0].Amount;
                            if (pieces <= 0f || !ItemVisible(data, id))
                                continue;
                            ComponentInfo component;
                            if (!catalog.Components.TryGetValue(id.SubtypeName, out component))
                            {
                                catalog.Components[id.SubtypeName] = component = new ComponentInfo { Subtype = id.SubtypeName, Name = ItemName(id), Icons = Icon(id) };
                                foreach (var p in bp.Prerequisites)
                                    component.Cost.Add(new Amount { Id = p.Id, Value = (float)p.Amount / pieces });
                            }
                            AddOnce(component.Blocks, blockName);
                        }
                    }
            }

            // Who needs what: component to ingot to ores, component to component
            foreach (var component in catalog.Components.Values)
                foreach (var m in component.Cost)
                {
                    if (IsType(m.Id, "Ingot"))
                    {
                        List<string> from;
                        if (catalog.IngotFrom.TryGetValue(m.Id.SubtypeName, out from))
                            foreach (var ore in from)
                                catalog.Ores[ore].UsedFor.Add(component.Subtype);
                    }
                    else if (IsType(m.Id, "Ore"))
                        catalog.Ore(m.Id.SubtypeName)?.UsedFor.Add(component.Subtype);
                    else if (IsType(m.Id, "Component"))
                    {
                        ComponentInfo other;
                        if (catalog.Components.TryGetValue(m.Id.SubtypeName, out other))
                            other.UsedFor.Add(component.Subtype);
                    }
                }

            // Ores that only occur on planets (ice) get a page as well
            foreach (var g in planets.Values)
                foreach (var deposit in Ores(g))
                    catalog.Ore(deposit.Subtype);
            return catalog;
        }

        private static string ParentIfExists(WikiData data, string id) => data.Pages.ContainsKey(id) ? id : "";

        private static List<T> ByName<T>(IEnumerable<T> items, Func<T, string> name)
        {
            var result = new List<T>(items);
            result.Sort((x, y) => string.Compare(name(x), name(y), StringComparison.CurrentCultureIgnoreCase));
            return result;
        }

        private static WikiEntry Entry(string target, string name, string[] icons, string extraDe = null, string extraEn = null) =>
            new WikiEntry { Target = target, De = name, En = name, Icons = icons, ExtraDe = extraDe, ExtraEn = extraEn };

        private static WikiEntry ItemEntry(MyDefinitionId id, string self, string extraDe = null, string extraEn = null)
        {
            string target = null;
            if (IsType(id, "Ore") && _catalog.Ores.ContainsKey(id.SubtypeName))
                target = OrePage(id.SubtypeName);
            else if (IsType(id, "Component") && _catalog.Components.ContainsKey(id.SubtypeName))
                target = ComponentPage(id.SubtypeName);
            else if (IsType(id, "Ingot"))
            {
                var ore = _catalog.MainSource(id.SubtypeName);
                if (ore != null)
                    target = OrePage(ore);
            }
            if (string.Equals(target, self, StringComparison.OrdinalIgnoreCase))
                target = null;
            return Entry(target, ItemName(id), Icon(id), extraDe, extraEn);
        }

        private static List<WikiEntry> ComponentEntries(IEnumerable<string> subtypes)
        {
            var list = new List<WikiEntry>();
            foreach (var c in ByName(subtypes, s => _catalog.Components[s].Name))
                list.Add(Entry(ComponentPage(c), _catalog.Components[c].Name, _catalog.Components[c].Icons));
            return list;
        }

        private static string BlockList(List<string> blocks)
        {
            var sorted = new List<string>(blocks);
            sorted.Sort((x, y) => string.Compare(x, y, StringComparison.CurrentCultureIgnoreCase));
            return string.Join(", ", sorted);
        }

        private static void CreateOrePages(WikiData data)
        {
            var parent = ParentIfExists(data, "ores");
            int order = 0;
            foreach (var ore in ByName(_catalog.Ores.Values, x => x.Name))
            {
                var id = OrePage(ore.Subtype);
                order += 10;
                var page = PageFor(data, id, parent, 1000 + order, ore.Name, ore.Name, "Syntrix");
                if (page.Icons == null)
                    page.Icons = ore.Icons;

                page.Heading("Raffinerie", "Refinery");
                if (ore.Ingots.Count > 0)
                {
                    var list = new List<WikiEntry>();
                    foreach (var m in ore.Ingots)
                        list.Add(ItemEntry(m.Id, id, Number(m.Value, "0.###", true) + " kg je kg Erz", Number(m.Value, "0.###", false) + " kg per kg of ore"));
                    page.Blocks.Add(WikiData.EntryList(list, 2));
                    var where = BlockList(ore.Blocks);
                    page.Text("Verarbeitet in: " + where + ". Die Menge gilt für die beste dieser Raffinerien.",
                              "Processed in: " + where + ". The amount applies to the best of these refineries.");
                }
                else
                    page.Text("Dieses Erz wird nicht in der Raffinerie verarbeitet.", "This ore is not processed in the refinery.");

                page.Heading("Vorkommen", "Where to find it");
                var deposits = new List<WikiEntry>();
                foreach (var system in data.Systems)
                    foreach (var p in InOrder(system))
                    {
                        MyPlanetGeneratorDefinition g;
                        if (!_planets.TryGetValue(p.Id, out g))
                            continue;
                        foreach (var v in Ores(g))
                            if (string.Equals(v.Subtype, ore.Subtype, StringComparison.OrdinalIgnoreCase))
                                deposits.Add(new WikiEntry
                                {
                                    Target = p.Id,
                                    De = p.Name(true),
                                    En = p.Name(false),
                                    ExtraDe = $"{Number(v.From, "0", true)} bis {Number(v.To, "0", true)} m",
                                    ExtraEn = $"{Number(v.From, "0", false)} to {Number(v.To, "0", false)} m",
                                });
                    }
                if (deposits.Count > 0)
                    page.Blocks.Add(WikiData.EntryList(deposits, 3));
                else
                    page.Text("Auf keinem Planeten oder Mond der geladenen Planetensysteme.", "On no planet or moon of the loaded planetary systems.");

                if (ore.UsedFor.Count > 0)
                {
                    page.Heading("Gebraucht für", "Used for");
                    page.Blocks.Add(WikiData.EntryList(ComponentEntries(ore.UsedFor), 3));
                }
            }
        }

        private static void CreateComponentPages(WikiData data)
        {
            var parent = ParentIfExists(data, "components");
            int order = 0;
            foreach (var component in ByName(_catalog.Components.Values, x => x.Name))
            {
                var id = ComponentPage(component.Subtype);
                order += 10;
                var page = PageFor(data, id, parent, 1000 + order, component.Name, component.Name, "Syntrix");
                if (page.Icons == null)
                    page.Icons = component.Icons;

                page.Heading("Herstellung", "Production");
                var list = new List<WikiEntry>();
                foreach (var m in component.Cost)
                {
                    bool kg = IsType(m.Id, "Ingot") || IsType(m.Id, "Ore");
                    list.Add(ItemEntry(m.Id, id,
                        kg ? Number(m.Value, "0.##", true) + " kg" : Number(m.Value, "0.##", true) + " Stück",
                        kg ? Number(m.Value, "0.##", false) + " kg" : Number(m.Value, "0.##", false) + " pcs"));
                }
                page.Blocks.Add(WikiData.EntryList(list, 2));
                var where = BlockList(component.Blocks);
                page.Text("Hergestellt in: " + where + ". Die Mengen gelten für ein Stück, ohne die Assembler-Effizienz des Servers.",
                          "Made in: " + where + ". The amounts are for one piece, without the server's assembler efficiency.");

                if (component.UsedFor.Count > 0)
                {
                    page.Heading("Gebraucht für", "Used for");
                    page.Blocks.Add(WikiData.EntryList(ComponentEntries(component.UsedFor), 3));
                }
            }
        }

        private static bool FillList(WikiData data, WikiPage page, WikiBlock block)
        {
            switch (block.Auto.ToLowerInvariant())
            {
                case "ores":
                    foreach (var ore in ByName(_catalog.Ores.Values, x => x.Name))
                        if (data.Pages.ContainsKey(OrePage(ore.Subtype)))
                            block.Entries.Add(Entry(OrePage(ore.Subtype), ore.Name, ore.Icons));
                    break;
                case "components":
                    foreach (var component in ByName(_catalog.Components.Values, y => y.Name))
                        if (data.Pages.ContainsKey(ComponentPage(component.Subtype)))
                            block.Entries.Add(Entry(ComponentPage(component.Subtype), component.Name, component.Icons));
                    break;
                case "oresperplanet":
                    MyPlanetGeneratorDefinition g;
                    if (string.IsNullOrEmpty(block.AutoPlanet) || !_planets.TryGetValue(block.AutoPlanet, out g))
                    {
                        if (string.IsNullOrEmpty(block.AutoPlanet) || !data.Planets.ContainsKey(block.AutoPlanet))
                            data.Errors.Add($"Page '{page.Id}': List OresPerPlanet needs Planet= from a system list");
                        return true;
                    }
                    foreach (var v in Ores(g))
                    {
                        var ore = _catalog.Ore(v.Subtype);
                        block.Entries.Add(Entry(ore != null && data.Pages.ContainsKey(OrePage(v.Subtype)) ? OrePage(v.Subtype) : null, v.Name, ore?.Icons,
                            $"{Number(v.From, "0", true)} bis {Number(v.To, "0", true)} m", $"{Number(v.From, "0", false)} to {Number(v.To, "0", false)} m"));
                    }
                    break;
                default:
                    return false;
            }
            return true;
        }
    }
}
