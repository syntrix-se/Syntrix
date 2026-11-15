// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using Syntrix.Ui;
using VRage.Utils;
using VRageMath;

namespace Syntrix
{
    /// <summary>
    /// Entry point. Negotiates between several installed copies (highest version wins),
    /// starts the services and hands every docking mod its own endpoint dictionary.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate, int.MinValue + 100)]
    public class SyntrixCore : MySessionComponentBase
    {
        /// <summary>The running session component.</summary>
        public static SyntrixCore Instance;

        /// <summary>This copy is the active Syntrix (highest version).</summary>
        public bool Active { get; private set; }
        /// <summary>The services are running.</summary>
        public bool Started { get; private set; }
        /// <summary>Runs on the server (or single player).</summary>
        public bool IsServer { get; private set; }
        /// <summary>Has a local player (not a dedicated server).</summary>
        public bool IsPlayer { get; private set; }

        /// <summary>Network service.</summary>
        public Network Net;
        /// <summary>Block settings service.</summary>
        public BlockStore Blocks;
        /// <summary>Chat command service.</summary>
        public ChatCommands Chat;
        /// <summary>Event service.</summary>
        public EventBus Events;
        /// <summary>Settings service.</summary>
        public SettingsService Settings;
        /// <summary>UI service.</summary>
        public UiService Ui;

        private readonly Dictionary<string, int> _docked = new Dictionary<string, int>();
        private string _instanceKey;

        /// <summary>Docked mods with their API version.</summary>
        public IEnumerable<KeyValuePair<string, int>> DockedMods { get { return _docked; } }

        /// <summary>Announces this copy to other installed copies.</summary>
        public override void LoadData()
        {
            Instance = this;
            Active = true;
            IsServer = MyAPIGateway.Session.IsServer;
            IsPlayer = !MyAPIGateway.Utilities.IsDedicated;
            _instanceKey = (ModContext != null ? ModContext.ModId : "") ?? "";

            MyAPIGateway.Utilities.RegisterMessageHandler(Protocol.ApiChannel, OnModMessage);
            // Announce ourselves; an older copy that hears this steps down, a newer one answers.
            MyAPIGateway.Utilities.SendModMessage(Protocol.ApiChannel,
                new MyTuple<string, int, string, Action<int, string>>(Protocol.TagInstance, Protocol.ApiVersion, _instanceKey, OnInstanceReply));
        }

        /// <summary>Starts the services when this copy is the active one.</summary>
        public override void BeforeStart()
        {
            if (!Active)
                return;

            Events = new EventBus(this);
            Net = new Network(this);
            Blocks = new BlockStore(this);
            Chat = new ChatCommands(this);
            Settings = new SettingsService(this);
            Ui = new UiService(this);
            Net.Start();
            Blocks.Start();
            Chat.Start();
            Settings.Start();
            Started = true;

            Log.Info("Syntrix " + Protocol.VersionText + " (API " + Protocol.ApiVersion + ") started, " + (IsServer ? "server" : "client") + (IsPlayer ? " with player" : ""));
            MyAPIGateway.Utilities.SendModMessage(Protocol.ApiChannel, new MyTuple<string, int>(Protocol.TagHello, Protocol.ApiVersion));
        }

        /// <summary>Stops the services.</summary>
        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(Protocol.ApiChannel, OnModMessage);
            if (Started)
            {
                MyAPIGateway.Utilities.SendModMessage(Protocol.ApiChannel, new MyTuple<string, int>(Protocol.TagBye, Protocol.ApiVersion));
                Ui.Stop();
                Settings.Stop();
                Events.Stop();
                Chat.Stop();
                Blocks.Stop();
                Net.Stop();
            }
            Started = false;
            Active = false;
            if (Instance == this)
                Instance = null;
        }

        // ---------- copies of the framework ----------

        private bool OtherWins(int version, string key)
        {
            if (version != Protocol.ApiVersion)
                return version > Protocol.ApiVersion;
            return string.CompareOrdinal(key, _instanceKey) < 0;
        }

        private void OnInstanceReply(int version, string key)
        {
            if (key == _instanceKey || !Active)
                return;
            if (OtherWins(version, key))
                StepDown(key);
        }

        private void StepDown(string winner)
        {
            if (!Active)
                return;
            Active = false;
            Log.Info("Another Syntrix copy (" + winner + ") is newer or preferred, this copy (" + _instanceKey + ") stays inactive.");
        }

        // ---------- docking ----------

        private void OnModMessage(object msg)
        {
            try
            {
                if (msg is MyTuple<string, int, string, Action<int, string>>)
                {
                    var m = (MyTuple<string, int, string, Action<int, string>>)msg;
                    if (m.Item1 != Protocol.TagInstance || m.Item3 == _instanceKey)
                        return;
                    if (m.Item4 != null)
                        m.Item4(Protocol.ApiVersion, _instanceKey);
                    if (OtherWins(m.Item2, m.Item3))
                        StepDown(m.Item3);
                    return;
                }

                if (msg is MyTuple<string, int, string, Action<object>>)
                {
                    var r = (MyTuple<string, int, string, Action<object>>)msg;
                    if (r.Item1 == Protocol.TagRegister && r.Item4 != null && Active && Started)
                        Register(r.Item3, r.Item2, r.Item4);
                }
            }
            catch (Exception e)
            {
                Log.Error("mod message", e);
            }
        }

