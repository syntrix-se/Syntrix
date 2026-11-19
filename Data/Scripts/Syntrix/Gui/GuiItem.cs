// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using VRageMath;

namespace Syntrix.Gui
{
    /// <summary>One item of a list, tree, table, tab row or drop-down.</summary>
    public class GuiItem
    {
        /// <summary>Key of the item (given by the mod, otherwise its index).</summary>
        public string Key;
        /// <summary>Key of the parent item (tree); null = top level.</summary>
        public string ParentKey;
        /// <summary>Item text (markup).</summary>
        public string Text = "";
        /// <summary>Icon materials drawn on top of each other; null = no icon.</summary>
        public string[] Icons;
        /// <summary>Item can be selected; disabled items are drawn dimmed.</summary>
        public bool Enabled = true;
        /// <summary>Children of the item are visible (tree).</summary>
        public bool Expanded;
        /// <summary>Second text (list: right aligned or below the text).</summary>
        public string Extra;
        /// <summary>Text colour of the item; null = default.</summary>
        public Color? Color;
        /// <summary>Tooltip of the item; null = tooltip of the element.</summary>
        public string Tooltip;
        /// <summary>Small tag behind the text, e.g. "New".</summary>
        public string Badge;
        /// <summary>Nesting depth below the root (tree), set when the tree is built.</summary>
        public int Depth;
        /// <summary>Visible, i.e. all parents are expanded (tree), set when the tree is built.</summary>
        public bool Shown = true;
        /// <summary>Laid out cell texts of a table row; null for other elements.</summary>
        public GuiRichText[] Cells;
        /// <summary>Row height in screen pixels, set when the element is laid out.</summary>
        public float Height;
    }
}
