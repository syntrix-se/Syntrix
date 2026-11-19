// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
namespace Syntrix.Wiki
{
    /// <summary>A planet or moon from a &lt;System&gt; list of a wiki file.</summary>
    public class WikiPlanet
    {
        /// <summary>Unique id of the body (link target and filter value).</summary>
        public string Id;
        /// <summary>SubtypeId of the PlanetGeneratorDefinition.</summary>
        public string Definition;
        /// <summary>Id of the planet a moon orbits; empty = directly in the system.</summary>
        public string Parent;
        /// <summary>Name in German.</summary>
        public string De;
        /// <summary>Name in English.</summary>
        public string En;
        /// <summary>Id of the system the body belongs to.</summary>
        public string SystemId;
        /// <summary>Position in the system, ascending (file order, steps of 10).</summary>
        public int Order;

        /// <summary>Fact sheet: image material, its source, facts per language (all optional).</summary>
        public string Image;
        /// <summary>Source of the image, shown small below it.</summary>
        public string ImageSource;
        /// <summary>Facts about the body in German.</summary>
        public string FactsDe;
        /// <summary>Facts about the body in English.</summary>
        public string FactsEn;

        /// <summary>Name in the language, the other one if empty, otherwise the id.</summary>
        public string Name(bool german)
        {
            var name = WikiData.Pick(german, De, En);
            return string.IsNullOrEmpty(name) ? Id : name;
        }
    }
}
