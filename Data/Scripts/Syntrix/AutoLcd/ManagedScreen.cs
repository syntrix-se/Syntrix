// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using VRage.Game.GUI.TextPanel;
using VRageMath;
using IMyTextSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;

namespace Syntrix.AutoLcd
{
    /// <summary>
    /// One screen AutoLCD writes to: a text panel or one surface of a block with screens. Builds its lines command
    /// by command in small steps (each command again only after its interval, else its last output), shows them
    /// scrolled and split over linked panels, and writes only when the text changed. Also boot frames and clear.
    /// </summary>
    internal class ManagedScreen
    {
        private const double RefreshSeconds = 1;

        private readonly List<string> _content = new List<string>();
        private readonly List<string> _building = new List<string>();
        private readonly StringBuilder _text = new StringBuilder();
        private readonly Dictionary<IMyTextSurface, string> _written = new Dictionary<IMyTextSurface, string>();
        private IEnumerator<int> _work;
        private CommandContext _context;
        private string _layout = "";
        private int _registryVersion = -1;
        private double _nextBuild;
        private int _offset;
        private double _nextScroll;
        private int _scrollWait;
        private int _bootFrame = -1;
        private double _bootNext;
        private bool _cleared;
        private bool _clearWritten;

        public ManagedScreen(IMyTerminalBlock block, int surfaceIndex, IMyTextSurface surface)
        {
            Block = block;
            SurfaceIndex = surfaceIndex;
            Surface = surface;
            Errors = new List<string>();
            Linked = new List<ManagedScreen>();
        }

        public IMyTerminalBlock Block { get; private set; }

        public int SurfaceIndex { get; private set; }

        public IMyTextSurface Surface { get; private set; }

        /// <summary>Commands of the screen (from the custom data of the block, or of the link master).</summary>
        public List<CommandLine> Commands;

        /// <summary>Programmable block settings that cover the screen; null = server settings.</summary>
        public ScriptContext Context;

        /// <summary>Id of the construct for fair scheduling.</summary>
        public long ConstructId;

        /// <summary>Spaces left and right (!MARGIN:n).</summary>
        public int Margin;

        /// <summary>No automatic scrolling (!NOSCROLL).</summary>
        public bool NoScroll;

        /// <summary>Link group name (!LINK:G n), null if not linked.</summary>
        public string LinkGroup;

        public int LinkIndex;

        /// <summary>Master of a link group: the other panels below it, in order.</summary>
        public List<ManagedScreen> Linked { get; private set; }

        /// <summary>Panel of a link group that is not the first; it is written by its master.</summary>
        public ManagedScreen Master;

        /// <summary>Problems of the last build (shown in the info text).</summary>
        public List<string> Errors { get; private set; }

        /// <summary>Game time of the last finished build.</summary>
        public double LastBuilt = double.NaN;

        /// <summary>Milliseconds spent on this screen since the last statistics reset.</summary>
        public double Milliseconds;

        /// <summary>Lines of the last build.</summary>
        public int LineCount
        {
            get { return _content.Count; }
        }

        public IMyTerminalBlock Reference
        {
            get { return Context != null ? (IMyTerminalBlock)Context.Block : Block; }
        }

        public long Owner
        {
            get { return Context != null ? Context.Block.OwnerId : Block.OwnerId; }
        }

        private AutoLcdSettings Settings
        {
            get { return AutoLcdCore.Settings; }
        }

        private bool SlowMode
        {
            get { return Context != null ? Context.SlowMode : Settings.SlowMode; }
        }

        private int ScrollLines
        {
            get { return Context != null ? Context.ScrollLines : Settings.ScrollLines; }
        }

        private bool SkipContentType
        {
            get { return Context != null ? Context.SkipContentType : Settings.SkipContentType; }
        }

        public bool BootEnabled
        {
            get { return Context != null ? Context.EnableBoot : Settings.EnableBoot; }
        }

