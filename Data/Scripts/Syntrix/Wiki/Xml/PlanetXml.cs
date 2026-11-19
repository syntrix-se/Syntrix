// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System.Xml.Serialization;

namespace Syntrix.Wiki
{
    /// <summary>Element &lt;Planet&gt; inside &lt;System&gt;.</summary>
    public class PlanetXml : LangTextXml
    {
        [XmlAttribute("Id")]
        public string Id;

        /// <summary>SubtypeId of the PlanetGeneratorDefinition.</summary>
        [XmlAttribute("Definition")]
        public string Definition;

        /// <summary>Id of the planet a moon orbits. Empty = directly in the system.</summary>
        [XmlAttribute("Parent")]
        public string Parent;

        /// <summary>TransparentMaterial of the image for the profile (optional).</summary>
        [XmlAttribute("Image")]
        public string Image;

        /// <summary>Source of the image, shown small below it (e.g. NASA/JPL).</summary>
        [XmlAttribute("ImageSource")]
        public string ImageSource;

        /// <summary>A few lines of real facts about the body (optional).</summary>
        [XmlElement("Facts")]
        public LangTextXml Facts;
    }
}
