// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using Syntrix.Account;
using Syntrix.Blocks;
using Syntrix.Effects;
using Syntrix.Grids;
using Syntrix.Gui;
using Syntrix.Price;
using Syntrix.Reputation;
using Syntrix.Screens;
using Syntrix.Time;
using Syntrix.Use;
using Syntrix.Wiki;
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
        /// <summary>Window service.</summary>
        public GuiService Gui;
        /// <summary>F action service.</summary>
        public UseService Use;

        /// <summary>In-game wiki (API 3).</summary>
        public WikiService Wiki;

        /// <summary>Block watcher for the services of API 4.</summary>
        public BlockWatch Watch;
        /// <summary>Grid storage (API 4).</summary>
        public GridStore Grids;
        /// <summary>Fixed screens (API 4).</summary>
        public ScreenService Screens;
        /// <summary>Admin only blocks (API 4).</summary>
        public AdminBlocks AdminBlocks;
        /// <summary>Price requests (API 4).</summary>
        public PriceService Prices;
        /// <summary>Block lights (API 4, 1.3.0).</summary>
        public LightService Lights;
        /// <summary>Short sounds at a block or a place (API 4, 1.3.0).</summary>
        public SoundService Sounds;
        /// <summary>Reputation of players with factions (API 5, 1.4.0).</summary>
        public ReputationService Rep;
        /// <summary>Money of players and factions (API 6, 1.5.0).</summary>
        public AccountService Accounts;
        /// <summary>World clock, local time, planet calendars (API 6, 1.5.0).</summary>
        public TimeService Time;
        /// <summary>Shared HUD values on the player's machine (API 6, 1.5.0).</summary>
        public HudService Hud;

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
            Gui = new GuiService(this);
            Use = new UseService();
            Wiki = new WikiService(this);
            Watch = new BlockWatch();
            Grids = new GridStore(this);
            Screens = new ScreenService(this, Watch);
            AdminBlocks = new AdminBlocks(this, Watch);
            Prices = new PriceService();
            Lights = new LightService(this);
            Sounds = new SoundService(this);
            Rep = new ReputationService(this);
            Accounts = new AccountService(this);
            Hud = new HudService(this);
            Time = new TimeService(this);
            Net.Start();
            Blocks.Start();
            Chat.Start();
            Settings.Start();
            Wiki.Start();
            Lights.Start();
            Rep.Start();
            Time.Start();
            Chat.Register("Syntrix", "syxrep", (int)MyPromoteLevel.Admin, true, "reputation of players (/syxrep list|set|add|reset)", Rep.OnChat);
            Started = true;

            Log.Info("Syntrix " + Protocol.VersionText + " (API " + Protocol.ApiVersion + ") started, " + (IsServer ? "server" : "client") + (IsPlayer ? " with player" : ""));
            MyAPIGateway.Utilities.SendModMessage(Protocol.ApiChannel, new MyTuple<string, int>(Protocol.TagHello, Protocol.ApiVersion));
        }

        /// <summary>Saves what the services keep with the world.</summary>
        public override void SaveData()
        {
            if (Started)
                Rep.Save();
        }

        /// <summary>Stops the services.</summary>
        protected override void UnloadData()
        {
            MyAPIGateway.Utilities.UnregisterMessageHandler(Protocol.ApiChannel, OnModMessage);
            if (Started)
            {
                MyAPIGateway.Utilities.SendModMessage(Protocol.ApiChannel, new MyTuple<string, int>(Protocol.TagBye, Protocol.ApiVersion));
                Wiki.Stop();
                Screens.Stop();
                AdminBlocks.Stop();
                Prices.Stop();
                Lights.Stop();
                Sounds.Stop();
                Rep.Stop();
                Accounts.Stop();
                Time.Stop();
                Hud.Stop();
                TimeControls.Unload();
                Watch.Stop();
                Gui.Stop();
                Use.Stop();
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

            // API 2: UI service on Rich HUD. Removed in 1.2 (moved to the module Syntrix.RichHud); the names stay and
            // report "not available" (State -1), everything else does nothing.
            AddOldUi(d, modName);

            // API 3: window service in the Vanilla style (players only; on dedicated servers State is -1)
            var gui = Gui.Client(modName);
            d["Gui.State"] = new Func<int>(() => Gui.State);
            d["Gui.Events"] = new Action<Action<int, int, int, string>>(h => gui.SetHandler(h));
            d["Gui.Release"] = new Action(() => gui.Release());
            d["Gui.Create"] = new Func<int, int, int>((kind, parent) => gui.Create(kind, parent));
            d["Gui.Remove"] = new Action<int>(h => gui.Remove(h));
            d["Gui.Clear"] = new Action<int>(h => gui.Clear(h));
            d["Gui.SetParent"] = new Action<int, int>((h, parent) => gui.SetParent(h, parent));
            d["Gui.SetBool"] = new Action<int, int, bool>(gui.SetBool);
            d["Gui.SetInt"] = new Action<int, int, int>(gui.SetInt);
            d["Gui.SetFloat"] = new Action<int, int, float>(gui.SetFloat);
            d["Gui.SetVector"] = new Action<int, int, Vector2>(gui.SetVector);
            d["Gui.SetColor"] = new Action<int, int, Color>(gui.SetColor);
            d["Gui.SetText"] = new Action<int, int, string>(gui.SetText);
            d["Gui.GetBool"] = new Func<int, int, bool>(gui.GetBool);
            d["Gui.GetInt"] = new Func<int, int, int>(gui.GetInt);
            d["Gui.GetFloat"] = new Func<int, int, float>(gui.GetFloat);
            d["Gui.GetVector"] = new Func<int, int, Vector2>(gui.GetVector);
            d["Gui.GetColor"] = new Func<int, int, Color>(gui.GetColor);
            d["Gui.GetText"] = new Func<int, int, string>(gui.GetText);
            d["Gui.ItemAdd"] = new Func<int, string, string, string, string, int>(gui.ItemAdd);
            d["Gui.ItemSet"] = new Action<int, int, int, string>(gui.ItemSet);
            d["Gui.ItemRemove"] = new Action<int, int>(gui.ItemRemove);
            d["Gui.ItemClear"] = new Action<int>(gui.ItemClear);
            d["Gui.ItemCount"] = new Func<int, int>(gui.ItemCount);
            d["Gui.ItemFind"] = new Func<int, string, int>(gui.ItemFind);
            d["Gui.Expand"] = new Action<int, int>(gui.Expand);
            d["Gui.ScrollTo"] = new Action<int, int>(gui.ScrollTo);
            d["Gui.Open"] = new Func<int, bool>(gui.Open);
            d["Gui.Close"] = new Action<int>(gui.Close);
            d["Gui.Focus"] = new Action<int>(gui.Focus);
            d["Gui.SetDraw"] = new Action<int, Action<Vector4, float>>(gui.SetDraw);
            d["Gui.Measure"] = new Func<string, string, float, Vector2>(GuiService.Measure);
            d["Gui.Screen"] = new Func<Vector2>(() => GuiService.ScreenReference);
            d["Gui.Scale"] = new Func<float>(() => { GuiDraw.UpdateScale(); return GuiDraw.K; });
            d["Gui.Cursor"] = new Func<Vector2>(() =>
            {
                Vector2 p;
                GuiDraw.UpdateScale();
                return IsPlayer && MyScreenSpace.TryGetCursorPositionPixels(out p) ? p / GuiDraw.K : new Vector2(-1f, -1f);
            });
            d["Gui.Confirm"] = new Func<string, string, string, string, Action<bool>, bool>(Gui.Confirm);
            d["Gui.WindowCount"] = new Func<int>(() => Gui.WindowCount);

            // API 3: own F action for blocks (UseObjectTypeOverride "syntrix")
            d["Use.Register"] = new Action<string, string, Action<long, int>>((block, hint, h) => Use.Register(modName, block, hint, h));
            d["Use.Unregister"] = new Action<string>(block => Use.Unregister(modName, block));

            // API 4: grid storage (server only)
            d["Grids.Nearby"] = new Func<Vector3D, double, long, long[]>(Grids.Nearby);
            d["Grids.Capture"] = new Func<long, MatrixD, string>(Grids.Capture);
            d["Grids.Spawn"] = new Func<string, MatrixD, long, int>(Grids.Spawn);
            d["Grids.Fits"] = new Func<string, MatrixD, long, bool>(Grids.Fits);

            // API 4: fixed screens (players only)
            d["Screen.Register"] = new Action<string, int, Func<long, string>>((block, surface, content) => Screens.Register(modName, block, surface, content));
            d["Screen.Refresh"] = new Action<long>(Screens.Refresh);

            // API 4: admin only blocks (all machines)
            d["Admin.RegisterBlock"] = new Action<string, string, string>((block, key, fallback) => AdminBlocks.Register(modName, block, key, fallback));
            d["Admin.UnregisterBlock"] = new Action<string>(block => AdminBlocks.Unregister(modName, block));

            // API 4: price requests
            d["Price.Register"] = new Action<string, Func<string, string, long>>((service, giver) => Prices.Register(modName, service, giver));
            d["Price.Has"] = new Func<string, bool>(Prices.Has);
            d["Price.Ask"] = new Func<string, string, long>((service, description) => Prices.Ask(modName, service, description));

            // API 4 (1.3.0): block lights and short sounds
            d["Lights.Set"] = new Action<long, string, Color, float, float>(Lights.Set);
            d["Lights.Clear"] = new Action<long, string>(Lights.Clear);
            d["Sound.Play"] = new Action<long, string, double>(Sounds.Play);
            d["Sound.PlayAt"] = new Action<Vector3D, string, double>(Sounds.PlayAt);

            // API 5 (1.4.0): reputation of players with factions
            d["Rep.DefineScale"] = new Action<int, int>((min, max) => Rep.DefineScale(modName, min, max));
            d["Rep.DefineTiers"] = new Action<List<MyTuple<string, int, float, int>>>(tiers => Rep.DefineTiers(modName, tiers));
            d["Rep.DefineFaction"] = new Action<string, int, int, int, int>((tag, start, min, max, tradeFrom) => Rep.DefineFaction(modName, tag, start, min, max, tradeFrom));
            d["Rep.DefineRecovery"] = new Action<string, int, int>((tag, perHour, upTo) => Rep.DefineRecovery(modName, tag, perHour, upTo));
            d["Rep.DefineLink"] = new Action<string, string, float>((from, to, factor) => Rep.DefineLink(modName, from, to, factor));
            d["Rep.ClearDefinitions"] = new Action(() => Rep.ClearDefinitions(modName));
            d["Rep.SetBetween"] = new Action<string, string, int>(Rep.SetBetween);
            d["Rep.Get"] = new Func<long, string, int>(Rep.Get);
            d["Rep.Info"] = new Func<long, string, MyTuple<int, int, float, int>>(Rep.Info);
            d["Rep.TierOf"] = new Func<int, int>(Rep.TierIndex);
            d["Rep.TierCount"] = new Func<int>(() => Rep.TierCount);
            d["Rep.Tier"] = new Func<int, MyTuple<string, int, int, float, int>>(Rep.TierAt);
            d["Rep.Scale"] = new Func<MyTuple<int, int>>(() => Rep.Scale);
            d["Rep.Limits"] = new Func<string, MyTuple<int, int>>(Rep.Limits);
            d["Rep.Factions"] = new Func<string[]>(Rep.Factions);
            d["Rep.Change"] = new Func<long, string, int, string, int, int>(Rep.Change);
            d["Rep.Set"] = new Action<long, string, int, string>(Rep.Set);
            d["Rep.ResetStart"] = new Action<long>(Rep.ResetStart);

            // API 6 (1.5.0): reputation gate and prices, accounts, price changes, world clock
            d["Rep.Price"] = new Func<long, string, long, bool, long>(Rep.Price);
            d["Rep.Refuses"] = new Func<long, long, bool>(Rep.RefusesById);
            d["Rep.GateType"] = new Action<string, int>(Rep.GateType);
            d["Rep.GateBlock"] = new Action<long, int>(Rep.GateBlock);
            d["Rep.MinGoods"] = new Func<long, int>(id => Rep.MinGoods(MyAPIGateway.Entities.GetEntityById(id) as IMyCubeBlock));
            d["Account.Balance"] = new Func<long, long>(Accounts.Balance);
            d["Account.Charge"] = new Func<long, long, string, bool>(Accounts.Charge);
            d["Account.Pay"] = new Func<long, long, string, bool>(Accounts.Pay);
            d["Account.FactionBalance"] = new Func<string, long>(Accounts.FactionBalance);
            d["Account.FactionChange"] = new Func<string, long, string, bool>(Accounts.FactionChange);
            d["Price.Changed"] = new Action<string>(service => Prices.Changed(this, service));
            d["Time.Utc"] = new Func<long>(() => Time.Utc.Ticks);
            d["Time.WorldDays"] = new Func<double>(() => Time.WorldDays);
            d["Time.WorldDate"] = new Func<MyTuple<int, int, double>>(Time.WorldDate);
            d["Time.WorldText"] = new Func<bool, string>(Time.WorldText);
            d["Time.Local"] = new Func<Vector3D, MyTuple<bool, double, float, bool>>(Time.Local);
            d["Time.LocalText"] = new Func<Vector3D, bool, string>(Time.LocalText);
            d["Time.Calendar"] = new Func<Vector3D, MyTuple<string, string>>(Time.CalendarAt);
            d["Time.IsNight"] = new Func<Vector3D, bool>(Time.IsNight);
            d["Hud.Set"] = new Action<string, object>(Hud.Set);
            d["Hud.Get"] = new Func<string, object>(Hud.Get);
            d["Hud.Keys"] = new Func<string[]>(Hud.Keys);

            // Wiki (API 3)
            d["Wiki.Open"] = new Func<string, bool>(Wiki.Open);
            d["Wiki.Close"] = new Action(Wiki.Close);
            d["Wiki.IsOpen"] = new Func<bool>(() => Wiki.IsOpen);
            d["Wiki.OnClosed"] = new Action<Action>(h => Wiki.OnClosed(modName, h));
            d["Wiki.AddSource"] = new Action<Func<string, string>>(f => Wiki.AddSource(modName, f));
            d["Wiki.AddTables"] = new Action<Func<string, string, string, string, string, string>>(f => Wiki.AddTables(modName, f));
            d["Wiki.Context"] = new Func<string>(Wiki.Context);
            d["Wiki.Reload"] = new Action(Wiki.Reload);
            d["Wiki.Planet"] = new Func<string, bool, string[]>(Wiki.Planet);
            d["Wiki.Title"] = new Func<string, bool, string>(Wiki.Title);
            d["Wiki.Text"] = new Func<string, string, bool, string>(WikiTexts.Get);

            return d;
        }

        private bool _oldUiHinted;

        /// <summary>The API 2 names of the Rich HUD UI service: kept, State -1, the rest does nothing.</summary>
        private void AddOldUi(Dictionary<string, Delegate> d, string modName)
        {
            d["Ui.State"] = new Func<int>(() =>
            {
                if (IsPlayer && !_oldUiHinted)
                {
                    _oldUiHinted = true;
                    Log.Warn(modName + " asks for the Rich HUD UI service (Ui.*, API 2). Since Syntrix 1.2 it is not part of Syntrix: "
                        + "use the window service (Gui.*, API 3) or build in the module Syntrix.RichHud. The Ui.* calls do nothing.");
                }
                return -1;
            });
            d["Ui.Events"] = new Action<Action<int, int, string>>(h => { });
            d["Ui.Release"] = new Action(() => { });
            d["Ui.Create"] = new Func<int, int, int>((kind, parent) => 0);
            d["Ui.Remove"] = new Action<int>(h => { });
            d["Ui.Clear"] = new Action<int>(h => { });
            d["Ui.SetParent"] = new Action<int, int>((h, parent) => { });
            d["Ui.Part"] = new Func<int, int, int>((h, part) => 0);
            d["Ui.Listen"] = new Action<int, int, bool>((h, ev, on) => { });
            d["Ui.SetBool"] = new Action<int, int, bool>((h, p, v) => { });
            d["Ui.SetInt"] = new Action<int, int, int>((h, p, v) => { });
            d["Ui.SetFloat"] = new Action<int, int, float>((h, p, v) => { });
            d["Ui.SetVector"] = new Action<int, int, Vector2>((h, p, v) => { });
            d["Ui.SetColor"] = new Action<int, int, Color>((h, p, v) => { });
            d["Ui.SetText"] = new Action<int, int, string>((h, p, v) => { });
            d["Ui.GetBool"] = new Func<int, int, bool>((h, p) => false);
            d["Ui.GetInt"] = new Func<int, int, int>((h, p) => 0);
            d["Ui.GetFloat"] = new Func<int, int, float>((h, p) => 0f);
            d["Ui.GetVector"] = new Func<int, int, Vector2>((h, p) => Vector2.Zero);
            d["Ui.GetColor"] = new Func<int, int, Color>((h, p) => Color.Transparent);
            d["Ui.GetText"] = new Func<int, int, string>((h, p) => null);
            d["Ui.Focus"] = new Action<int>(h => { });
            d["Ui.Measure"] = new Func<int, string, float, Vector2>((h, text, wrap) => Vector2.Zero);
            d["Ui.Screen"] = new Func<Vector2>(() => Vector2.Zero);
            d["Ui.ResScale"] = new Func<float>(() => 1f);
            d["Ui.CursorPos"] = new Func<Vector2>(() => Vector2.Zero);
            d["Ui.Cursor"] = new Action<bool>(on => { });
            d["Ui.Bind"] = new Action<string, string>((name, keys) => { });
            d["Ui.BindKeys"] = new Func<string, string>(name => null);
            d["Ui.BindPage"] = new Action<string>(title => { });
            d["Ui.ItemInsert"] = new Func<int, int, string, string, int>((h, i, text, key) => -1);
            d["Ui.ItemRemove"] = new Action<int, int>((h, i) => { });
            d["Ui.ItemText"] = new Action<int, int, string>((h, i, text) => { });
            d["Ui.ItemEnabled"] = new Action<int, int, bool>((h, i, on) => { });
            d["Ui.ItemKey"] = new Func<int, int, string>((h, i) => null);
            d["Ui.ItemFind"] = new Func<int, string, int>((h, key) => -1);
            d["Ui.SetVector3"] = new Action<int, int, Vector3D>((h, p, v) => { });
            d["Ui.GetVector3"] = new Func<int, int, Vector3D>((h, p) => Vector3D.Zero);
            d["Ui.SetMatrix"] = new Action<int, MatrixD>((h, m) => { });
            d["Ui.SetPoints"] = new Action<int, Vector2[], int[]>((h, pts, idx) => { });
            d["Ui.Clipboard"] = new Func<string>(() => null);
            d["Ui.SetClipboard"] = new Action<string>(text => { });
            d["Ui.Terminal"] = new Action<int>(h => { });
            d["Ui.TerminalOpen"] = new Func<bool>(() => false);
            d["Ui.ScreenInfo"] = new Func<int, float>(what => 0f);
            d["Ui.CursorWorld"] = new Func<Vector3D>(() => Vector3D.Zero);
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
