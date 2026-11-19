// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;

namespace Syntrix.Wiki
{
    /// <summary>One block of a page: text, heading, note, table, image, list or fact sheet.</summary>
    public class WikiBlock
    {
        /// <summary>Kind of the block.</summary>
        public WikiBlockKind Kind;
        /// <summary>Text in German (text, heading, note, image caption, list title, profile text).</summary>
        public string De;
        /// <summary>Text in English.</summary>
        public string En;

        // Table: header and rows per language, cells already split
        /// <summary>Table header cells in German.</summary>
        public string[] HeaderDe;
        /// <summary>Table header cells in English.</summary>
        public string[] HeaderEn;
        /// <summary>Table rows in German, cells split.</summary>
        public List<string[]> RowsDe;
        /// <summary>Table rows in English, cells split.</summary>
        public List<string[]> RowsEn;
        /// <summary>Relative column widths of the table; null = by text length.</summary>
        public float[] Widths;

        // Image (also in the fact sheet, there with image source; empty material = fact sheet with text only)
        /// <summary>TransparentMaterial of the image; empty = fact sheet with text only.</summary>
        public string Material;
        /// <summary>Source of the image, shown small below it.</summary>
        public string ImageSource;
        /// <summary>Image width in reference pixels.</summary>
        public float Width;
        /// <summary>Image height in reference pixels.</summary>
        public float Height;

        // Automatic table or list: kind and filters, filled by WikiAuto
        /// <summary>Kind of the automatic table or list (e.g. Ores, Components); null = by hand.</summary>
        public string Auto;
        /// <summary>System id filter of an automatic table.</summary>
        public string AutoSystem;
        /// <summary>Planet id filter of an automatic table or list.</summary>
        public string AutoPlanet;

        // List
        /// <summary>Entries of a list.</summary>
        public List<WikiEntry> Entries;
        /// <summary>Number of list columns.</summary>
        public int Columns = 3;

        /// <summary>Creates a block of a kind with German and English text.</summary>
        public WikiBlock(WikiBlockKind kind, string de, string en)
        {
            Kind = kind;
            De = de;
            En = en;
        }

        /// <summary>Text in the language, the other one if empty.</summary>
        public string Text(bool german) => WikiData.Pick(german, De, En);

        /// <summary>Table header in the language, the other one if missing.</summary>
        public string[] Header(bool german) => german ? (HeaderDe ?? HeaderEn) : (HeaderEn ?? HeaderDe);

        /// <summary>Table rows in the language; a row missing in it is taken from the other language.</summary>
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
