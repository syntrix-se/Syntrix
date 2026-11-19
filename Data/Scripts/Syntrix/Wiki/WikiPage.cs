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

        /// <summary>Position among the sibling pages, ascending.</summary>
        public int Order;
        /// <summary>Highlight in the page list.</summary>
        public WikiLevel Level;

        /// <summary>Name of the symbol (Textures/GUI/Wiki/&lt;name&gt;.dds), empty = none.</summary>
        public string Symbol = "";

        /// <summary>Title in German.</summary>
        public string TitleDe;
        /// <summary>Title in English.</summary>
        public string TitleEn;

        /// <summary>Where the page comes from (mod name or "Server").</summary>
        public string Source = "";

        /// <summary>Icons next to the title (material names), null = none.</summary>
        public string[] Icons;

        /// <summary>Day up to which Level="new" applies (NewUntil=), null = none.</summary>
        public string NewUntil;

        /// <summary>Icon of this block in the page header ("Type/Subtype").</summary>
        public string IconBlock;

        /// <summary>Blocks of the page in order.</summary>
        public readonly List<WikiBlock> Blocks = new List<WikiBlock>();

        /// <summary>For "Neu": key under which the window remembers that the player has seen the page.</summary>
        public string NewKey => Id + "|" + (NewUntil ?? "");

        /// <summary>Title in the language, the other one if empty, otherwise the id.</summary>
        public string Title(bool german)
        {
            var title = WikiData.Pick(german, TitleDe, TitleEn);
            return string.IsNullOrEmpty(title) ? Id : title;
        }

        /// <summary>Appends a text block; returns the page.</summary>
        public WikiPage Text(string de, string en)
        {
            Blocks.Add(new WikiBlock(WikiBlockKind.Text, de, en));
            return this;
        }

        /// <summary>Appends a heading block; returns the page.</summary>
        public WikiPage Heading(string de, string en)
        {
            Blocks.Add(new WikiBlock(WikiBlockKind.Heading, de, en));
            return this;
        }

        /// <summary>Appends a note block; returns the page.</summary>
        public WikiPage Note(string de, string en)
        {
            Blocks.Add(new WikiBlock(WikiBlockKind.Note, de, en));
            return this;
        }
    }
}