        /// <summary>Records a problem of a command line.</summary>
        public void AddError(CommandLine line, string text)
        {
            if (Errors.Count < 20)
                Errors.Add((line != null ? LcdText.Get("SyxAutoLcd_Line", "Line") + " " + line.SourceLine + ": " : "") + text);
        }

        /// <summary>Forgets cached outputs (after the commands, the font or the language changed).</summary>
        public void Invalidate()
        {
            if (Commands != null)
                foreach (var line in Commands)
                    line.LastRun = double.MinValue;
            _work = null;
            _nextBuild = 0;
            _layout = "";
        }

        /// <summary>Scrolls by lines (negative = up).</summary>
        public void Scroll(int lines)
        {
            if (Master != null)
            {
                Master.Scroll(lines);
                return;
            }
            _offset = Math.Max(0, _offset + lines);
            _scrollWait = 0;
            _nextScroll = AutoLcdCore.Now + Settings.ScrollSeconds * 3;
            Present();
        }

        /// <summary>Empties the screen until it is booted again.</summary>
        public void Clear()
        {
            _cleared = true;
            _clearWritten = false;
            _bootFrame = -1;
        }

        /// <summary>Shows the boot frames, then the content again.</summary>
        public void Boot()
        {
            _cleared = false;
            _bootFrame = 0;
            _bootNext = 0;
            _offset = 0;
        }

        /// <summary>Does one piece of work; false if nothing was due.</summary>
        public bool Step(double now, bool visible)
        {
            if (Master != null || Block.Closed)
                return false;
            if (_cleared)
            {
                if (_clearWritten)
                    return false;
                WriteAll("");
                _clearWritten = true;
                return true;
            }
            bool did = false;
            if (_bootFrame >= 0 && now >= _bootNext && visible)
            {
                ShowBootFrame();
                _bootNext = now + 1;
                did = true;
            }
            if (_work == null)
            {
                if (now < _nextBuild || (!visible && !double.IsNaN(LastBuilt)))
                {
                    if (!did && _bootFrame < 0 && visible && now >= _nextScroll)
                        return AutoScroll(now);
                    return did;
                }
                _work = Build(now).GetEnumerator();
            }
            bool more;
            try
            {
                more = _work.MoveNext();
            }
            catch (Exception e)
            {
                AutoLcdCore.Log.Error("screen " + Block.CustomName, e);
                more = false;
            }
            if (!more)
            {
                _work = null;
                _nextBuild = now + RefreshSeconds;
                LastBuilt = now;
                if (_bootFrame < 0)
                    Present();
            }
            return true;
        }

        private IEnumerable<int> Build(double now)
        {
            if (_registryVersion != CommandRegistry.Version)
            {
                _registryVersion = CommandRegistry.Version;
                if (Commands != null)
                    foreach (var line in Commands)
                        line.Handler = null;
            }
            float width;
            var metrics = Layout(out width);
            var source = BlockSource.For(Reference, Owner);
            if (_context == null || _context.Source != source)
                _context = new CommandContext(this, source, Settings);
            Errors.Clear();
            _building.Clear();
            if (Commands == null || Commands.Count == 0)
            {
                _building.Add(LcdText.Get("SyxAutoLcd_NoCommands", "Write AutoLCD commands into the custom data of this block."));
                _building.Add(LcdText.Get("SyxAutoLcd_NoCommandsHelp", "Help: chat /syxlcd help"));
            }
            else
            {
                double slow = SlowMode ? 4 : 1;
                foreach (var line in Commands)
                {
                    if (line.Handler == null && !CommandRegistry.Resolve(line))
                    {
                        string message = LcdText.Get("SyxAutoLcd_UnknownCommand", "Unknown command:") + " " + line.Word;
                        AddError(line, message);
                        _building.Add(message);
                        continue;
                    }
                    if (line.LastRun == double.MinValue || now - line.LastRun >= line.Handler.Interval * slow)
                    {
                        var canvas = new LineCanvas(metrics, width, Margin, Settings);
                        _context.Begin(line, canvas);
                        var run = line.Handler.Provider != null ? CommandRegistry.RunProvider(_context) : line.Handler.Run(_context);
                        var steps = run.GetEnumerator();
                        while (true)
                        {
                            bool more;
                            try
                            {
                                more = steps.MoveNext();
                            }
                            catch (Exception e)
                            {
                                AutoLcdCore.Log.Error("command " + line.Word + " on " + Block.CustomName, e);
                                AddError(line, LcdText.Get("SyxAutoLcd_CommandError", "Command failed:") + " " + line.Word);
                                more = false;
                            }
                            if (!more)
                                break;
                            yield return 0;
                        }
                        line.Output.Clear();
                        line.Output.AddRange(canvas.Lines);
                        line.LastRun = now;
                        yield return 0;
                    }
                    _building.AddRange(line.Output);
                    if (_building.Count >= Settings.MaxLinesPerScreen)
                        break;
                }
            }
            if (_building.Count > Settings.MaxLinesPerScreen)
                _building.RemoveRange(Settings.MaxLinesPerScreen, _building.Count - Settings.MaxLinesPerScreen);
            _content.Clear();
            _content.AddRange(_building);
        }

