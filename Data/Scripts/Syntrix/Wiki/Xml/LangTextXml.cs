// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System.Xml.Serialization;

namespace Syntrix.Wiki
{
    /// <summary>Text in both languages, short as attribute (de="…") or long as element (&lt;de&gt;…&lt;/de&gt;).</summary>
    public class LangTextXml
    {
        [XmlAttribute("de")]
        public string DeShort;

        [XmlAttribute("en")]
        public string EnShort;

        [XmlElement("de")]
        public string DeLong;

        [XmlElement("en")]
        public string EnLong;

        [XmlIgnore]
        public string De => string.IsNullOrEmpty(DeLong) ? DeShort : DeLong;

        [XmlIgnore]
        public string En => string.IsNullOrEmpty(EnLong) ? EnShort : EnLong;
    }
}
