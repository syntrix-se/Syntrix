// Smallest mod that docks to Syntrix. Copy Api/SyntrixApi.cs next to this file.
// MIT License, see LICENSE in the Syntrix repository.
using Sandbox.ModAPI;
using Syntrix.Api;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace SyntrixExample
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class ExampleSession : MySessionComponentBase
    {
        const string Mod = "SyntrixExample";
        const string SettingsFile = "SyntrixExample.ini";
        const string EventWatched = "SyntrixExample.DoorWatched";

        readonly SyntrixApi _syx = new SyntrixApi();
        readonly SyntrixLog _log = new SyntrixLog(Mod);
        string _greeting = "Hello";

        public override void LoadData()
        {
            _syx.Load(Mod, OnReady);

            // Terminal controls are only described here; Syntrix creates them when a door is first seen.
            SyntrixControls<IMyDoor>.Checkbox(_syx, "SyntrixExample_Watch", "SyntrixExample_Watch", "SyntrixExample_Watch_Tooltip",
                Mod, "watch", false);
            SyntrixControls<IMyDoor>.TextBox(_syx, "SyntrixExample_Note", "SyntrixExample_Note", null, Mod, "note");

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

        void ParseSettings(string text)
        {
            foreach (var line in text.Split('\n'))
            {
                var parts = line.Split(new[] { '=' }, 2);
                if (parts.Length == 2 && parts[0].Trim() == "greeting")
                    _greeting = parts[1].Trim();
            }
        }

        void OnReady()
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

        protected override void UnloadData()
        {
            SyntrixControls<IMyDoor>.Unload();
            _syx.Unload();
        }
    }
}
