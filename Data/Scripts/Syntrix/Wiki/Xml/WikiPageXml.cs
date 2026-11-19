// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Syntrix.Wiki
{
    /// <summary>Element &lt;Page&gt;: one page or an addition to a page (Extend="true").</summary>
    public class WikiPageXml
    {
        /// <summary>Unique page id that links and sub pages point to.</summary>
        [XmlAttribute("Id")]
        public string Id;

        /// <summary>Id of the parent page; empty = top level.</summary>
        [XmlAttribute("Parent")]
        public string Parent;

        /// <summary>Position among the sibling pages, ascending.</summary>
        [XmlAttribute("Order")]
        public int Order;

        /// <summary>true: append the blocks to the page with this Id instead of replacing it.</summary>
        [XmlAttribute("Extend")]
        public bool Extend;

        /// <summary>Highlight: "important" or "new"; otherwise normal.</summary>
        [XmlAttribute("Level")]
        public string Level;

        /// <summary>Name of the symbol (Textures/GUI/Wiki/NAME.dds).</summary>
        [XmlAttribute("Symbol")]
        public string Symbol;

        /// <summary>Level="new" applies up to this day (YYYY-MM-DD), normal afterwards.</summary>
        [XmlAttribute("NewUntil")]
        public string NewUntil;

        /// <summary>Large icon in the page header: icon of this block ("Type/Subtype").</summary>
        [XmlAttribute("Block")]
        public string Block;

        /// <summary>Icon materials of the page header separated by ';' (written by generating mods).</summary>
        [XmlAttribute("Icons")]
        public string Icons;

        /// <summary>Mod shown below the title; empty = the mod of the file.</summary>
        [XmlAttribute("Source")]
        public string Source;

        /// <summary>Title of the page.</summary>
        [XmlElement("Title")]
        public LangTextXml Title;

        /// <summary>Blocks in file order.</summary>
        [XmlElement("Text", typeof(TextXml))]
        [XmlElement("Heading", typeof(HeadingXml))]
        [XmlElement("Note", typeof(NoteXml))]
        [XmlElement("Table", typeof(TableXml))]
        [XmlElement("Image", typeof(ImageXml))]
        [XmlElement("Link", typeof(LinkXml))]
        [XmlElement("List", typeof(ListXml))]
        [XmlElement("Profile", typeof(ProfileXml))]
        public List<LangTextXml> Blocks = new List<LangTextXml>();
    }
}