        private void Register(string modName, int apiVersion, Action<object> reply)
        {
            if (string.IsNullOrWhiteSpace(modName))
            {
                reply("Syntrix: registration without mod name rejected.");
                return;
            }
            if (apiVersion < Protocol.MinApiVersion)
            {
                string text = "Syntrix " + Protocol.VersionText + ": " + modName + " uses dock file version " + apiVersion
                    + ", oldest supported is " + Protocol.MinApiVersion + ". Update the dock file of that mod.";
                Log.Warn(text);
                reply(text);
                return;
            }

            bool again = _docked.ContainsKey(modName);
            _docked[modName] = apiVersion;
            if (!again)
                Log.Info("Docked: " + modName + " (dock file version " + apiVersion + ")");
            reply(new MyTuple<int, string, IReadOnlyDictionary<string, Delegate>>(Protocol.ApiVersion, Protocol.VersionText, Endpoints(modName)));
        }

        /// <summary>Endpoint dictionary for one mod. Keys are never removed or changed in meaning.</summary>
        private IReadOnlyDictionary<string, Delegate> Endpoints(string modName)
        {
            var d = new Dictionary<string, Delegate>();

            d["Info.IsServer"] = new Func<bool>(() => IsServer);
            d["Info.IsPlayer"] = new Func<bool>(() => IsPlayer);
            d["Info.PromoteLevel"] = new Func<ulong, int>(steamId => (int)PromoteLevel(steamId));

            d["Net.Register"] = new Action<string, Action<ulong, byte[]>>((ch, h) => Net.RegisterChannel(modName, ch, h));
            d["Net.Unregister"] = new Action<string>(ch => Net.UnregisterChannel(modName, ch));
            d["Net.SendToServer"] = new Action<string, byte[]>((ch, data) => Net.SendModData(ch, data, 0, Network.Target.Server));
            d["Net.SendToPlayer"] = new Action<string, ulong, byte[]>((ch, id, data) => Net.SendModData(ch, data, id, Network.Target.Player));
            d["Net.SendToAll"] = new Action<string, byte[]>((ch, data) => Net.SendModData(ch, data, 0, Network.Target.All));

            d["Block.Get"] = new Func<long, string, string, string>((id, mod, key) => Blocks.Get(id, mod, key));
            d["Block.Set"] = new Action<long, string, string, string>((id, mod, key, value) => Blocks.Set(id, mod, key, value));
            d["Block.OnChanged"] = new Action<string, Action<long, string, string>>((mod, h) => Blocks.Subscribe(mod, h));
            d["Block.RemoveOnChanged"] = new Action<string, Action<long, string, string>>((mod, h) => Blocks.Unsubscribe(mod, h));
            d["Block.RequestSync"] = new Action<long>(id => Blocks.RequestSync(id));

            d["Chat.Register"] = new Action<string, int, bool, string, Func<ulong, string[], string>>((cmd, level, onServer, help, h) => Chat.Register(modName, cmd, level, onServer, help, h));

            d["Events.Subscribe"] = new Action<string, Action<object>>((name, h) => Events.Subscribe(name, h));
            d["Events.Unsubscribe"] = new Action<string, Action<object>>((name, h) => Events.Unsubscribe(name, h));
            d["Events.Publish"] = new Action<string, object>((name, payload) => Events.Publish(name, payload));

            // API 2
            d["Chat.Unregister"] = new Action<string>(cmd => Chat.Unregister(modName, cmd));
            d["Events.SendToServer"] = new Action<string, string[]>((name, args) => Events.SendToServer(name, args));
            d["Events.SendToAll"] = new Action<string, string[]>((name, args) => Events.SendToAll(name, args));
            d["Events.SendToPlayer"] = new Action<string, ulong, string[]>((name, id, args) => Events.SendToPlayer(name, id, args));
            d["Events.Sender"] = new Func<ulong>(() => Events.Sender);
            d["Settings.Publish"] = new Action<string, string>((file, text) => Settings.Publish(modName, file, text));
            d["Settings.Subscribe"] = new Action<string, Action<string>>((file, h) => Settings.Subscribe(modName, file, h));

            d["Notify.Player"] = new Action<ulong, string, string, string[], int, string>((id, key, fallback, args, ms, font) => Net.SendNotify(id, key, fallback, args, ms, font));

            // API 2: UI service (players with Rich HUD Master only; elsewhere State is -1 and the rest does nothing)
            var ui = Ui.Client(modName);
            d["Ui.State"] = new Func<int>(() => Ui.State);
            d["Ui.Events"] = new Action<Action<int, int, string>>(h => Ui.SetDispatcher(ui, h));
            d["Ui.Release"] = new Action(() => ui.Release());
            d["Ui.Create"] = new Func<int, int, int>((kind, parent) => ui.Create(kind, parent));
            d["Ui.Remove"] = new Action<int>(h => ui.Remove(h));
            d["Ui.Clear"] = new Action<int>(h => ui.Clear(h));
            d["Ui.SetParent"] = new Action<int, int>((h, parent) => ui.SetParent(h, parent));
            d["Ui.Part"] = new Func<int, int, int>((h, part) => ui.Part(h, part));
            d["Ui.Listen"] = new Action<int, int, bool>((h, ev, on) => ui.Listen(h, ev, on));
            d["Ui.SetBool"] = new Action<int, int, bool>(ui.SetBool);
            d["Ui.SetInt"] = new Action<int, int, int>(ui.SetInt);
            d["Ui.SetFloat"] = new Action<int, int, float>(ui.SetFloat);
            d["Ui.SetVector"] = new Action<int, int, Vector2>(ui.SetVector);
            d["Ui.SetColor"] = new Action<int, int, Color>(ui.SetColor);
            d["Ui.SetText"] = new Action<int, int, string>(ui.SetText);
            d["Ui.GetBool"] = new Func<int, int, bool>(ui.GetBool);
            d["Ui.GetInt"] = new Func<int, int, int>(ui.GetInt);
            d["Ui.GetFloat"] = new Func<int, int, float>(ui.GetFloat);
            d["Ui.GetVector"] = new Func<int, int, Vector2>(ui.GetVector);
            d["Ui.GetColor"] = new Func<int, int, Color>(ui.GetColor);
            d["Ui.GetText"] = new Func<int, int, string>(ui.GetText);
            d["Ui.Focus"] = new Action<int>(h => ui.Focus(h));
            d["Ui.Measure"] = new Func<int, string, float, Vector2>((h, text, wrap) => ui.Measure(h, text, wrap));
            d["Ui.Screen"] = new Func<Vector2>(() => Ui.ScreenSize);
            d["Ui.ResScale"] = new Func<float>(() => Ui.ResScale);
            d["Ui.CursorPos"] = new Func<Vector2>(() => Ui.CursorPosition);
            d["Ui.Cursor"] = new Action<bool>(on => ui.WantsCursor = on);
            d["Ui.Bind"] = new Action<string, string>((name, keys) => ui.Bind(name, keys));
            d["Ui.BindKeys"] = new Func<string, string>(name => ui.BindKeys(name));
            d["Ui.BindPage"] = new Action<string>(title => ui.BindPage(title));
            d["Ui.ItemInsert"] = new Func<int, int, string, string, int>(ui.ItemInsert);
            d["Ui.ItemRemove"] = new Action<int, int>(ui.ItemRemove);
            d["Ui.ItemText"] = new Action<int, int, string>(ui.ItemText);
            d["Ui.ItemEnabled"] = new Action<int, int, bool>(ui.ItemEnabled);
            d["Ui.ItemKey"] = new Func<int, int, string>(ui.ItemKey);
            d["Ui.ItemFind"] = new Func<int, string, int>(ui.ItemFind);
            d["Ui.SetVector3"] = new Action<int, int, Vector3D>(ui.SetVector3);
            d["Ui.GetVector3"] = new Func<int, int, Vector3D>(ui.GetVector3);
            d["Ui.SetMatrix"] = new Action<int, MatrixD>(ui.SetMatrix);
            d["Ui.SetPoints"] = new Action<int, Vector2[], int[]>(ui.SetPoints);
            d["Ui.Clipboard"] = new Func<string>(() => Ui.Clipboard);
            d["Ui.SetClipboard"] = new Action<string>(text => Ui.Clipboard = text);
            d["Ui.Terminal"] = new Action<int>(h =>
            {
                if (h > 0) ui.OpenTerminal(h);
                else Ui.Terminal(h);
            });
            d["Ui.TerminalOpen"] = new Func<bool>(() => Ui.TerminalIsOpen);
            d["Ui.ScreenInfo"] = new Func<int, float>(what => Ui.ScreenInfo(what));
            d["Ui.CursorWorld"] = new Func<Vector3D>(() => Ui.CursorWorld);

            return d;
        }

        /// <summary>Promote level of a player.</summary>
        public MyPromoteLevel PromoteLevel(ulong steamId)
        {
            if (!MyAPIGateway.Multiplayer.MultiplayerActive || steamId == MyAPIGateway.Multiplayer.MyId)
                return IsPlayer ? MyAPIGateway.Session.PromoteLevel : MyPromoteLevel.Owner;
            IMyPlayer p = FindPlayer(steamId);
            return p != null ? p.PromoteLevel : MyPromoteLevel.None;
        }

        private readonly List<IMyPlayer> _players = new List<IMyPlayer>();

        /// <summary>Player with this Steam id, null if not online.</summary>
        public IMyPlayer FindPlayer(ulong steamId)
        {
            _players.Clear();
            MyAPIGateway.Players.GetPlayers(_players, p => p.SteamUserId == steamId);
            return _players.Count > 0 ? _players[0] : null;
        }
    }
}
