// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System.Xml.Serialization;

namespace Syntrix.Wiki
{
    /// <summary>&lt;Hide Id="Type/Subtype" /&gt; or &lt;Show Id="Type/Subtype" /&gt; in a wiki file.</summary>
    public class DefinitionRefXml
    {
        /// <summary>Definition as "Type/Subtype", without "MyObjectBuilder_".</summary>
        [XmlAttribute("Id")]
        public string Id;
    }
}
