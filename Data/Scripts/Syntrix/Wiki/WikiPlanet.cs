// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
namespace Syntrix.Wiki
{
    /// <summary>A planet or moon from a &lt;System&gt; list of a wiki file.</summary>
    public class WikiPlanet
    {
        public string Id;
        public string Definition;
        public string Parent;
        public string De;
        public string En;
        public string SystemId;
        public int Order;

        /// <summary>Fact sheet: image material, its source, facts per language (all optional).</summary>
        public string Image;
        public string ImageSource;
        public string FactsDe;
        public string FactsEn;

        public string Name(bool german)
        {
            var name = WikiData.Pick(german, De, En);
            return string.IsNullOrEmpty(name) ? Id : name;
        }
    }
}
