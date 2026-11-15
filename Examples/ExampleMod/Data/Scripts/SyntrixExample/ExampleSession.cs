// Smallest mod that docks to Syntrix. Copy Api/SyntrixApi.cs next to this file, and for the window and
// the terminal page Api/SyntrixUi.cs, Api/SyntrixUi.Controls.cs and Api/SyntrixUi.Terminal.cs
// (needs Rich HUD Master in the world).
// MIT License, see LICENSE in the Syntrix repository.
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage;
using Syntrix.Api;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;

namespace SyntrixExample
{
    /// <summary>Example session component: docks to Syntrix and uses its services.</summary>
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class ExampleSession : MySessionComponentBase
    {
        private const string Mod = "SyntrixExample";
        private const string SettingsFile = "SyntrixExample.ini";
        private const string EventWatched = "SyntrixExample.DoorWatched";

        private readonly SyntrixApi _syx = new SyntrixApi();
        private readonly SyntrixLog _log = new SyntrixLog(Mod);
        private string _greeting = "Hello";

        // Window through the Syntrix UI service
        private SyntrixUi _ui;
        private UiWindow _window;
        private UiLabel _status;
        private UiTextField _name;
        private UiDropdown _greetings;
        private int _clicks;

        /// <summary>Docks to Syntrix and registers controls, settings and events.</summary>
        public override void LoadData()
        {
            _syx.Load(Mod, OnReady);

            // UI: the window is built when Rich HUD is ready (again after a Rich HUD restart).
            // F7 toggles it; players can change the key on the page "Syntrix Example" of the Rich HUD terminal (F2).
            _ui = new SyntrixUi(_syx);
            _ui.Load(BuildWindow);
            _ui.Bind("ToggleWindow", "F7", () => { if (_window != null) _window.Toggle(); });
            _ui.BindPage("Syntrix Example");

            // Terminal controls are only described here; Syntrix creates them when a door is first seen.
            SyntrixControls<IMyDoor>.Checkbox(_syx, "SyntrixExample_Watch", "SyntrixExample_Watch", "SyntrixExample_Watch_Tooltip",
                Mod, "watch", false);
            SyntrixControls<IMyDoor>.TextBox(_syx, "SyntrixExample_Note", "SyntrixExample_Note", null, Mod, "note");
            // A list box with several selected entries, and the same setting for programmable block scripts:
            // door.GetValueBool("SyntrixExample.Watch") / SetValueBool(...).
            SyntrixControls<IMyDoor>.Listbox(_syx, "SyntrixExample_Alarm", "SyntrixExample_Alarm", null, Mod, "alarm", true, 3,
                new List<MyTuple<string, string>>
                {
                    new MyTuple<string, string>("open", "SyntrixExample_Alarm_Open"),
                    new MyTuple<string, string>("closed", "SyntrixExample_Alarm_Closed"),
                    new MyTuple<string, string>("damaged", "SyntrixExample_Alarm_Damaged"),
                });
            SyntrixControls<IMyDoor>.PropertyBool(_syx, "SyntrixExample.Watch", Mod, "watch", false);

            // Settings: the server reads SyntrixExample.ini from the world (or writes the default),
            // every player gets the same text. Registrations like this one may come before onReady.
            _syx.SettingsLoad(SettingsFile, "greeting=Hello\n", true, ParseSettings);

            // An event sent by the server: payload is string[]; EventSender tells who sent it.
            _syx.Subscribe(EventWatched, payload =>
            {
                var args = payload as string[];
                if (args != null && args.Length == 2 && _syx.IsPlayer)
                    MyAPIGateway.Utilities.ShowMessage(Mod, "Door " + args[0] + " watched: " + args[1]);
            });
        }

        private void BuildWindow()
        {
            _window = _ui.Window("[b]Syntrix example[/b]");
            _window.Size = new Vector2(560f, 520f);
            _window.HeaderColor = new Color(20, 26, 32, 250);
            _window.BodyColor = new Color(28, 34, 41, 240);

            // Formatted text with a link; it wraps at the window width and follows resizing
            var text = _ui.Label(_window, "Text can be [b]bold[/b], [i]italic[/i], [color=#E8B931]coloured[/color] or [size=1.3]bigger[/size]."
                + "\n[link=hello]This link[/link] writes into the status line.");
            text.ParentAlign = UiAlign.InnerTopLeft;
            text.Offset = new Vector2(16f, -12f);
            text.WrapToParent(32f);
            text.OnLink(target => _status.Text = "Link clicked: [b]" + SyntrixUi.Escape(target) + "[/b]");

            // List: one panel per row, highlighted under the cursor, click copies the name
            var list = _ui.ScrollBox(_window);
            list.ParentAlign = UiAlign.InnerTopLeft;
            list.Offset = new Vector2(16f, -90f);
            list.Size = new Vector2(240f, 210f);
            list.Color = new Color(18, 23, 29, 250);
            foreach (var name in new[] { "Alpha", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot", "Golf", "Hotel" })
            {
                var row = _ui.Panel(list);
                row.Height = 30f;
                row.HighlightColor = new Color(40, 52, 62);
                var label = _ui.Label(row, name);
                label.ParentAlign = UiAlign.InnerLeft;
                label.Offset = new Vector2(10f, 0f);
                string n = name;
                row.OnClick(() => _name.Text = n);
            }

            // Text field: Enter greets
            _name = _ui.TextField(_window);
            _name.ParentAlign = UiAlign.InnerTopRight;
            _name.Offset = new Vector2(-16f, -90f);
            _name.Size = new Vector2(250f, 34f);
            _name.Placeholder = "Type a name ...";
            _name.MaxLength = 24;
            _name.OnSubmit(t => _status.Text = Greeting() + ", [b]" + SyntrixUi.Escape(t) + "[/b]!");

            // Drop-down: entries have a key (given back on selection) and a text with markup
            _greetings = _ui.Dropdown(_window);
            _greetings.ParentAlign = UiAlign.InnerTopRight;
            _greetings.Offset = new Vector2(-16f, -140f);
            _greetings.Size = new Vector2(250f, 30f);
            _greetings.Add("settings", "[i]From the settings[/i]");
            _greetings.Add("Hi", "Hi");
            _greetings.Add("Good evening", "Good evening");
            _greetings.SelectedIndex = 0;
            _greetings.OnSelected(key => _status.Text = "Greeting: [b]" + SyntrixUi.Escape(Greeting()) + "[/b]");

            // Tree box: the children are the entries; selecting one gives its handle
            var tree = _ui.TreeBox(_window, "Fleet");
            tree.ParentAlign = UiAlign.InnerTopRight;
            tree.Offset = new Vector2(-16f, -190f);
            tree.Size = new Vector2(250f, 30f);
            var miner = _ui.Label(tree, "Miner");
            _ui.Label(tree, "Hauler");
            tree.OnSelected(h => _status.Text = h == miner.Handle ? "The miner is busy." : "The hauler waits.");

            // Progress bar drawn with a bare slider bar
            var bar = _ui.SliderBar(_window);
            bar.ParentAlign = UiAlign.InnerBottomLeft;
            bar.Offset = new Vector2(16f, 64f);
            bar.Size = new Vector2(240f, 12f);
            bar.BarSize = new Vector2(240f, 12f);
            bar.SliderVisible = false;
            bar.InputEnabled = false;
            bar.Percent = 0.4f;

            var button = _ui.Button(_window, "Count");
            button.ParentAlign = UiAlign.InnerBottomRight;
            button.Offset = new Vector2(-16f, 16f);
            button.Size = new Vector2(140f, 36f);
            button.Color = new Color(24, 31, 38);
            button.HighlightColor = new Color(34, 44, 53);
            button.BorderThickness = 1f;
            button.BorderColor = new Color(42, 52, 61);
            button.OnClick(() =>
            {
                _status.Text = "Clicked [b]" + (++_clicks) + "[/b] times";
                bar.Percent = (_clicks % 10) / 10f;
            });

            _status = _ui.Label(_window, "Press " + (_ui.BindKeys("ToggleWindow") ?? "F7") + " to open and close.");
            _status.ParentAlign = UiAlign.InnerBottomLeft;
            _status.Offset = new Vector2(16f, 24f);

            BuildTerminalPage();
        }

        // A page in the Rich HUD terminal (F2): category, control page, one row with a tile of controls, a text page.
        private void BuildTerminalPage()
        {
            var category = _ui.TerminalCategory("Syntrix Example");
            var page = category.ControlPage("Options");
            var row = page.Category("Window", "Settings of the example window");
            var tile = row.Tile();
            var open = tile.CheckBox("Open the window");
            open.Value = _window.Visible;
            open.OnChanged(on => { if (on) _window.Open(); else _window.Close(); });
            var size = tile.Slider("Width", 400f, 900f);
            size.Value = _window.Width;
            size.ValueText = ((int)size.Value).ToString();
            size.OnChanged(v =>
            {
                _window.Width = v;
                size.ValueText = ((int)v).ToString();
            });
            var color = tile.ColorPicker("Header colour");
            color.Value = _window.HeaderColor;
            color.OnChanged(c => _window.HeaderColor = c);

            var help = category.TextPage("Help");
            help.SubHeader = "What this example shows";
            help.Body = "A window with a list, a text field, a drop-down, a tree and a progress bar, and this terminal page.";
        }

        private string Greeting()
        {
            var key = _greetings != null ? _greetings.SelectedKey : null;
            return key == null || key == "settings" ? _greeting : key;
        }

        private void ParseSettings(string text)
        {
            foreach (var line in text.Split('\n'))
            {
                var parts = line.Split(new[] { '=' }, 2);
                if (parts.Length == 2 && parts[0].Trim() == "greeting")
                    _greeting = parts[1].Trim();
            }
        }

        private void OnReady()
        {
            _log.Info("docked to Syntrix " + _syx.FrameworkVersion);

            // Runs on the server when a player ticks the box (after the access check) and on every player.
            _syx.BlockOnChanged(Mod, (entityId, key, value) =>
            {
                if (!_syx.IsServer || key != "watch")
                    return;
                var door = MyAPIGateway.Entities.GetEntityById(entityId) as IMyDoor;
                if (door == null)
                    return;
                _syx.Notify(0, "SyntrixExample_Changed", "Door {0}: watch {1}", new[] { door.CustomName, value });
                // To subscribers of this event on the server and on every player.
                _syx.SendEventToAll(EventWatched, door.CustomName, value);
            });

            // Chat command handled on the server.
            _syx.ChatRegister("syxexample", MyPromoteLevel.None, true, "count watched doors", (sender, args) =>
            {
                int n = 0;
                MyAPIGateway.Entities.GetEntities(null, e =>
                {
                    var grid = e as IMyCubeGrid;
                    if (grid == null)
                        return false;
                    foreach (var door in grid.GetFatBlocks<IMyDoor>())
                        if (_syx.BlockGetBool(door.EntityId, Mod, "watch", false))
                            n++;
                    return false;
                });
                return _greeting + "! Watched doors: " + n;
            });

            // Raw network channel: the server answers every ping with a pong.
            _syx.NetRegister(Mod + ".ping", (sender, data) =>
            {
                if (_syx.IsServer && data != null && data.Length > 0 && data[0] == 1)
                    _syx.NetSendToPlayer(Mod + ".ping", sender, new byte[] { 2 });
                else if (data != null && data.Length > 0 && data[0] == 2)
                    MyAPIGateway.Utilities.ShowMessage(Mod, "pong");
            });
        }

        /// <summary>Undocks from Syntrix.</summary>
        protected override void UnloadData()
        {
            SyntrixControls<IMyDoor>.Unload();
            if (_ui != null)
                _ui.Unload();
            _syx.Unload();
        }
    }
}
