// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Syntrix.Wiki
{
    /// <summary>
    /// All pages of the wiki, merged from the Data/Syntrix-Wiki.xml of all loaded mods and the pages of generating
    /// mods (Wiki.AddSource). No game dependencies, so it can be tested outside the game.
    /// </summary>
    public class WikiData
    {
        /// <summary>Id of the start page.</summary>
        public const string StartId = "start";
        /// <summary>Id of the page that lists the errors.</summary>
        public const string ErrorId = "wiki-error";

        /// <summary>Known symbols; other names are reported and left out.</summary>
        public static readonly HashSet<string> Symbols = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "home", "core", "cube", "planet", "shield", "ore", "tool", "gear",
            "book", "info", "warning", "component", "research", "star",
        };

        /// <summary>Pages by id.</summary>
        public readonly Dictionary<string, WikiPage> Pages = new Dictionary<string, WikiPage>(StringComparer.OrdinalIgnoreCase);
        /// <summary>Errors found while reading, shown on the error page.</summary>
        public readonly List<string> Errors = new List<string>();

        /// <summary>Log only, no error page (e.g. an addition for a page of a mod that is not loaded).</summary>
        public readonly List<string> Notices = new List<string>();

        /// <summary>Check outside the game with all mods: notices count as errors as well.</summary>
        public bool Strict;

        /// <summary>Wiki files that were read.</summary>
        public readonly List<string> Sources = new List<string>();
        /// <summary>Planetary systems in file order.</summary>
        public readonly List<WikiSystem> Systems = new List<WikiSystem>();
        /// <summary>Planets and moons of all systems by id.</summary>
        public readonly Dictionary<string, WikiPlanet> Planets = new Dictionary<string, WikiPlanet>(StringComparer.OrdinalIgnoreCase);

        /// <summary>System lists as read, with their mod in Source (context for generating mods).</summary>
        public readonly List<SystemXml> SystemSources = new List<SystemXml>();

        /// <summary>Definitions ("Type/Subtype") the generated lists leave out (&lt;Hide&gt;).</summary>
        public readonly HashSet<string> Hidden = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Hidden definitions ("Type/Subtype") the generated lists take in (&lt;Show&gt;).</summary>
        public readonly HashSet<string> Shown = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private readonly List<KeyValuePair<WikiPageXml, string>> _additions = new List<KeyValuePair<WikiPageXml, string>>();

        /// <summary>A link [[target]] or [[target|text]] inside a text.</summary>
        public struct Link
        {
            /// <summary>Index of the first character of the link in the text.</summary>
            public int Start;
            /// <summary>Length of the link including the brackets.</summary>
            public int Length;
            /// <summary>Id of the page the link points to.</summary>
            public string Target;
            /// <summary>Text shown for the link.</summary>
            public string Display;
        }

        /// <summary>Text in the language, the other one if empty, otherwise "".</summary>
        public static string Pick(bool german, string de, string en)
        {
            if (german)
                return string.IsNullOrEmpty(de) ? (en ?? "") : de;
            return string.IsNullOrEmpty(en) ? (de ?? "") : en;
        }

        /// <summary>
        /// Prepares text from the XML file: indentation goes, single line breaks become spaces,
        /// blank lines separate paragraphs, lines starting with "- " become bullet points.
        /// </summary>
        public static string Clean(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            var sb = new StringBuilder();
            bool paragraph = false;
            foreach (var raw in s.Replace("\r\n", "\n").Split('\n'))
            {
                var line = raw.Trim();
                if (line.Length == 0)
                {
                    if (sb.Length > 0)
                        paragraph = true;
                    continue;
                }
                bool bullet = line.StartsWith("- ") || line.StartsWith("* ") || line.StartsWith("•");
                if (bullet && !line.StartsWith("•"))
                    line = "•  " + line.Substring(2).TrimStart();
                if (sb.Length > 0)
                    sb.Append(paragraph ? "\n\n" : bullet ? "\n" : " ");
                sb.Append(line);
                paragraph = false;
            }
            return sb.ToString();
        }

        /// <summary>Takes over the content of a read file. origin = for error messages, source = mod name.</summary>
        public void Merge(WikiFileXml file, string origin, string source)
        {
            if (file == null)
                return;
            Sources.Add(origin);
            if (file.Errors != null)
                foreach (var error in file.Errors)
                    if (!string.IsNullOrWhiteSpace(error))
                        Errors.Add(source + ": " + error.Trim());
            MergeSystems(file, origin, source);
            MergeIds(file.Hide, Hidden, origin, "Hide");
            MergeIds(file.Show, Shown, origin, "Show");
            if (file.Pages == null)
                return;
            foreach (var x in file.Pages)
            {
                var id = x?.Id?.Trim();
                if (string.IsNullOrEmpty(id))
                {
                    Errors.Add($"{origin}: page without Id skipped");
                    continue;
                }
                if (x.Extend)
                {
                    _additions.Add(new KeyValuePair<WikiPageXml, string>(x, origin));
                    continue;
                }
                WikiPage existing;
                if (Pages.TryGetValue(id, out existing))
                    Errors.Add($"{origin}: page '{id}' already exists from {existing.Source}, it is replaced (Extend=\"true\" appends)");
                var page = new WikiPage
                {
                    Id = id,
                    Parent = x.Parent?.Trim() ?? "",
                    Order = x.Order,
                    TitleDe = x.Title?.De?.Trim(),
                    TitleEn = x.Title?.En?.Trim(),
                    Source = string.IsNullOrWhiteSpace(x.Source) ? source : x.Source.Trim(),
                    IconBlock = string.IsNullOrWhiteSpace(x.Block) ? null : x.Block.Trim(),
                    Icons = SplitIcons(x.Icons),
                };
                ApplyLevelAndSymbol(page, x, origin);
                MergeBlocks(page, x, origin);
                Pages[id] = page;
            }
        }

        private void MergeIds(List<DefinitionRefXml> list, HashSet<string> target, string origin, string element)
        {
            if (list == null)
                return;
            foreach (var x in list)
            {
                var id = x?.Id?.Trim();
                if (string.IsNullOrEmpty(id) || id.IndexOf('/') <= 0)
                {
                    Errors.Add($"{origin}: <{element}> needs Id=\"Type/Subtype\"");
                    continue;
                }
                if (id.StartsWith("MyObjectBuilder_", StringComparison.Ordinal))
                    id = id.Substring("MyObjectBuilder_".Length);
                target.Add(id);
            }
        }

        private void MergeSystems(WikiFileXml file, string origin, string source)
        {
            if (file.Systems == null)
                return;
            foreach (var sx in file.Systems)
            {
                var id = sx?.Id?.Trim();
                if (string.IsNullOrEmpty(id))
                {
                    Errors.Add($"{origin}: system without Id skipped");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(sx.Source))
                    sx.Source = source;
                SystemSources.Add(sx);
                var system = new WikiSystem
                {
                    Id = id,
                    Parent = sx.Parent?.Trim() ?? "",
                    Order = sx.Order,
                    De = sx.De?.Trim(),
                    En = sx.En?.Trim(),
                    Source = sx.Source,
                };
                int order = 0;
                foreach (var px in sx.Planets)
                {
                    var planetId = px?.Id?.Trim();
                    if (string.IsNullOrEmpty(planetId) || string.IsNullOrWhiteSpace(px.Definition))
                    {
                        Errors.Add($"{origin}: planet without Id or Definition in system '{id}'");
                        continue;
                    }
                    if (Planets.ContainsKey(planetId))
                    {
                        Errors.Add($"{origin}: planet '{planetId}' already exists, duplicate entry skipped");
                        continue;
                    }
                    var planet = new WikiPlanet
                    {
                        Id = planetId,
                        Definition = px.Definition.Trim(),
                        Parent = px.Parent?.Trim() ?? "",
                        De = px.De?.Trim(),
                        En = px.En?.Trim(),
                        SystemId = id,
                        Order = order += 10,
                        Image = string.IsNullOrWhiteSpace(px.Image) ? null : px.Image.Trim(),
                        ImageSource = px.ImageSource?.Trim(),
                        FactsDe = Clean(px.Facts?.De),
                        FactsEn = Clean(px.Facts?.En),
                    };
                    system.Planets.Add(planet);
                    Planets[planetId] = planet;
                }
                Systems.Add(system);
            }
        }

        private void ApplyLevelAndSymbol(WikiPage page, WikiPageXml x, string origin)
        {
            var level = x.Level?.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(level))
            {
                if (level == "important")
                    page.Level = WikiLevel.Important;
                else if (level == "new")
                    page.Level = WikiLevel.New;
                else if (level == "normal")
                    page.Level = WikiLevel.Normal;
                else
                    Errors.Add($"{origin}: page '{page.Id}': unknown Level '{x.Level}' (important, new, normal)");
            }
            var until = x.NewUntil?.Trim();
            if (!string.IsNullOrEmpty(until))
            {
                DateTime day;
                if (!TryParseDate(until, out day))
                    Errors.Add($"{origin}: page '{page.Id}': NewUntil '{x.NewUntil}' is not a date (YYYY-MM-DD)");
                else
                {
                    page.NewUntil = until;
                    if (page.Level == WikiLevel.New && DateTime.Now.Date > day)
                        page.Level = WikiLevel.Normal;
                }
            }
            var symbol = x.Symbol?.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(symbol))
            {
                if (Symbols.Contains(symbol))
                    page.Symbol = symbol;
                else
                    Errors.Add($"{origin}: page '{page.Id}': unknown Symbol '{x.Symbol}'");
            }
        }

        /// <summary>Splits YYYY-MM-DD by hand (independent of language settings).</summary>
        public static bool TryParseDate(string s, out DateTime day)
        {
            day = DateTime.MinValue;
            var parts = s.Split('-');
            int year, month, dayOfMonth;
            if (parts.Length != 3 || !int.TryParse(parts[0], out year) || !int.TryParse(parts[1], out month) || !int.TryParse(parts[2], out dayOfMonth)
                || year < 2000 || year > 9999 || month < 1 || month > 12 || dayOfMonth < 1 || dayOfMonth > DateTime.DaysInMonth(year, month))
                return false;
            day = new DateTime(year, month, dayOfMonth);
            return true;
        }

        internal WikiBlock ReadList(WikiPage page, ListXml list, string origin)
        {
            var block = new WikiBlock(WikiBlockKind.List, "", "") { Entries = new List<WikiEntry>() };
            if (list.Columns > 0)
                block.Columns = Math.Min(list.Columns, 6);
            foreach (var e in list.Entries)
            {
                if (e == null)
                    continue;
                var target = e.Page?.Trim();
                var blockId = e.Block?.Trim();
                if (string.IsNullOrEmpty(target) && string.IsNullOrEmpty(blockId) && string.IsNullOrWhiteSpace(e.De) && string.IsNullOrWhiteSpace(e.En))
                {
                    Errors.Add($"{origin}: list entry without page, block and text on page '{page.Id}'");
                    continue;
                }
                block.Entries.Add(new WikiEntry
                {
                    Target = string.IsNullOrEmpty(target) ? null : target,
                    De = Clean(e.De),
                    En = Clean(e.En),
                    ExtraDe = string.IsNullOrWhiteSpace(e.ExtraDe) ? null : e.ExtraDe.Trim(),
                    ExtraEn = string.IsNullOrWhiteSpace(e.ExtraEn) ? null : e.ExtraEn.Trim(),
                    Block = string.IsNullOrEmpty(blockId) ? null : blockId,
                    Icons = SplitIcons(e.Icons),
                });
            }
            if (!string.IsNullOrWhiteSpace(list.Auto))
            {
                block.Auto = list.Auto.Trim();
                block.AutoPlanet = list.PlanetId?.Trim();
                return block;
            }
            if (block.Entries.Count == 0 && !_filling)
            {
                Errors.Add($"{origin}: list without entries on page '{page.Id}'");
                return null;
            }
            return block;
        }

        /// <summary>Splits a table row into cells (separator |, not inside [[link|text]]).</summary>
        public static string[] Cells(string row)
        {
            if (string.IsNullOrWhiteSpace(row))
                return null;
            var cells = new List<string>();
            int start = 0, depth = 0;
            for (int i = 0; i < row.Length; i++)
            {
                if (i + 1 < row.Length && row[i] == '[' && row[i + 1] == '[')
                {
                    depth++;
                    i++;
                }
                else if (i + 1 < row.Length && row[i] == ']' && row[i + 1] == ']' && depth > 0)
                {
                    depth--;
                    i++;
                }
                else if (row[i] == '|' && depth == 0)
                {
                    cells.Add(Clean(row.Substring(start, i - start)));
                    start = i + 1;
                }
            }
            cells.Add(Clean(row.Substring(start)));
            return cells.ToArray();
        }

        /// <summary>Icon materials separated by ';', null = none.</summary>
        public static string[] SplitIcons(string icons)
        {
            if (string.IsNullOrWhiteSpace(icons))
                return null;
            var parts = icons.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
                parts[i] = parts[i].Trim();
            return parts.Length > 0 ? parts : null;
        }

        internal WikiBlock ReadTable(WikiPage page, TableXml table, string origin)
        {
            var block = new WikiBlock(WikiBlockKind.Table, "", "")
            {
                HeaderDe = Cells(table.Header?.De),
                HeaderEn = Cells(table.Header?.En),
                RowsDe = new List<string[]>(),
                RowsEn = new List<string[]>(),
            };
            foreach (var row in table.Rows)
            {
                if (row == null)
                    continue;
                block.RowsDe.Add(Cells(row.De));
                block.RowsEn.Add(Cells(row.En));
            }
            if (!string.IsNullOrWhiteSpace(table.Auto))
            {
                block.Auto = table.Auto.Trim();
                block.AutoSystem = table.SystemId?.Trim();
                block.AutoPlanet = table.PlanetId?.Trim();
            }
            else if (block.RowsDe.Count == 0 && !_filling)
            {
                Errors.Add($"{origin}: table without rows on page '{page.Id}'");
                return null;
            }
            if (!string.IsNullOrWhiteSpace(table.Widths))
            {
                var parts = table.Widths.Split('|');
                block.Widths = new float[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                {
                    float width;
                    block.Widths[i] = float.TryParse(parts[i].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out width) && width > 0f ? width : 1f;
                }
            }
            return block;
        }

        private void MergeBlocks(WikiPage page, WikiPageXml x, string origin)
        {
            if (x.Blocks == null)
                return;
            foreach (var b in x.Blocks)
            {
                if (b == null)
                    continue;
                if (b is TableXml)
                {
                    var table = ReadTable(page, (TableXml)b, origin);
                    if (table != null)
                        page.Blocks.Add(table);
                    continue;
                }
                if (b is ListXml)
                {
                    var list = ReadList(page, (ListXml)b, origin);
                    if (list != null)
                        page.Blocks.Add(list);
                    continue;
                }
                if (b is ProfileXml)
                {
                    var profile = (ProfileXml)b;
                    page.Blocks.Add(new WikiBlock(WikiBlockKind.FactSheet, Clean(b.De), Clean(b.En))
                    {
                        Material = string.IsNullOrWhiteSpace(profile.Material) ? null : profile.Material.Trim(),
                        ImageSource = string.IsNullOrWhiteSpace(profile.ImageSource) ? null : profile.ImageSource.Trim(),
                        Width = profile.Width > 0f ? profile.Width : 240f,
                        Height = profile.Width > 0f ? profile.Width : 240f,
                    });
                    continue;
                }
                if (b is ImageXml)
                {
                    var image = (ImageXml)b;
                    if (string.IsNullOrWhiteSpace(image.Material))
                    {
                        Errors.Add($"{origin}: image without material on page '{page.Id}'");
                        continue;
                    }
                    page.Blocks.Add(new WikiBlock(WikiBlockKind.Image, Clean(b.De), Clean(b.En))
                    {
                        Material = image.Material.Trim(),
                        Width = image.Width > 0f ? image.Width : 512f,
                        Height = image.Height > 0f ? image.Height : 288f,
                    });
                    continue;
                }
                if (b is LinkXml)
                {
                    // Link line: "›  [[target|Text]]", without text it shows the page title
                    var target = ((LinkXml)b).Page?.Trim();
                    if (string.IsNullOrEmpty(target))
                    {
                        Errors.Add($"{origin}: link without page on page '{page.Id}'");
                        continue;
                    }
                    var linkDe = Clean(b.De);
                    var linkEn = Clean(b.En);
                    page.Blocks.Add(new WikiBlock(WikiBlockKind.Text,
                        "›  [[" + target + (linkDe.Length > 0 ? "|" + linkDe : "") + "]]",
                        "›  [[" + target + (linkEn.Length > 0 ? "|" + linkEn : "") + "]]"));
                    continue;
                }
                var kind = b is HeadingXml ? WikiBlockKind.Heading
                         : b is NoteXml ? WikiBlockKind.Note
                         : WikiBlockKind.Text;
                var de = Clean(b.De);
                var en = Clean(b.En);
                if (de.Length == 0 && en.Length == 0)
                {
                    Errors.Add($"{origin}: empty element on page '{page.Id}'");
                    continue;
                }
                page.Blocks.Add(new WikiBlock(kind, de, en));
            }
        }

        /// <summary>Automatic table with kind and filters (WikiAuto fills it later).</summary>
        public static WikiBlock AutoTable(string auto, string system = null, string planet = null) =>
            new WikiBlock(WikiBlockKind.Table, "", "")
            {
                Auto = auto,
                AutoSystem = system,
                AutoPlanet = planet,
                RowsDe = new List<string[]>(),
                RowsEn = new List<string[]>(),
            };

        /// <summary>Automatic link list (WikiAuto fills it later).</summary>
        public static WikiBlock AutoList(string auto, string planet = null, int columns = 3) =>
            new WikiBlock(WikiBlockKind.List, "", "") { Auto = auto, AutoPlanet = planet, Columns = columns, Entries = new List<WikiEntry>() };

        /// <summary>Ready link list (for pages that WikiAuto writes itself).</summary>
        public static WikiBlock EntryList(List<WikiEntry> entries, int columns) =>
            new WikiBlock(WikiBlockKind.List, "", "") { Entries = entries, Columns = columns };

        /// <summary>True while blocks from generating mods are read (empty lists are fine there).</summary>
        internal bool _filling;

        /// <summary>In the game only into the log (other mod not loaded), in the strict check an error.</summary>
        private void Report(string text)
        {
            if (Strict)
                Errors.Add(text);
            else
                Notices.Add(text);
        }

        /// <summary>
        /// After reading all files: append additions, fill automatic tables (auto, needs the game),
        /// move orphaned pages to the top, fallback and error page.
        /// </summary>
        public void Finish(Action<WikiData> auto = null)
        {
            foreach (var a in _additions)
            {
                WikiPage page;
                if (!Pages.TryGetValue(a.Key.Id.Trim(), out page))
                {
                    Report($"{a.Value}: addition for missing page '{a.Key.Id}' skipped");
                    continue;
                }
                ApplyLevelAndSymbol(page, a.Key, a.Value);
                MergeBlocks(page, a.Key, a.Value);
            }
            _additions.Clear();

            if (auto != null)
            {
                try
                {
                    auto(this);
                }
                catch (Exception e)
                {
                    Errors.Add("Automatic tables not generated: " + e.Message);
                }
            }

            foreach (var page in Pages.Values)
            {
                if (string.Equals(page.Parent, page.Id, StringComparison.OrdinalIgnoreCase))
                    page.Parent = "";
                if (!string.IsNullOrEmpty(page.Parent) && !Pages.ContainsKey(page.Parent))
                {
                    Report($"Page '{page.Id}' ({page.Source}): parent page '{page.Parent}' missing, moved to the top");
                    page.Parent = "";
                }
            }
            // Break cycles (a below b, b below a), otherwise none of the pages shows up in the list
            foreach (var page in Pages.Values)
            {
                var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { page.Id };
                var p = page;
                while (!string.IsNullOrEmpty(p.Parent) && Pages.TryGetValue(p.Parent, out p))
                {
                    if (!seen.Add(p.Id))
                    {
                        Errors.Add($"Page '{page.Id}': cycle in Parent=, moved to the top");
                        page.Parent = "";
                        break;
                    }
                }
            }

            CheckLinks();

            if (!Pages.ContainsKey(StartId))
                Pages[StartId] = new WikiPage { Id = StartId, Order = -1000, TitleDe = "Willkommen", TitleEn = "Welcome", Source = "Syntrix" }
                    .Note("Es wurden keine Wiki-Seiten gefunden (Data/Syntrix-Wiki.xml in den Mods).",
                          "No wiki pages were found (Data/Syntrix-Wiki.xml in the mods).");

            Pages.Remove(ErrorId);
            if (Errors.Count > 0)
            {
                var errorPage = new WikiPage
                {
                    Id = ErrorId,
                    Order = 100000,
                    TitleDe = "Wiki-Fehler",
                    TitleEn = "Wiki errors",
                    Source = "Syntrix",
                    Level = WikiLevel.Important,
                    Symbol = "warning",
                }
                    .Text("Beim Laden der Wiki-Dateien ist Folgendes aufgefallen:", "The following problems were found while loading the wiki files:");
                foreach (var message in Errors)
                    errorPage.Note(message, message);
                Pages[ErrorId] = errorPage;
            }
        }

        /// <summary>All [[target]] and [[target|text]] in a text.</summary>
        public static List<Link> LinksIn(string text)
        {
            var links = new List<Link>();
            if (string.IsNullOrEmpty(text))
                return links;
            int i = 0;
            while ((i = text.IndexOf("[[", i, StringComparison.Ordinal)) >= 0)
            {
                int end = text.IndexOf("]]", i + 2, StringComparison.Ordinal);
                if (end < 0)
                    break;
                var content = text.Substring(i + 2, end - i - 2);
                int bar = content.IndexOf('|');
                links.Add(new Link
                {
                    Start = i,
                    Length = end + 2 - i,
                    Target = (bar >= 0 ? content.Substring(0, bar) : content).Trim(),
                    Display = bar >= 0 ? content.Substring(bar + 1).Trim() : null,
                });
                i = end + 2;
            }
            return links;
        }

        private static IEnumerable<string> Texts(WikiBlock b)
        {
            yield return b.De;
            yield return b.En;
            foreach (var header in new[] { b.HeaderDe, b.HeaderEn })
                if (header != null)
                    foreach (var cell in header)
                        yield return cell;
            foreach (var rows in new[] { b.RowsDe, b.RowsEn })
                if (rows != null)
                    foreach (var row in rows)
                        if (row != null)
                            foreach (var cell in row)
                                yield return cell;
            if (b.Entries != null)
                foreach (var e in b.Entries)
                {
                    yield return e.De;
                    yield return e.En;
                }
        }

        /// <summary>Reports links to unknown pages (they are then shown as plain text only).</summary>
        private void CheckLinks()
        {
            foreach (var page in Pages.Values)
                foreach (var b in page.Blocks)
                {
                    foreach (var t in Texts(b))
                        foreach (var link in LinksIn(t))
                            if (link.Target.Length == 0 || !Pages.ContainsKey(link.Target))
                                Errors.Add($"Page '{page.Id}' ({page.Source}): link to unknown page '{link.Target}'");
                    if (b.Entries != null)
                        foreach (var e in b.Entries)
                            if (!string.IsNullOrEmpty(e.Target) && !Pages.ContainsKey(e.Target))
                            {
                                Errors.Add($"Page '{page.Id}' ({page.Source}): list entry to unknown page '{e.Target}'");
                                e.Target = null;
                            }
                }
        }

        /// <summary>For the search: id, title and all texts of the page in both languages.</summary>
        public static bool Contains(WikiPage page, string search)
        {
            if (string.IsNullOrEmpty(search))
                return true;
            if (Matches(page.TitleDe, search) || Matches(page.TitleEn, search) || Matches(page.Id, search))
                return true;
            foreach (var b in page.Blocks)
                foreach (var t in Texts(b))
                    if (Matches(t, search))
                        return true;
            return false;
        }

        private static bool Matches(string text, string search) =>
            !string.IsNullOrEmpty(text) && text.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0;

        /// <summary>Sub pages of a page, sorted by order, then id.</summary>
        public List<WikiPage> Children(string id)
        {
            var children = new List<WikiPage>();
            foreach (var page in Pages.Values)
                if (string.Equals(page.Parent ?? "", id ?? "", StringComparison.OrdinalIgnoreCase) && !string.Equals(page.Id, id, StringComparison.OrdinalIgnoreCase))
                    children.Add(page);
            children.Sort((a, b) => a.Order != b.Order ? a.Order.CompareTo(b.Order) : string.CompareOrdinal(a.Id, b.Id));
            return children;
        }
    }
}
