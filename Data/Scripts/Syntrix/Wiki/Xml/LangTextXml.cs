// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System.Xml.Serialization;

namespace Syntrix.Wiki
{
    /// <summary>Text in both languages, short as attribute (de="…") or long as element (&lt;de&gt;…&lt;/de&gt;).</summary>
    public class LangTextXml
    {
        /// <summary>German text as attribute.</summary>
        [XmlAttribute("de")]
        public string DeShort;

        /// <summary>English text as attribute.</summary>
        [XmlAttribute("en")]
        public string EnShort;

        /// <summary>German text as element.</summary>
        [XmlElement("de")]
        public string DeLong;

        /// <summary>English text as element.</summary>
        [XmlElement("en")]
        public string EnLong;

        /// <summary>German text; the element wins over the attribute.</summary>
        [XmlIgnore]
        public string De => string.IsNullOrEmpty(DeLong) ? DeShort : DeLong;

        /// <summary>English text; the element wins over the attribute.</summary>
        [XmlIgnore]
        public string En => string.IsNullOrEmpty(EnLong) ? EnShort : EnLong;
    }
}
