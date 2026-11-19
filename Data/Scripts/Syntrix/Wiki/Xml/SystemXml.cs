// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Syntrix.Wiki
{
    /// <summary>Element &lt;System&gt;: a planetary system with its bodies.</summary>
    public class SystemXml : LangTextXml
    {
        [XmlAttribute("Id")]
        public string Id;

        [XmlAttribute("Parent")]
        public string Parent;

        [XmlAttribute("Order")]
        public int Order;

        /// <summary>Mod of the system (set by Syntrix in the context for generating mods).</summary>
        [XmlAttribute("Source")]
        public string Source;

        [XmlElement("Planet")]
        public List<PlanetXml> Planets = new List<PlanetXml>();
    }
}
