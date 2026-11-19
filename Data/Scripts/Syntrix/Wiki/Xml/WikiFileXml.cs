// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Syntrix.Wiki
{
    // File format of Data/Syntrix-Wiki.xml (and of further files named there under <File>).
    // Read with MyAPIGateway.Utilities.SerializeFromXML, i.e. XmlSerializer: names are case sensitive.
    // The element and attribute names are shared by all Syntrix mods and must not change.
    //
    // <SyxWiki>
    //   <File>Data/Wiki/ores.xml</File>                         further file of the same mod
    //   <Page Id="gridcore" Parent="building" Order="20" Level="important" Symbol="core">
    //     <Title de="GridCore" en="GridCore" />
    //     <Heading de="…" en="…" />
    //     <Text>
    //       <de>Absatz eins …
    //
    //       Absatz zwei …</de>
    //       <en>Paragraph one …</en>
    //     </Text>
    //     <Note de="Kurzer Hinweis" en="Short note" />
    //     <Table Widths="2|1|3">                                  columns separated by |, widths optional
    //       <Header de="Erz|Barren|Wofür" en="Ore|Ingot|Used for" />
    //       <Row de="Kupfererz|Kupfer|Kupferdraht" en="Copper ore|Copper|Copper wire" />
    //     </Table>
    //     <Image Material="SyxWiki_Image_X" Width="640" Height="360" de="Bildunterschrift" en="Caption" />
    //     <Link Page="ores" de="Mehr zu Erzen" en="More about ores" />
    //   </Page>
    //   <Page Id="refineries" Extend="true"> … </Page>             appends blocks to a page of another mod
    //
    //   <System Id="sol" Order="50" de="Sol-System" en="Sol system">     planet mods: which bodies the wiki shows
    //     <Planet Id="earth" Definition="Sol-Earth" de="Erde" en="Earth" />
    //     <Planet Id="moon" Definition="Sol-Moon" Parent="earth" de="Mond" en="Moon" />
    //   </System>
    //   The system and every planet get a page (Id as given, moons below their planet),
    //   with values and ores from the PlanetGeneratorDefinition. Own texts with <Page Id="earth" Extend="true">.
    //   If a page with that Id exists already, it stays and does not get the automatic tables.
    //
    //   Automatic tables (content from the definitions of the loaded mods, no rows needed):
    //     <Table Auto="Ores" />                          refinery: ore to ingot per kg, which refineries
    //     <Table Auto="Components" />                    components and their cost
    //     <Table Auto="Planets" System="sol" />          planets of a system with gravity, atmosphere, temperature
    //     <Table Auto="OresPerPlanet" System="sol" />    which ores on which body
    //     <Table Auto="OresPerPlanet" Planet="earth" />  ores of one body with depth
    //     <Table Auto="PlanetData" Planet="earth" />     values of one body
    //   Which Auto names exist depends on the mods that provide them (Syntrix API Wiki.AddTables).
    //
    //   Link lists in columns, with icons (item icons); a click opens the page:
    //     <List Auto="Ores" Columns="3" />               all ores (each ore has its own page ore-<subtype>)
    //     <List Auto="Components" />                     all components (each has a page component-<subtype>)
    //     <List Auto="OresPerPlanet" Planet="earth" />   ores of one body with depth
    //     <List Columns="2"><Entry Page="ores" de="Erze" en="Ores" /></List>   by hand
    //   Ore and component pages are created automatically below "ores" and "components". Own texts
    //   with <Page Id="ore-copper" Extend="true">.
    //
    // Extend="true" on a page that does not exist (its mod is not loaded) is dropped without an error page,
    //   only with a log line. So every mod can append sentences and links to pages of other mods without the
    //   other mod having to link back. The same applies to Parent= pointing to a missing page (page moves to the top).
    // Block="Reactor/SyxLargeBlockLargeCoalReactor" on a list entry (Page= may then be missing) or on the page:
    //   icon of the block (its first definition icon; a TransparentMaterial "SyxWikiIcon_<path>" is used if present).
    // Level (optional): important = yellow with tag "Wichtig", new = tag "Neu". Without it normal.
    //   "Neu" disappears for each player as soon as he has opened the page, and for everybody after
    //   NewUntil="2026-12-01" (YYYY-MM-DD). A new NewUntil date shows the tag again.
    // Symbol (optional, mainly for top pages): home, core, cube, planet, shield, ore, tool,
    //   gear, book, info, warning, component, research, star (images in Textures/GUI/Wiki of Syntrix).
    //
    // Written by generating mods only (Wiki.AddSource / Wiki.AddTables): Icons="a;b", Source= on a page,
    //   Icons=, ExtraDe=, ExtraEn= on a list entry, <Profile Material ImageSource Width de en />, <Error>.
    //
    // In text (also in tables): [[ores]] or [[ores|own text]] = link (clickable),
    //   **this** = highlighted, "- " at the start of a line = bullet point.
    // Image: Material is a TransparentMaterial definition of the own mod (texture as DDS).
    //
    // Profile of a planet (top of its page: image left, facts right, then the game values):
    //   <Planet Id="mars" Definition="…" de="Mars" en="Mars" Image="SyxWiki_Sol_mars" ImageSource="NASA/JPL">
    //     <Facts><de>Ein paar Zeilen echte Fakten …</de><en>A few lines of real facts …</en></Facts>
    //   </Planet>
    //   Image is a TransparentMaterial of the planet mod (square, e.g. 256 px as BC1 DDS).
    // </SyxWiki>

    /// <summary>Root element of a wiki file (Data/Syntrix-Wiki.xml and the files listed in it).</summary>
    [XmlRoot("SyxWiki")]
    public class WikiFileXml
    {
        /// <summary>More wiki files of the mod (path relative to the mod folder).</summary>
        [XmlElement("File")]
        public List<string> Files = new List<string>();

        /// <summary>Pages and additions to pages.</summary>
        [XmlElement("Page")]
        public List<WikiPageXml> Pages = new List<WikiPageXml>();

        /// <summary>Planetary systems with their bodies.</summary>
        [XmlElement("System")]
        public List<SystemXml> Systems = new List<SystemXml>();

        /// <summary>Problems reported by a generating mod (shown on the error page).</summary>
        [XmlElement("Error")]
        public List<string> Errors = new List<string>();
    }
}
