// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
namespace Syntrix.Wiki
{
    /// <summary>Entry of a link list: icon, name (link to Target if set), quiet extra text.</summary>
    public class WikiEntry
    {
        public string Target;
        public string De;
        public string En;
        public string ExtraDe;
        public string ExtraEn;

        /// <summary>Icon of this block ("Type/Subtype"), resolved in WikiForeign.ResolveBlockIcons.</summary>
        public string Block;

        /// <summary>Material names of the icons, drawn on top of each other (e.g. LQ mark on the ore).</summary>
        public string[] Icons;

        public string Text(bool german) => WikiData.Pick(german, De, En);

        public string Extra(bool german) => WikiData.Pick(german, ExtraDe, ExtraEn);
    }
}
