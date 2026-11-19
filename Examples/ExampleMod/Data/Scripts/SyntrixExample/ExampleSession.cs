// Smallest mod that docks to Syntrix. Copy Api/SyntrixApi.cs next to this file, and for the window
// Api/SyntrixGui.cs as well. The example block (Data/CubeBlocks_SyntrixExample.sbc and
// Data/EntityComponents_SyntrixExample.sbc) shows the own F action.
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
        private const string ExampleBlock = "FunctionalBlock/SyntrixExample_Terminal";

        private readonly SyntrixApi _syx = new SyntrixApi();
        private readonly SyntrixLog _log = new SyntrixLog(Mod);
        private string _greeting = "Hello";

        // Window through the Syntrix window service (Vanilla look, players only)
        private SyntrixGui _gui;
        private GuiWindow _window;
        private GuiLabel _status;
        private GuiTextField _name;
        private GuiDropdown _greetings;
        private GuiProgressBar _bar;
        private long _block;
        private int _clicks;

        /// <summary>Docks to Syntrix and registers controls, settings and events.</summary>
        public override void LoadData()
        {
            _syx.Load(Mod, OnReady);

            // The window service: elements can be made once it is available (again after Syntrix reloaded).
            _gui = new SyntrixGui(_syx);
            _gui.AvailabilityChanged += () => { _window = null; };

            // Own F action: F on the example block opens the window, K still opens the Vanilla terminal.
            // Players only. The hint is a text key (MyTexts, "{0}" = the key); null shows "[F] Use".
            _syx.UseRegister(ExampleBlock, null, (entityId, action) => OpenWindow(entityId));

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

            // Wiki: fills an automatic table <Table Auto="SyntrixExample.Doors" /> in a Data/Syntrix-Wiki.xml (any mod).
            _syx.WikiAddTables((kind, auto, system, planet, page) =>
            {
                if (auto != "SyntrixExample.Doors" || kind != "Table")
                    return null;
                return "<SyxWiki><Page Id=\"_\"><Table Widths=\"2|1\"><Header de=\"Tür|Bewacht\" en=\"Door|Watched\" />"
                    + "<Row de=\"Haupttor|ja\" en=\"Main gate|yes\" /></Table></Page></SyxWiki>";
            });
        }

        private void OpenWindow(long block)
        {
            _block = block;
            if (!_gui.IsAvailable)
                return;
            if (_window == null || !_window.IsValid)
                BuildWindow();
            var b = MyAPIGateway.Entities.GetEntityById(block) as IMyTerminalBlock;
            _window.Title = b != null ? b.CustomName : "Syntrix example";
            _window.Open();
        }

        private void BuildWindow()
        {
            // Vanilla terminal size by default; the content lies below the title band
            _window = _gui.Window("Syntrix example");
            var root = _window.AddStack(false, 10f);
            root.FillWidth = true;
            root.FillHeight = true;

            // Formatted text with links; it wraps at the available width
            var text = root.AddLabel("Text can be [b]bright[/b], [i]quiet[/i], [color=#E8B931]coloured[/color] or [size=1.3]bigger[/size]."
                + "\n[link=hello]This link[/link] writes into the status line, [link=wiki]this one[/link] opens the wiki.");
            text.Link += target =>
            {
                if (target == "wiki")
                {
                    _window.Close();
                    _syx.WikiOpen();
                }
                else
                    _status.Text = "Link clicked: [b]" + SyntrixGui.Escape(target) + "[/b]";
            };

            var row = root.AddStack(true, 16f);
            row.FillWidth = true;

            // List with icons (a definition id draws its icon): click copies the name
            var list = row.AddList();
            list.Size = new Vector2(300f, 260f);
            foreach (var name in new[] { "Alpha", "Bravo", "Charlie", "Delta", "Echo", "Foxtrot", "Golf", "Hotel" })
                list.ItemAdd(name, name, "MyObjectBuilder_Component/SteelPlate");
            list.Selected += (index, key) => _name.Text = key;

            var side = row.AddStack(false, 10f);
            side.FillWidth = true;

            // Text field: Enter greets
            _name = side.AddTextField();
            _name.Placeholder = "Type a name ...";
            _name.MaxLength = 24;
            _name.Submitted += t => _status.Text = Greeting() + ", [b]" + SyntrixGui.Escape(t) + "[/b]!";

            // Drop-down: items have a key (given back on selection) and a text
            _greetings = side.AddDropdown();
            _greetings.ItemAdd("settings", "From the settings");
            _greetings.ItemAdd("Hi", "Hi");
            _greetings.ItemAdd("Good evening", "Good evening");
            _greetings.Selection = 0;
            _greetings.Selected += (index, key) => _status.Text = "Greeting: [b]" + SyntrixGui.Escape(Greeting()) + "[/b]";

            // Tree: items with a parent key below their parent
            var tree = side.AddTree();
            tree.Size = new Vector2(0f, 120f);
            tree.FillWidth = true;
            tree.ItemAdd("fleet", "Fleet");
            tree.ItemAdd("miner", "Miner", null, "fleet");
            tree.ItemAdd("hauler", "Hauler", null, "fleet");
            tree.Selected += (index, key) => _status.Text = key == "miner" ? "The miner is busy." : key == "hauler" ? "The hauler waits." : "Two ships.";

            // Table with header, relative widths and markup in the cells
            var table = root.AddTable();
            table.FillWidth = true;
            table.SetHeader("Ship", "Cargo", "State");
            table.SetWidths(2f, 1f, 1f);
            table.AddRow("miner", "Miner", "[b]12 t[/b]", "[color=#78D26E]ready[/color]");
            table.AddRow("hauler", "Hauler", "40 t", "[color=#E6504A]damaged[/color]");

            _bar = root.AddProgressBar();
            _bar.Size = new Vector2(0f, 14f);
            _bar.Value = 0.4f;

            var buttons = root.AddStack(true, 10f);
            buttons.AddButton("Count", () =>
            {
                _status.Text = "Clicked [b]" + (++_clicks) + "[/b] times";
                _bar.Value = (_clicks % 10) / 10f;
            });
            buttons.AddButton("Ask", () => _gui.Confirm("Syntrix example", "Reset the counter?", "Yes", "No", yes =>
            {
                if (yes)
                    _clicks = 0;
                _status.Text = yes ? "Counter reset." : "Counter kept.";
            }));

            _status = root.AddLabel("Esc closes the window, K on the block opens its terminal.");
            _status.TextColor = new Color(150, 165, 175);
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
            _syx.UseUnregister(ExampleBlock);
            if (_gui != null)
                _gui.Release();
            _syx.Unload();
        }
    }
}