        // Font metrics and usable width at scale 1; forgets cached outputs when the layout changed.
        private FontMetrics Layout(out float width)
        {
            var surface = Surface;
            string font = surface.Font;
            var metrics = FontMetrics.For(font, surface);
            float scale;
            Vector2 usable = Usable(surface, out scale);
            width = usable.X / scale;
            string layout = font + "|" + surface.FontSize + "|" + usable.X + "|" + Margin;
            if (layout != _layout)
            {
                if (Commands != null)
                    foreach (var line in Commands)
                        line.LastRun = double.MinValue;
                _layout = layout;
            }
            return metrics;
        }

        // Usable area in texture pixels and the text scale of a surface.
        private Vector2 Usable(IMyTextSurface surface, out float scale)
        {
            Vector2 size = surface.SurfaceSize;
            Vector2 texture = surface.TextureSize;
            var factor = Settings.ScreenOf(Block.BlockDefinition.SubtypeId);
            if (factor != null)
                size = new Vector2(size.X * factor.Width, size.Y * factor.Height);
            float padding = 1f - surface.TextPadding * 0.02f;
            scale = Math.Max(0.01f, surface.FontSize * Math.Min(texture.X, texture.Y) / 512f);
            return new Vector2(size.X * padding, size.Y * padding);
        }

        // Number of lines a surface shows with the font of the master.
        private int Capacity(IMyTextSurface surface, FontMetrics metrics)
        {
            float scale;
            Vector2 usable = Usable(surface, out scale);
            return Math.Max(1, (int)Math.Floor(usable.Y / (metrics.LineHeight * scale) + 0.001f));
        }

        private bool AutoScroll(double now)
        {
            _nextScroll = now + Settings.ScrollSeconds;
            if (NoScroll)
                return false;
            int capacity = TotalCapacity();
            if (_content.Count <= capacity)
            {
                if (_offset == 0)
                    return false;
                _offset = 0;
                Present();
                return true;
            }
            int last = _content.Count - capacity;
            if (_offset >= last)
            {
                if (++_scrollWait < 2)
                    return false;
                _scrollWait = 0;
                _offset = 0;
            }
            else if (_offset == 0 && _scrollWait == 0)
            {
                _scrollWait = 1;
                return false;
            }
            else
            {
                _scrollWait = 0;
                _offset = Math.Min(last, _offset + ScrollLines);
            }
            Present();
            return true;
        }

        private int TotalCapacity()
        {
            var metrics = FontMetrics.For(Surface.Font, Surface);
            int capacity = Capacity(Surface, metrics);
            foreach (var screen in Linked)
                capacity += Capacity(screen.Surface, metrics);
            return capacity;
        }

