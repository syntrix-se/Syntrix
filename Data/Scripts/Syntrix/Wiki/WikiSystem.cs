// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System.Collections.Generic;

namespace Syntrix.Wiki
{
    /// <summary>A planetary system from a &lt;System&gt; list of a wiki file.</summary>
    public class WikiSystem
    {
        public string Id;
        public string Parent;
        public string De;
        public string En;
        public string Source;
        public int Order;
        public readonly List<WikiPlanet> Planets = new List<WikiPlanet>();
    }
}
