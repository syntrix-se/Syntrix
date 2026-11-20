// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;
using System.Collections.Generic;
using System.Text;

namespace Syntrix.AutoLcd
{
    /// <summary>How a value block is shown (variants of the commands).</summary>
    internal enum StatMode
    {
        /// <summary>Title with values, then bar with percent.</summary>
        Full,

        /// <summary>Title with percent only (variant X).</summary>
        Percent,

        /// <summary>Title with percent, then bar (variant P).</summary>
        PercentBar,

        /// <summary>Title with values only (variant V).</summary>
        Values,

        /// <summary>Bar only (variant Bar).</summary>
        Bar,
    }

    /// <summary>
    /// Builds the text lines of a screen: left, right and centred text, two texts on one line, and text bars, all
    /// fitted to the width of the screen with the widths of its font.
    /// </summary>
    internal class LineCanvas
    {
        private readonly FontMetrics _metrics;
        private readonly AutoLcdSettings _settings;
        private readonly StringBuilder _line = new StringBuilder();
        private readonly string _margin;

        public LineCanvas(FontMetrics metrics, float width, int marginSpaces, AutoLcdSettings settings)
        {
            _metrics = metrics;
            _settings = settings;
            _margin = marginSpaces > 0 ? new string(' ', marginSpaces) : "";
            Width = Math.Max(1f, width - 2 * marginSpaces * metrics.SpaceWidth);
            Lines = new List<string>();
        }

        /// <summary>Usable line width at scale 1 (without margins).</summary>
        public float Width { get; private set; }

        /// <summary>Lines built so far (with margin).</summary>
        public List<string> Lines { get; private set; }

        public FontMetrics Metrics
        {
            get { return _metrics; }
        }

        /// <summary>Width of a text at scale 1.</summary>
        public float Measure(string text)
        {
            return _metrics.Width(text);
        }

        /// <summary>Left aligned text; line breaks make several lines.</summary>
        public void Text(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                Add("");
                return;
            }
            foreach (string part in text.Replace("\r", "").Split('\n'))
                Add(part);
        }

        /// <summary>Right aligned text.</summary>
        public void Right(string text)
        {
            text = text ?? "";
            Add(Spaces(Width - _metrics.Width(text)) + text);
        }

        /// <summary>Centred text.</summary>
        public void Center(string text)
        {
            text = text ?? "";
            Add(Spaces((Width - _metrics.Width(text)) / 2) + text);
        }

        /// <summary>Text on the left and text on the right of one line.</summary>
        public void Pair(string left, string right)
        {
            Add(PairText(left, right));
        }

        /// <summary>Left text, bar over the free space, right text.</summary>
        public void PairBar(string left, double fraction, string right)
        {
            left = left ?? "";
            right = right ?? "";
            float gap = Width - _metrics.Width(left) - _metrics.Width(right) - 2 * _metrics.SpaceWidth;
            if (gap < 4 * _metrics.Width(_settings.BarFull[0]) + BarEndsWidth())
            {
                Pair(left, right);
                return;
            }
            _line.Clear();
            _line.Append(left);
            if (left.Length > 0)
                _line.Append(' ');
            AppendBar(_line, fraction, gap + (left.Length > 0 ? 0 : _metrics.SpaceWidth) + (right.Length > 0 ? 0 : _metrics.SpaceWidth));
            if (right.Length > 0)
                _line.Append(' ').Append(right);
            Add(_line.ToString());
        }

        /// <summary>Bar over the whole width.</summary>
        public void Bar(double fraction)
        {
            _line.Clear();
            AppendBar(_line, fraction, Width);
            Add(_line.ToString());
        }

        /// <summary>A value block: title, values, fill fraction, shown as the mode says.</summary>
        public void Stat(string title, string values, double fraction, StatMode mode)
        {
            string percent = Formatting.Percent(fraction);
            switch (mode)
            {
                case StatMode.Percent:
                    Pair(title, percent);
                    break;
                case StatMode.PercentBar:
                    Pair(title, percent);
                    Bar(fraction);
                    break;
                case StatMode.Values:
                    Pair(title, values);
                    break;
                case StatMode.Bar:
                    Bar(fraction);
                    break;
                default:
                    Pair(title, values);
                    PairBar("", fraction, percent);
                    break;
            }
        }

        /// <summary>Two texts on one line as a string (for commands that build their own lines).</summary>
        public string PairText(string left, string right)
        {
            left = left ?? "";
            right = right ?? "";
            if (right.Length == 0)
                return left;
            float gap = Width - _metrics.Width(left) - _metrics.Width(right);
            string spaces = Spaces(gap);
            if (spaces.Length == 0)
                spaces = " ";
            return left + spaces + right;
        }

        /// <summary>Spaces filling about the given width.</summary>
        public string Spaces(float width)
        {
            int count = (int)Math.Floor(width / Math.Max(0.1f, _metrics.SpaceWidth));
            return count > 0 ? new string(' ', Math.Min(count, 400)) : "";
        }

        /// <summary>Adds a finished line (the margin is put in front).</summary>
        public void Add(string line)
        {
            Lines.Add(_margin.Length > 0 ? _margin + line : line);
        }

        private float BarEndsWidth()
        {
            return _metrics.Width(_settings.BarStart) + _metrics.Width(_settings.BarEnd);
        }

        private void AppendBar(StringBuilder target, double fraction, float width)
        {
            if (double.IsNaN(fraction))
                fraction = 0;
            fraction = Math.Max(0, Math.Min(1, fraction));
            float inner = Math.Max(0, width - BarEndsWidth());
            float full = Math.Max(0.1f, _metrics.Width(_settings.BarFull[0]));
            float empty = Math.Max(0.1f, _metrics.Width(_settings.BarEmpty[0]));
            int fullCount = (int)Math.Round(fraction * inner / full);
            int emptyCount = (int)Math.Floor((inner - fullCount * full) / empty);
            target.Append(_settings.BarStart);
            target.Append(_settings.BarFull[0], Math.Max(0, fullCount));
            target.Append(_settings.BarEmpty[0], Math.Max(0, emptyCount));
            target.Append(_settings.BarEnd);
        }
    }
}
