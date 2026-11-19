// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
namespace Syntrix.Wiki
{
    /// <summary>Entry of a link list: icon, name (link to Target if set), quiet extra text.</summary>
    public class WikiEntry
    {
        /// <summary>Id of the page the entry links to; null = no link.</summary>
        public string Target;
        /// <summary>Name in German.</summary>
        public string De;
        /// <summary>Name in English.</summary>
        public string En;
        /// <summary>Quiet second text in German.</summary>
        public string ExtraDe;
        /// <summary>Quiet second text in English.</summary>
        public string ExtraEn;

        /// <summary>Icon of this block ("Type/Subtype"), drawn from its definition id.</summary>
        public string Block;

        /// <summary>Material names of the icons, drawn on top of each other (e.g. LQ mark on the ore).</summary>
        public string[] Icons;

        /// <summary>Name in the language, the other one if empty.</summary>
        public string Text(bool german) => WikiData.Pick(german, De, En);

        /// <summary>Second text in the language, the other one if empty.</summary>
        public string Extra(bool german) => WikiData.Pick(german, ExtraDe, ExtraEn);
    }
}
