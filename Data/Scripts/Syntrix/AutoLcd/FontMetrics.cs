// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System.Collections.Generic;
using System.Text;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace Syntrix.AutoLcd
{
    /// <summary>
    /// Character widths of a font at scale 1, measured once per character with MeasureStringInPixels (works on
    /// dedicated servers: it reads the glyph data of the font definitions, also of mod fonts). If a font cannot be
    /// measured, widths of the Debug font (from the game's glyph data) are used.
    /// </summary>
    internal class FontMetrics
    {
        private const float GuiScale = 144f / 185f;
        private const int FallbackFirst = 32;

        // Advance widths of the Debug font for the characters 32 to 126 (Content/Fonts/white_shadow/FontDataPA.xml).
        private static readonly int[] _fallback =
        {
            8, 8, 10, 19, 20, 24, 20, 6, 9, 9, 11, 18, 9, 10, 9, 14, 19, 9, 19, 17, 19, 19, 19, 16, 19, 19, 9, 9, 18, 18,
            18, 16, 25, 21, 21, 19, 21, 18, 17, 20, 20, 8, 16, 17, 15, 26, 21, 21, 20, 21, 21, 21, 17, 20, 20, 31, 19,
            20, 19, 9, 12, 9, 18, 15, 8, 17, 17, 16, 17, 17, 9, 17, 17, 8, 8, 17, 8, 27, 17, 17, 17, 17, 10, 17, 9, 17,
            15, 27, 15, 17, 16, 9, 6, 9, 18,
        };

        private static readonly Dictionary<string, FontMetrics> _fonts = new Dictionary<string, FontMetrics>();

        private readonly Dictionary<char, float> _widths = new Dictionary<char, float>();
        private readonly StringBuilder _probe = new StringBuilder();
        private readonly string _font;
        private readonly bool _monospace;
        private IMyTextSurface _surface;
        private bool _measurable = true;

        private FontMetrics(string font, IMyTextSurface surface)
        {
            _font = font;
            _surface = surface;
            _monospace = font != null && font.IndexOf("Mono", System.StringComparison.OrdinalIgnoreCase) >= 0;
            LineHeight = 0;
            if (surface != null)
            {
                _probe.Clear().Append("Ag");
                LineHeight = surface.MeasureStringInPixels(_probe, font, 1f).Y;
            }
            if (LineHeight <= 0)
            {
                _measurable = false;
                LineHeight = 37f * GuiScale;
            }
            SpaceWidth = Width(' ');
        }

        /// <summary>Height of one line at scale 1.</summary>
        public float LineHeight { get; private set; }

        /// <summary>Width of a space at scale 1.</summary>
        public float SpaceWidth { get; private set; }

        /// <summary>Metrics of a font, measured with the given surface.</summary>
        public static FontMetrics For(string font, IMyTextSurface surface)
        {
            FontMetrics metrics;
            string key = font ?? "Debug";
            if (_fonts.TryGetValue(key, out metrics))
            {
                if (metrics._surface == null && surface != null)
                    metrics._surface = surface;
                return metrics;
            }
            metrics = new FontMetrics(key, surface);
            _fonts[key] = metrics;
            return metrics;
        }

        public static void Clear()
        {
            _fonts.Clear();
        }

        /// <summary>Width of one character at scale 1.</summary>
        public float Width(char c)
        {
            float width;
            if (_widths.TryGetValue(c, out width))
                return width;
            width = 0;
            if (_measurable && _surface != null)
            {
                _probe.Clear().Append(c, 8);
                width = _surface.MeasureStringInPixels(_probe, _font, 1f).X / 8f;
            }
            if (width <= 0)
                width = FallbackWidth(c);
            _widths[c] = width;
            return width;
        }

        /// <summary>Width of a text at scale 1.</summary>
        public float Width(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 0;
            float width = 0;
            foreach (char c in text)
                width += Width(c);
            return width;
        }

        private float FallbackWidth(char c)
        {
            if (_monospace)
                return 24f * GuiScale;
            int index = c - FallbackFirst;
            int advance = index >= 0 && index < _fallback.Length ? _fallback[index] : 17;
            return advance * GuiScale;
        }
    }
}
