// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Syntrix.Wiki
{
    /// <summary>Element &lt;Table&gt; of a page, by hand or automatic.</summary>
    public class TableXml : LangTextXml
    {
        /// <summary>Relative column widths, e.g. "2|1|3". Without: by text length.</summary>
        [XmlAttribute("Widths")]
        public string Widths;

        /// <summary>Automatic table: Ores, Components, Planets, OresPerPlanet, PlanetData (and those of WikiForeign).</summary>
        [XmlAttribute("Auto")]
        public string Auto;

        [XmlAttribute("System")]
        public string SystemId;

        [XmlAttribute("Planet")]
        public string PlanetId;

        [XmlElement("Header")]
        public LangTextXml Header;

        [XmlElement("Row")]
        public List<LangTextXml> Rows = new List<LangTextXml>();
    }
}
