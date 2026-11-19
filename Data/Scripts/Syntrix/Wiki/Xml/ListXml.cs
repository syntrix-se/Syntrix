// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Syntrix.Wiki
{
    /// <summary>Element &lt;List&gt; of a page, by hand or automatic.</summary>
    public class ListXml : LangTextXml
    {
        /// <summary>Automatic list: Ores, Components, OresPerPlanet (and those of WikiForeign).</summary>
        [XmlAttribute("Auto")]
        public string Auto;

        [XmlAttribute("Planet")]
        public string PlanetId;

        /// <summary>Columns, 3 without it (fewer in a narrow window).</summary>
        [XmlAttribute("Columns")]
        public int Columns;

        [XmlElement("Entry")]
        public List<LinkXml> Entries = new List<LinkXml>();
    }
}