        /// <summary>Writes the visible part of the content to the surface(s).</summary>
        public void Present()
        {
            var metrics = FontMetrics.For(Surface.Font, Surface);
            int total = TotalCapacity();
            int maxOffset = Math.Max(0, _content.Count - total);
            if (_offset > maxOffset)
                _offset = maxOffset;
            int index = _offset;
            index = Write(Surface, index, Capacity(Surface, metrics));
            foreach (var screen in Linked)
            {
                if (screen.Surface.Font != Surface.Font)
                    screen.Surface.Font = Surface.Font;
                if (Math.Abs(screen.Surface.FontSize - Surface.FontSize) > 0.0001f)
                    screen.Surface.FontSize = Surface.FontSize;
                index = Write(screen.Surface, index, Capacity(screen.Surface, metrics));
            }
        }

        private int Write(IMyTextSurface surface, int from, int count)
        {
            _text.Clear();
            int end = Math.Min(_content.Count, from + count);
            for (int i = from; i < end; i++)
            {
                if (i > from)
                    _text.Append('\n');
                _text.Append(_content[i]);
            }
            WriteText(surface, _text.ToString());
            return end;
        }

        private void WriteAll(string text)
        {
            WriteText(Surface, text);
            foreach (var screen in Linked)
                WriteText(screen.Surface, text);
        }

        private void WriteText(IMyTextSurface surface, string text)
        {
            if (!SkipContentType && surface.ContentType != ContentType.TEXT_AND_IMAGE)
                surface.ContentType = ContentType.TEXT_AND_IMAGE;
            string last;
            if (_written.TryGetValue(surface, out last) && last == text)
                return;
            _written[surface] = text;
            surface.WriteText(text, false);
        }

        private void ShowBootFrame()
        {
            var frames = Settings.BootFrames.Count > 0
                ? Settings.BootFrames
                : new List<string>
                {
                    LcdText.Get("SyxAutoLcd_Boot1", "Starting systems"),
                    LcdText.Get("SyxAutoLcd_Boot2", "Checking connections"),
                    LcdText.Get("SyxAutoLcd_Boot3", "Loading commands"),
                };
            if (_bootFrame >= frames.Count)
            {
                _bootFrame = -1;
                _offset = 0;
                Present();
                return;
            }
            float width;
            var metrics = Layout(out width);
            var canvas = new LineCanvas(metrics, width, Margin, Settings);
            canvas.Add("");
            canvas.Text(frames[_bootFrame].Replace("\r", "").Trim('\n'));
            canvas.Add("");
            canvas.Bar((double)(_bootFrame + 1) / frames.Count);
            _bootFrame++;
            _text.Clear();
            for (int i = 0; i < canvas.Lines.Count; i++)
            {
                if (i > 0)
                    _text.Append('\n');
                _text.Append(canvas.Lines[i]);
            }
            WriteAll(_text.ToString());
        }

        /// <summary>Status lines for the info text of the block.</summary>
        public string Status()
        {
            var text = new StringBuilder();
            text.Append(LcdText.Get("SyxAutoLcd_InfoScreen", "AutoLCD screen")).Append(' ').Append(SurfaceIndex);
            if (Master != null)
                text.Append(" (").Append(LcdText.Get("SyxAutoLcd_InfoLinked", "linked to")).Append(' ').Append(Master.Block.CustomName).Append(')');
            text.Append('\n');
            if (Context != null)
                text.Append(LcdText.Get("SyxAutoLcd_InfoProgram", "Settings of")).Append(' ').Append(Context.Block.CustomName).Append('\n');
            if (!double.IsNaN(LastBuilt))
                text.Append(LcdText.Get("SyxAutoLcd_InfoUpdated", "Updated")).Append(' ')
                    .Append(Formatting.Duration(Math.Max(0, AutoLcdCore.Now - LastBuilt))).Append(' ')
                    .Append(LcdText.Get("SyxAutoLcd_InfoAgo", "ago")).Append(", ").Append(_content.Count).Append(' ')
                    .Append(LcdText.Get("SyxAutoLcd_InfoLines", "lines")).Append('\n');
            foreach (string error in Errors)
                text.Append(error).Append('\n');
            return text.ToString();
        }
    }
}
