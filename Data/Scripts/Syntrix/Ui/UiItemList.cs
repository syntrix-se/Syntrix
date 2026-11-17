// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System.Collections.Generic;
using RichHudFramework.UI;
using RichHudFramework.UI.Client;

namespace Syntrix.Ui
{
    /// <summary>
    /// Text entries of the list controls (drop-down, list box, tree list, terminal drop-down and terminal list).
    /// Every entry has a key (the mod's value, given back in ValueChanged) and a text with markup.
    /// </summary>
    public static class UiItemList
    {
        /// <summary>True when the node holds text entries.</summary>
        /// <param name="n">The node.</param>
        public static bool HasItems(UiNode n)
        {
            return n.Drop != null || n.Items != null || n.TreeL != null || n.TermDrop != null || n.TermList != null;
        }

        /// <summary>Number of entries.</summary>
        /// <param name="n">The node.</param>
        public static int Count(UiNode n)
        {
            if (n.Drop != null) return n.Drop.EntryList.Count;
            if (n.Items != null) return n.Items.Count;
            if (n.TreeL != null) return n.TreeL.Count;
            if (n.TermDrop != null) return n.TermDrop.List.Count;
            if (n.TermList != null) return n.TermList.List.Count;
            return 0;
        }

        /// <summary>Inserts an entry; returns its index or -1.</summary>
        /// <param name="n">The node.</param>
        /// <param name="index">Position; below 0 or beyond the end appends.</param>
        /// <param name="key">Key of the entry.</param>
        /// <param name="markup">Text of the entry.</param>
        public static int Insert(UiNode n, int index, string key, string markup)
        {
            if (!HasItems(n))
                return -1;
            int count = Count(n);
            if (index < 0 || index > count)
                index = count;
            var text = n.Rich(markup);
            key = key ?? "";
            if (n.Drop != null) n.Drop.Insert(index, text, key);
            else if (n.Items != null) n.Items.Insert(index, text, key);
            else if (n.TreeL != null) n.TreeL.Insert(index, text, key);
            else if (n.TermDrop != null) n.TermDrop.List.Insert(index, text, key);
            else n.TermList.List.Insert(index, text, key);
            return index;
        }

        /// <summary>Removes an entry; index -1 removes all.</summary>
        /// <param name="n">The node.</param>
        /// <param name="index">Entry index or -1.</param>
        public static void Remove(UiNode n, int index)
        {
            if (index < 0)
            {
                if (n.Drop != null) n.Drop.ClearEntries();
                else if (n.Items != null) n.Items.ClearEntries();
                else if (n.TreeL != null) n.TreeL.ClearEntries();
                else if (n.TermDrop != null) n.TermDrop.List.Clear();
                else if (n.TermList != null) n.TermList.List.Clear();
                return;
            }
            if (index >= Count(n))
                return;
            if (n.Drop != null) n.Drop.RemoveAt(index);
            else if (n.Items != null) n.Items.RemoveAt(index);
            else if (n.TreeL != null) n.TreeL.RemoveAt(index);
            else if (n.TermDrop != null) n.TermDrop.List.RemoveAt(index);
            else if (n.TermList != null) n.TermList.List.RemoveAt(index);
        }

        /// <summary>Replaces the text of an entry.</summary>
        /// <param name="n">The node.</param>
        /// <param name="index">Entry index.</param>
        /// <param name="markup">New text.</param>
        public static void SetText(UiNode n, int index, string markup)
        {
            if (index < 0 || index >= Count(n))
                return;
            var text = n.Rich(markup);
            var entry = HudEntry(n, index);
            if (entry != null)
                entry.Element.Text = text;
            else if (n.TermDrop != null) n.TermDrop.List[index].Text = text;
            else if (n.TermList != null) n.TermList.List[index].Text = text;
        }

        /// <summary>Enables or disables an entry (disabled entries are hidden).</summary>
        /// <param name="n">The node.</param>
        /// <param name="index">Entry index.</param>
        /// <param name="enabled">True to show it.</param>
        public static void SetEnabled(UiNode n, int index, bool enabled)
        {
            if (index < 0 || index >= Count(n))
                return;
            var entry = HudEntry(n, index);
            if (entry != null)
                entry.Enabled = enabled;
            else if (n.TermDrop != null) n.TermDrop.List[index].Enabled = enabled;
            else if (n.TermList != null) n.TermList.List[index].Enabled = enabled;
        }

        /// <summary>Key of an entry, null outside the list.</summary>
        /// <param name="n">The node.</param>
        /// <param name="index">Entry index.</param>
        public static string Key(UiNode n, int index)
        {
            if (index < 0 || index >= Count(n))
                return null;
            var entry = HudEntry(n, index);
            if (entry != null)
                return entry.AssocMember;
            if (n.TermDrop != null) return n.TermDrop.List[index].AssocObject;
            if (n.TermList != null) return n.TermList.List[index].AssocObject;
            return null;
        }

        /// <summary>Index of the first entry with this key, -1 if none.</summary>
        /// <param name="n">The node.</param>
        /// <param name="key">The key.</param>
        public static int Find(UiNode n, string key)
        {
            int count = Count(n);
            for (int i = 0; i < count; i++)
                if (Key(n, i) == key)
                    return i;
            return -1;
        }

        /// <summary>Selected index, -1 if none.</summary>
        /// <param name="n">The node.</param>
        public static int SelectedIndex(UiNode n)
        {
            if (n.Drop != null) return n.Drop.SelectionIndex;
            if (n.Items != null) return n.Items.SelectionIndex;
            if (n.TreeL != null) return n.TreeL.selectionBox.SelectionIndex;
            if (n.TermDrop != null) return n.TermDrop.List.SelectionIndex;
            if (n.TermList != null) return n.TermList.List.SelectionIndex;
            return -1;
        }

        /// <summary>Selects an entry; -1 clears the selection (list box and tree list only).</summary>
        /// <param name="n">The node.</param>
        /// <param name="index">Entry index or -1.</param>
        public static void SetSelectedIndex(UiNode n, int index)
        {
            int count = Count(n);
            if (index >= count)
                return;
            if (index < 0)
            {
                if (n.Items != null) n.Items.ClearSelection();
                else if (n.TreeL != null) n.TreeL.ClearSelection();
                return;
            }
            if (n.Drop != null) n.Drop.SetSelectionAt(index);
            else if (n.Items != null) n.Items.SetSelectionAt(index);
            else if (n.TreeL != null) n.TreeL.SetSelectionAt(index);
            else if (n.TermDrop != null) n.TermDrop.List.SetSelection(index);
            else if (n.TermList != null) n.TermList.List.SetSelection(index);
        }

        /// <summary>Key of the selected entry, null if none.</summary>
        /// <param name="n">The node.</param>
        public static string SelectedKey(UiNode n) { return Key(n, SelectedIndex(n)); }

        /// <summary>Selects the first entry with this key; null or an unknown key clears the selection.</summary>
        /// <param name="n">The node.</param>
        /// <param name="key">The key.</param>
        public static void SelectKey(UiNode n, string key) { SetSelectedIndex(n, key == null ? -1 : Find(n, key)); }

        private static ListBoxEntry<string> HudEntry(UiNode n, int index)
        {
            IReadOnlyList<ListBoxEntry<string>> list = null;
            if (n.Drop != null) list = n.Drop.EntryList;
            else if (n.Items != null) list = n.Items.EntryList;
            else if (n.TreeL != null) list = n.TreeL.EntryList;
            return list != null && index < list.Count ? list[index] : null;
        }
    }
}
