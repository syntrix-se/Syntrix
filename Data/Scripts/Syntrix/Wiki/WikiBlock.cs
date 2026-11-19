// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;

namespace Syntrix.Wiki
{
    /// <summary>One block of a page: text, heading, note, table, image, list or fact sheet.</summary>
    public class WikiBlock
    {
        public WikiBlockKind Kind;
        public string De;
        public string En;

        // Table: header and rows per language, cells already split
        public string[] HeaderDe;
        public string[] HeaderEn;
        public List<string[]> RowsDe;
        public List<string[]> RowsEn;
        public float[] Widths;

        // Image (also in the fact sheet, there with image source; empty material = fact sheet with text only)
        public string Material;
        public string ImageSource;
        public float Width;
        public float Height;

        // Automatic table or list: kind and filters, filled by WikiAuto
        public string Auto;
        public string AutoSystem;
        public string AutoPlanet;

        // List
        public List<WikiEntry> Entries;
        public int Columns = 3;

        public WikiBlock(WikiBlockKind kind, string de, string en)
        {
            Kind = kind;
            De = de;
            En = en;
        }

        public string Text(bool german) => WikiData.Pick(german, De, En);

        public string[] Header(bool german) => german ? (HeaderDe ?? HeaderEn) : (HeaderEn ?? HeaderDe);

        public List<string[]> Rows(bool german)
        {
            var rows = new List<string[]>();
            int count = Math.Max(RowsDe?.Count ?? 0, RowsEn?.Count ?? 0);
            for (int i = 0; i < count; i++)
            {
                var de = RowsDe != null && i < RowsDe.Count ? RowsDe[i] : null;
                var en = RowsEn != null && i < RowsEn.Count ? RowsEn[i] : null;
                var row = german ? (de ?? en) : (en ?? de);
                if (row != null)
                    rows.Add(row);
            }
            return rows;
        }
    }
}
