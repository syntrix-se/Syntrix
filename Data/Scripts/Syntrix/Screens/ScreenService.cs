// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using Syntrix.Blocks;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using IngameSurface = Sandbox.ModAPI.Ingame.IMyTextSurface;
using IngameProvider = Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;

namespace Syntrix.Screens
{
    /// <summary>
    /// Fixed screens (API 4, players only). A mod registers a block type and a screen index with a content function;
    /// Syntrix asks it about every 100 frames for each such block within 150 m of the camera and draws the lines in one
    /// board style, only when the content changed (or someone changed the screen). The screen settings leave the K menu
    /// of the blocks whose content is not empty (1.5.0: before, of every block of the type).
    /// Content: lines separated by "\n", each "kind\ttext"; the title takes "T\tsymbol\ttext" (symbol = LCD texture,
    /// may be empty). Kinds: A alarm bar, T title, N name, S small, V value, H hint, F foot (bottom line), G gap.
    /// "{F}" in a text is replaced by the key of the use control. Null or "" leaves the screen as it is.
    /// The board scales with the screen width and, since 1.5.0, shrinks further until all lines fit the height.
    /// </summary>
    public sealed class ScreenService
    {
        private const double DrawDistance = 150;
        private const int Interval = 100;
        private static readonly Color Background = new Color(8, 14, 20);
        private static readonly Color Accent = new Color(132, 196, 228);
        private static readonly Color Text = new Color(220, 228, 234);
        private static readonly Color Quiet = new Color(130, 142, 150);
        private static readonly Color AlarmColor = new Color(200, 60, 40);

        /// <summary>Terminal controls of the screen settings (text panel component), removed for registered blocks.</summary>
        private static readonly HashSet<string> ScreenControls = new HashSet<string>
        {
            "PanelList", "Content", "ShowTextPanel", "Font", "FontSize", "FontColor", "alignment", "TextPaddingSlider",
            "BackgroundColor", "ImageList", "SelectTextures", "ChangeIntervalSlider", "SelectedImageList",
            "RemoveSelectedTextures", "PreserveAspectRatio", "Script", "ScriptForegroundColor", "ScriptBackgroundColor",
        };

        private struct Entry
        {
            public string Mod;
            public Func<long, string> Content;
        }

        private struct Line
        {
            public char Kind;
            public string Symbol;
            public string Text;
        }

        private readonly SyntrixCore _core;
        private readonly BlockWatch _watch;
        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();   // "TypeId/SubtypeId|surface"
        private readonly HashSet<string> _types = new HashSet<string>();
        private readonly Dictionary<long, IMyTerminalBlock> _blocks = new Dictionary<long, IMyTerminalBlock>();
        private readonly Dictionary<string, string> _drawn = new Dictionary<string, string>();   // "entity|surface" -> content
        private readonly List<long> _gone = new List<long>();
        private bool _watching, _controlsHooked;
        private int _tick;

        /// <summary>Makes the service.</summary>
        public ScreenService(SyntrixCore core, BlockWatch watch)
        {
            _core = core;
            _watch = watch;
        }

        /// <summary>Registers the content function of a screen of a block type; null removes it (only the mod that registered it).</summary>
        public void Register(string mod, string blockKey, int surface, Func<long, string> content)
        {
            if (!_core.IsPlayer || string.IsNullOrEmpty(blockKey) || surface < 0)
                return;
            string key = blockKey + "|" + surface;
            Entry old;
            bool had = _entries.TryGetValue(key, out old);
            if (content == null)
            {
                if (had && old.Mod == mod)
                    _entries.Remove(key);
                RebuildTypes();
                return;
            }
            if (had && old.Mod != mod)
                Log.Warn("Screen " + key + " taken over by " + mod + " from " + old.Mod);
            _entries[key] = new Entry { Mod = mod, Content = content };
            RebuildTypes();
            if (!_controlsHooked)
            {
                _controlsHooked = true;
                MyAPIGateway.TerminalControls.CustomControlGetter += HideScreenControls;
            }
            if (!_watching)
            {
                _watching = true;
                _watch.OnAdded(Added);
            }
            else
                _watch.ForEach(Added);
        }

        /// <summary>Draws the screens of a block again at the next pass (e.g. after its state changed).</summary>
        public void Refresh(long entityId)
        {
            var prefix = entityId + "|";
            var keys = new List<string>();
            foreach (var k in _drawn.Keys)
                if (k.StartsWith(prefix))
                    keys.Add(k);
            foreach (var k in keys)
                _drawn.Remove(k);
        }

        /// <summary>Unhooks and forgets everything.</summary>
        public void Stop()
        {
            if (_controlsHooked)
                MyAPIGateway.TerminalControls.CustomControlGetter -= HideScreenControls;
            _controlsHooked = false;
            _entries.Clear();
            _types.Clear();
            _blocks.Clear();
            _drawn.Clear();
        }

