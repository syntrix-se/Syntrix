// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System.Xml.Serialization;

namespace Syntrix.Wiki
{
    /// <summary>Element &lt;Image&gt; of a page.</summary>
    public class ImageXml : LangTextXml
    {
        [XmlAttribute("Material")]
        public string Material;

        [XmlAttribute("Width")]
        public float Width;

        [XmlAttribute("Height")]
        public float Height;
    }
}
