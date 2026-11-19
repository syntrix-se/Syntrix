// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System.Xml.Serialization;

namespace Syntrix.Wiki
{
    /// <summary>Element &lt;Profile&gt; of a page: image on the left (source small below), text beside it.</summary>
    public class ProfileXml : LangTextXml
    {
        [XmlAttribute("Material")]
        public string Material;

        [XmlAttribute("ImageSource")]
        public string ImageSource;

        /// <summary>Image size in reference pixels (square), 240 without.</summary>
        [XmlAttribute("Width")]
        public float Width;
    }
}