        /// <summary>Every frame (players).</summary>
        public void Tick()
        {
            if (_blocks.Count == 0 || ++_tick % Interval != 0)
                return;
            var camera = MyAPIGateway.Session?.Camera;
            if (camera == null)
                return;
            var eye = camera.WorldMatrix.Translation;
            _gone.Clear();
            foreach (var pair in _blocks)
            {
                var block = pair.Value;
                if (block.Closed || block.CubeGrid == null)
                {
                    _gone.Add(pair.Key);
                    continue;
                }
                if (Vector3D.DistanceSquared(eye, block.WorldMatrix.Translation) > DrawDistance * DrawDistance)
                    continue;
                string type = UseKey(block);
                var provider = block as IngameProvider;
                if (provider == null)
                    continue;
                for (int i = 0; i < provider.SurfaceCount; i++)
                {
                    Entry e;
                    if (_entries.TryGetValue(type + "|" + i, out e))
                        Update(block, provider.GetSurface(i), i, e);
                }
            }
            foreach (var id in _gone)
            {
                _blocks.Remove(id);
                Refresh(id);
            }
        }

        private void RebuildTypes()
        {
            _types.Clear();
            foreach (var k in _entries.Keys)
                _types.Add(k.Substring(0, k.LastIndexOf('|')));
        }

        private void Added(IMySlimBlock slim)
        {
            var block = slim.FatBlock as IMyTerminalBlock;
            if (block != null && _types.Contains(BlockWatch.KeyOf(slim)))
                _blocks[block.EntityId] = block;
        }

        private static string UseKey(IMyCubeBlock block)
        {
            var id = block.BlockDefinition;
            return id.TypeId.ToString().Replace("MyObjectBuilder_", "") + "/" + id.SubtypeName;
        }

        private void HideScreenControls(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if (block == null)
                return;
            string type = UseKey(block);
            if (!_types.Contains(type))
                return;
            // only blocks that show a board lose their screen settings; the others of the type keep them
            bool drawn = false;
            foreach (var pair in _entries)
            {
                if (!pair.Key.StartsWith(type + "|", StringComparison.Ordinal))
                    continue;
                try { drawn = !string.IsNullOrEmpty(pair.Value.Content(block.EntityId)); }
                catch (Exception e) { Log.Error("screen content of " + pair.Value.Mod, e); }
                if (drawn)
                    break;
            }
            if (drawn)
                controls.RemoveAll(c => c != null && ScreenControls.Contains(c.Id));
        }

        private void Update(IMyTerminalBlock block, IngameSurface surface, int index, Entry e)
        {
            if (surface == null)
                return;
            string content;
            try { content = e.Content(block.EntityId); }
            catch (Exception ex)
            {
                Log.Error("screen content of " + e.Mod, ex);
                return;
            }
            if (string.IsNullOrEmpty(content))
                return;
            string key = block.EntityId + "|" + index;
            string old;
            if (surface.ContentType == ContentType.SCRIPT && surface.Script == "" && _drawn.TryGetValue(key, out old) && old == content)
                return;
            try
            {
                Draw(surface, Parse(content));
                _drawn[key] = content;
            }
            catch (Exception ex)
            {
                Log.Error("screen of " + e.Mod, ex);
            }
        }

        private static List<Line> Parse(string content)
        {
            var lines = new List<Line>();
            string useKey = null;
            foreach (var raw in content.Split('\n'))
            {
                var row = raw.TrimEnd('\r');
                if (row.Length == 0)
                    continue;
                var parts = row.Split('\t');
                var line = new Line { Kind = parts[0].Length > 0 ? parts[0][0] : 'G', Symbol = "", Text = "" };
                if (line.Kind == 'T' && parts.Length > 2)
                {
                    line.Symbol = parts[1];
                    line.Text = parts[2];
                }
                else if (parts.Length > 1)
                    line.Text = parts[1];
                if (line.Text.Contains("{F}"))
                {
                    if (useKey == null)
                        useKey = UseControlKey();
                    line.Text = line.Text.Replace("{F}", useKey);
                }
                lines.Add(line);
            }
            return lines;
        }

        private static string UseControlKey()
        {
            var control = MyAPIGateway.Input.GetGameControl(MyStringId.GetOrCompute("USE"));
            return control != null ? control.GetControlButtonName(VRage.Input.MyGuiInputDeviceEnum.Keyboard) : "F";
        }

