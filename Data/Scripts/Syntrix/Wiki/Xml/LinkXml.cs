// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System.Xml.Serialization;

namespace Syntrix.Wiki
{
    /// <summary>Element &lt;Link&gt; of a page, also &lt;Entry&gt; of a list.</summary>
    public class LinkXml : LangTextXml
    {
        /// <summary>Id of the page the link points to.</summary>
        [XmlAttribute("Page")]
        public string Page;

        /// <summary>List entry only: icon of this block ("Type/Subtype"); Page= may then be missing.</summary>
        [XmlAttribute("Block")]
        public string Block;

        /// <summary>List entry only: icon materials separated by ';' (written by generating mods).</summary>
        [XmlAttribute("Icons")]
        public string Icons;

        /// <summary>List entry only: quiet second text (German).</summary>
        [XmlAttribute("ExtraDe")]
        public string ExtraDe;

        /// <summary>List entry only: quiet second text (English).</summary>
        [XmlAttribute("ExtraEn")]
        public string ExtraEn;
    }
}
