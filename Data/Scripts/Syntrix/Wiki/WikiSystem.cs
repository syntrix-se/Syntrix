// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System.Collections.Generic;

namespace Syntrix.Wiki
{
    /// <summary>A planetary system from a &lt;System&gt; list of a wiki file.</summary>
    public class WikiSystem
    {
        /// <summary>Unique id of the system.</summary>
        public string Id;
        /// <summary>Id of the parent page; empty = top level.</summary>
        public string Parent;
        /// <summary>Name in German.</summary>
        public string De;
        /// <summary>Name in English.</summary>
        public string En;
        /// <summary>Mod the system comes from.</summary>
        public string Source;
        /// <summary>Position among the sibling pages, ascending.</summary>
        public int Order;
        /// <summary>Planets and moons of the system in order.</summary>
        public readonly List<WikiPlanet> Planets = new List<WikiPlanet>();
    }
}