        private static void Draw(IngameSurface surface, List<Line> lines)
        {
            surface.ContentType = ContentType.SCRIPT;
            surface.Script = "";
            surface.ScriptBackgroundColor = Background;
            var view = new RectangleF((surface.TextureSize - surface.SurfaceSize) / 2f, surface.SurfaceSize);
            float k = view.Width / 320f;          // reference width 320
            // 1.5.0: shrink until the lines fit the height (wide, low screens such as the store block); wrapping changes
            // with the scale, so measure again after each step
            bool foot = false;
            foreach (var line in lines)
                foot |= line.Kind == 'F';
            for (int i = 0; i < 8; i++)
            {
                float room = view.Height - (foot ? 52f * k : 10f * k);
                float used = Layout(null, surface, view, lines, k) - view.Y;
                if (used <= room)
                    break;
                k *= Math.Max(0.5f, room / used * 0.98f);
            }
            using (var frame = surface.DrawFrame())
                Layout(frame, surface, view, lines, k);
        }

        /// <summary>Lays out the lines at scale k; adds the sprites when frame is given, else only measures. Returns the
        /// bottom of the text (without the foot line).</summary>
        private static float Layout(MySpriteDrawFrame? frame, IngameSurface surface, RectangleF view, List<Line> lines, float k)
        {
            float margin = 18f * k;
            float width = view.Width - 2f * margin;
            float y = view.Y + 14f * k;
            foreach (var line in lines)
            {
                switch (line.Kind)
                {
                    case 'A':
                        float h = Wrap(surface, line.Text, 0.62f * k, width - 12f * k).Count * 22f * k + 12f * k;
                        Add(frame, Box(new Vector2(view.X, y), new Vector2(view.Width, h), AlarmColor));
                        y = Paragraph(frame, surface, line.Text, new Vector2(view.X + margin, y + 6f * k), 0.62f * k, width, Color.White, 22f * k) + 12f * k;
                        break;
                    case 'T':
                        float s = 44f * k;
                        float x = view.X + margin;
                        if (!string.IsNullOrEmpty(line.Symbol))
                        {
                            Add(frame, new MySprite(SpriteType.TEXTURE, line.Symbol, new Vector2(x + s / 2f, y + s / 2f), new Vector2(s, s), Color.White));
                            x += s + 10f * k;
                        }
                        Add(frame, new MySprite(SpriteType.TEXT, line.Text, new Vector2(x, y + 6f * k), null, Accent, "White", TextAlignment.LEFT, 1.0f * k));
                        y += s + 6f * k;
                        Add(frame, Box(new Vector2(view.X + margin, y), new Vector2(width, 2f * k), Accent));
                        y += 10f * k;
                        break;
                    case 'N':
                        y = Paragraph(frame, surface, line.Text, new Vector2(view.X + margin, y), 0.8f * k, width, Text, 27f * k);
                        break;
                    case 'S':
                        y = Paragraph(frame, surface, line.Text, new Vector2(view.X + margin, y), 0.6f * k, width, Quiet, 21f * k);
                        break;
                    case 'V':
                        y = Paragraph(frame, surface, line.Text, new Vector2(view.X + margin, y), 0.75f * k, width, Text, 26f * k);
                        break;
                    case 'H':
                        y = Paragraph(frame, surface, "• " + line.Text, new Vector2(view.X + margin, y), 0.6f * k, width, Text, 21f * k) + 4f * k;
                        break;
                    case 'F':
                        float fy = view.Y + view.Height - 40f * k;
                        Add(frame, Box(new Vector2(view.X + margin, fy - 8f * k), new Vector2(width, 2f * k), Accent));
                        Add(frame, new MySprite(SpriteType.TEXT, line.Text, new Vector2(view.X + view.Width / 2f, fy), null, Accent, "White", TextAlignment.CENTER, 0.75f * k));
                        break;
                    default:
                        y += 12f * k;
                        break;
                }
            }
            return y;
        }

        private static void Add(MySpriteDrawFrame? frame, MySprite sprite)
        {
            if (frame.HasValue)
                frame.Value.Add(sprite);
        }

        private static MySprite Box(Vector2 topLeft, Vector2 size, Color color)
        {
            return new MySprite(SpriteType.TEXTURE, "SquareSimple", topLeft + size / 2f, size, color);
        }

        private static float Paragraph(MySpriteDrawFrame? frame, IngameSurface surface, string text, Vector2 at, float scale, float width,
            Color color, float lineHeight)
        {
            foreach (var row in Wrap(surface, text, scale, width))
            {
                Add(frame, new MySprite(SpriteType.TEXT, row, at, null, color, "White", TextAlignment.LEFT, scale));
                at.Y += lineHeight;
            }
            return at.Y;
        }

        private static List<string> Wrap(IngameSurface surface, string text, float scale, float width)
        {
            var rows = new List<string>();
            var row = "";
            foreach (var word in (text ?? "").Split(' '))
            {
                string test = row.Length == 0 ? word : row + " " + word;
                if (row.Length > 0 && surface.MeasureStringInPixels(new StringBuilder(test), "White", scale).X > width)
                {
                    rows.Add(row);
                    row = word;
                }
                else
                    row = test;
            }
            if (row.Length > 0)
                rows.Add(row);
            return rows;
        }
    }
}
