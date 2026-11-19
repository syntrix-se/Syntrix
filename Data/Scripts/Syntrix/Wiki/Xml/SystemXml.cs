// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Syntrix.Wiki
{
    /// <summary>Element &lt;System&gt;: a planetary system with its bodies.</summary>
    public class SystemXml : LangTextXml
    {
        /// <summary>Unique id of the system.</summary>
        [XmlAttribute("Id")]
        public string Id;

        /// <summary>Id of the parent page; empty = top level.</summary>
        [XmlAttribute("Parent")]
        public string Parent;

        /// <summary>Position among the sibling pages, ascending.</summary>
        [XmlAttribute("Order")]
        public int Order;

        /// <summary>Mod of the system (set by Syntrix in the context for generating mods).</summary>
        [XmlAttribute("Source")]
        public string Source;

        /// <summary>Planets and moons of the system.</summary>
        [XmlElement("Planet")]
        public List<PlanetXml> Planets = new List<PlanetXml>();
    }
}
