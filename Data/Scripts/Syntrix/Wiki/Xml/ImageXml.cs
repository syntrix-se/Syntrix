// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System.Xml.Serialization;

namespace Syntrix.Wiki
{
    /// <summary>Element &lt;Image&gt; of a page.</summary>
    public class ImageXml : LangTextXml
    {
        /// <summary>TransparentMaterial of the image.</summary>
        [XmlAttribute("Material")]
        public string Material;

        /// <summary>Width in reference pixels; 512 without.</summary>
        [XmlAttribute("Width")]
        public float Width;

        /// <summary>Height in reference pixels; 288 without.</summary>
        [XmlAttribute("Height")]
        public float Height;
    }
}
