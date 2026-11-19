// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System.Collections.Generic;

namespace Syntrix.Wiki
{
    /// <summary>One page of the wiki.</summary>
    public class WikiPage
    {
        /// <summary>Unique id that links and sub pages point to.</summary>
        public string Id;

        /// <summary>Id of the parent page, empty = top level.</summary>
        public string Parent = "";

        public int Order;
        public WikiLevel Level;

        /// <summary>Name of the symbol (Textures/GUI/Wiki/&lt;name&gt;.dds), empty = none.</summary>
        public string Symbol = "";

        public string TitleDe;
        public string TitleEn;

        /// <summary>Where the page comes from (mod name or "Server").</summary>
        public string Source = "";

        /// <summary>Icons next to the title (material names), null = none.</summary>
        public string[] Icons;

        /// <summary>Day up to which Level="new" applies (NewUntil=), null = none.</summary>
        public string NewUntil;

        /// <summary>Icon of this block in the page header ("Type/Subtype").</summary>
        public string IconBlock;

        public readonly List<WikiBlock> Blocks = new List<WikiBlock>();

        /// <summary>For "Neu": key under which the window remembers that the player has seen the page.</summary>
        public string NewKey => Id + "|" + (NewUntil ?? "");

        public string Title(bool german)
        {
            var title = WikiData.Pick(german, TitleDe, TitleEn);
            return string.IsNullOrEmpty(title) ? Id : title;
        }

        public WikiPage Text(string de, string en)
        {
            Blocks.Add(new WikiBlock(WikiBlockKind.Text, de, en));
            return this;
        }

        public WikiPage Heading(string de, string en)
        {
            Blocks.Add(new WikiBlock(WikiBlockKind.Heading, de, en));
            return this;
        }

        public WikiPage Note(string de, string en)
        {
            Blocks.Add(new WikiBlock(WikiBlockKind.Note, de, en));
            return this;
        }
    }
}
