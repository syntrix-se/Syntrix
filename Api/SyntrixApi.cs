// Syntrix dock file (API version 6, Syntrix 1.5.0). Copy this file into your mod's script folder.
// Do not edit it; take a newer copy from the Syntrix repository instead.
// Windows and screen elements: also copy SyntrixGui.cs (optional dock file of the window service).
//
// This file deliberately holds several types (SyntrixApi, SyntrixFiles, SyntrixText, SyntrixWikiXml,
// SyntrixLog, SyntrixSliderScale, SyntrixControls) so that a mod copies exactly one file.
//
// MIT License - Copyright (c) 2026 Syntrix contributors
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files, to deal in the Software without restriction,
// subject to including this notice. Full text: LICENSE in the Syntrix repository.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace Syntrix.Api
{
    /// <summary>
    /// Connection to the Syntrix framework. Create one per mod, call <see cref="Load"/> in LoadData
    /// (or BeforeStart) and <see cref="Unload"/> in UnloadData.
    /// Registrations (network channels, block change handlers, chat commands, event subscriptions,
    /// settings) may be made right after Load: the dock file keeps them and hands them over
    /// as soon as the framework is ready. Sending only works after onReady fired.
    /// Every method is safe to call without the framework: it then does nothing.
    /// </summary>
    public class SyntrixApi
    {
        /// <summary>Mod message channel used for docking. Fixed.</summary>
        public const long Channel = 8346120579001L;

        /// <summary>API version of this dock file.</summary>
        public const int ApiVersion = 6;

        /// <summary>Prefix for notification arguments that are text keys themselves.</summary>
        public const char TextArgPrefix = '\u0001';

        /// <summary>True while docked to a running framework.</summary>
        public bool IsReady { get; private set; }

        /// <summary>API version of the running framework (0 before docking).</summary>
        public int FrameworkApiVersion { get; private set; }

        /// <summary>Version text of the running framework, e.g. "1.1.0" (null before docking).</summary>
        public string FrameworkVersion { get; private set; }

        /// <summary>Mod name given to <see cref="Load"/>; used as prefix in log lines.</summary>
        public string ModName { get; private set; }

        /// <summary>Set when the framework refused this dock file (too old).</summary>
        public string Error { get; private set; }

        private IReadOnlyDictionary<string, Delegate> _endpoints;
        private Action _onReady;
        private bool _loaded, _warned;
        private readonly List<Action> _pending = new List<Action>();

        /// <summary>
        /// Starts docking. <paramref name="onReady"/> runs once when the framework answered
        /// (after the queued registrations were handed over).
        /// </summary>
        /// <param name="modName">Unique name of your mod; prefix for channels, events and log lines.</param>
        /// <param name="onReady">Runs once when docked; may be null.</param>
        public void Load(string modName, Action onReady = null)
        {
            if (_loaded)
                return;
            _loaded = true;
            ModName = modName;
            _onReady = onReady;
            MyAPIGateway.Utilities.RegisterMessageHandler(Channel, OnMessage);
            SendRegistration();
            MyAPIGateway.Session.OnSessionReady += OnSessionReady;
        }

        /// <summary>Undocks and forgets all queued registrations and settings texts.</summary>
        public void Unload()
        {
            if (!_loaded)
                return;
            _loaded = false;
            MyAPIGateway.Utilities.UnregisterMessageHandler(Channel, OnMessage);
            if (MyAPIGateway.Session != null)
                MyAPIGateway.Session.OnSessionReady -= OnSessionReady;
            IsReady = false;
            _endpoints = null;
            _onReady = null;
            _pending.Clear();
            _settings.Clear();
        }

        /// <summary>True if the running framework offers this endpoint.</summary>
        /// <param name="key">Endpoint name, e.g. "Gui.State".</param>
        public bool Has(string key) { return _endpoints != null && _endpoints.ContainsKey(key); }

        private void SendRegistration()
        {
            MyAPIGateway.Utilities.SendModMessage(Channel,
                new MyTuple<string, int, string, Action<object>>("syx.register", ApiVersion, ModName, OnReply));
        }

        private void OnMessage(object msg)
        {
            if (!(msg is MyTuple<string, int>))
                return;
            var m = (MyTuple<string, int>)msg;
            if (m.Item1 == "syx.hello")
                SendRegistration();
            else if (m.Item1 == "syx.bye")
            {
                IsReady = false;
                _endpoints = null;
                Changed();
            }
        }

        private void OnReply(object msg)
        {
            if (msg is string)
            {
                Error = (string)msg;
                MyLog.Default.WriteLineAndConsole(ModName + ": " + Error);
                return;
            }
            if (!(msg is MyTuple<int, string, IReadOnlyDictionary<string, Delegate>>))
                return;
            var r = (MyTuple<int, string, IReadOnlyDictionary<string, Delegate>>)msg;
            bool first = !IsReady;
            _endpoints = r.Item3;
            FrameworkApiVersion = r.Item1;
            FrameworkVersion = r.Item2;
            Bind();
            IsReady = true;
            Changed();
            if (!first)
                return;
            var pending = _pending.ToArray();
            _pending.Clear();
            foreach (var p in pending)
            {
                try { p(); }
                catch (Exception e) { MyLog.Default.WriteLineAndConsole(ModName + ": ERROR in Syntrix registration: " + e); }
            }
            if (_onReady != null)
            {
                try { _onReady(); }
                catch (Exception e) { MyLog.Default.WriteLineAndConsole(ModName + ": ERROR in Syntrix onReady: " + e); }
            }
        }

        /// <summary>Runs now when docked, otherwise as soon as the framework is ready.</summary>
        private void WhenReady(Action a)
        {
            if (IsReady)
                a();
            else if (_loaded)
                _pending.Add(a);
        }

        private void OnSessionReady()
        {
            if (IsReady || _warned)
                return;
            _warned = true;
            string text = Error ?? (ModName + " needs the Syntrix framework mod, which is not loaded. Its scripted features are off.");
            MyLog.Default.WriteLineAndConsole(ModName + ": " + text);
            if (!MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Admin)
                MyAPIGateway.Utilities.ShowMessage(ModName, text);
        }

        private T Get<T>(string key) where T : class
        {
            Delegate d;
            return _endpoints != null && _endpoints.TryGetValue(key, out d) ? d as T : null;
        }

        /// <summary>API 2. For the other dock files (SyntrixGui): runs when the endpoints were (re)bound or dropped.</summary>
        internal event Action EndpointsChanged;

        /// <summary>API 2. Endpoint by name for the other dock files, null if missing.</summary>
        internal T Endpoint<T>(string key) where T : class { return Get<T>(key); }

        private void Changed()
        {
            if (EndpointsChanged == null)
                return;
            try { EndpointsChanged(); }
            catch (Exception e) { MyLog.Default.WriteLineAndConsole(ModName + ": ERROR in Syntrix dock file: " + e); }
        }

        // ---------- endpoints ----------

        private Func<bool> _isServer, _isPlayer;
        private Func<ulong, int> _promoteLevel;
        private Action<string, Action<ulong, byte[]>> _netRegister;
        private Action<string> _netUnregister;
        private Action<string, byte[]> _netToServer, _netToAll;
        private Action<string, ulong, byte[]> _netToPlayer;
        private Func<long, string, string, string> _blockGet;
        private Action<long, string, string, string> _blockSet;
        private Action<string, Action<long, string, string>> _blockOnChanged, _blockRemoveOnChanged;
        private Action<long> _blockSync;
        private Action<string, int, bool, string, Func<ulong, string[], string>> _chatRegister;
        private Action<string, Action<object>> _evSub, _evUnsub;
        private Action<string, object> _evPub;
        private Action<ulong, string, string, string[], int, string> _notify;
        // API 2
        private Action<string> _chatUnregister;
        private Action<string, string[]> _evToServer, _evToAll;
        private Action<string, ulong, string[]> _evToPlayer;
        private Func<ulong> _evSender;
        private Action<string, string> _setPublish;
        private Action<string, Action<string>> _setSubscribe;
        // API 3
        private Action<string, string, Action<long, int>> _useRegister;
        private Action<string> _useUnregister;
        private Func<string, bool> _wikiOpen;
        private Action _wikiClose, _wikiReload;
        private Func<bool> _wikiIsOpen;
        private Action<Action> _wikiOnClosed;
        private Action<Func<string, string>> _wikiAddSource;
        private Action<Func<string, string, string, string, string, string>> _wikiAddTables;
        private Func<string> _wikiContext;
        private Func<string, bool, string[]> _wikiPlanet;
        private Func<string, bool, string> _wikiTitle;
        private Func<string, string, bool, string> _wikiText;
        // API 4
        private Func<VRageMath.Vector3D, double, long, long[]> _gridsNearby;
        private Func<long, VRageMath.MatrixD, string> _gridsCapture;
        private Func<string, VRageMath.MatrixD, long, int> _gridsSpawn;
        private Func<string, VRageMath.MatrixD, long, bool> _gridsFits;
        private Action<string, int, Func<long, string>> _screenRegister;
        private Action<long> _screenRefresh;
        private Action<string, string, string> _adminRegister;
        private Action<string> _adminUnregister;
        private Action<string, Func<string, string, long>> _priceRegister;
        private Func<string, bool> _priceHas;
        private Func<string, string, long> _priceAsk;
        private Action<long, string, VRageMath.Color, float, float> _lightsSet;
        private Action<long, string> _lightsClear;
        private Action<long, string, double> _soundPlay;
        private Action<VRageMath.Vector3D, string, double> _soundPlayAt;
        // API 5
        private Action<int, int> _repScale;
        private Action<List<MyTuple<string, int, float, int>>> _repTiers;
        private Action<string, int, int, int, int> _repFaction;
        private Action<string, int, int> _repRecovery;
        private Action<string, string, float> _repLink;
        private Action _repClear;
        private Action<string, string, int> _repBetween;
        private Func<long, string, int> _repGet;
        private Func<long, string, MyTuple<int, int, float, int>> _repInfo;
        private Func<int, int> _repTierOf;
        private Func<int> _repTierCount;
        private Func<int, MyTuple<string, int, int, float, int>> _repTier;
        private Func<MyTuple<int, int>> _repScaleGet;
        private Func<string, MyTuple<int, int>> _repLimits;
        private Func<string[]> _repFactions;
        private Func<long, string, int, string, int, int> _repChange;
        private Action<long, string, int, string> _repSet;
        private Action<long> _repReset;
        // API 6
        private Func<long, string, long, bool, long> _repPrice;
        private Func<long, long, bool> _repRefuses;
        private Action<string, int> _repGateType;
        private Action<long, int> _repGateBlock;
        private Func<long, int> _repMinGoods;
        private Func<long, long> _accBalance;
        private Func<long, long, string, bool> _accCharge, _accPay;
        private Func<string, long> _accFactionBalance;
        private Func<string, long, string, bool> _accFactionChange;
        private Action<string> _priceChanged;
        private Func<long> _timeUtc;
        private Func<double> _timeWorldDays;
        private Func<MyTuple<int, int, double>> _timeWorldDate;
        private Func<bool, string> _timeWorldText;
        private Func<VRageMath.Vector3D, MyTuple<bool, double, float, bool>> _timeLocal;
        private Func<VRageMath.Vector3D, bool, string> _timeLocalText;
        private Func<VRageMath.Vector3D, MyTuple<string, string>> _timeCalendar;
        private Func<VRageMath.Vector3D, bool> _timeIsNight;
        private Action<string, object> _hudSet;
        private Func<string, object> _hudGet;
        private Func<string[]> _hudKeys;
        private Action<string, string> _supplyProfile;
        private Action<string> _supplySource;
        private Action<long, string> _supplyWatch;
        private Action<long> _supplyUnwatch;
        private Func<long, bool> _supplyIsWatched;

        private void Bind()
        {
            _isServer = Get<Func<bool>>("Info.IsServer");
            _isPlayer = Get<Func<bool>>("Info.IsPlayer");
            _promoteLevel = Get<Func<ulong, int>>("Info.PromoteLevel");
            _netRegister = Get<Action<string, Action<ulong, byte[]>>>("Net.Register");
            _netUnregister = Get<Action<string>>("Net.Unregister");
            _netToServer = Get<Action<string, byte[]>>("Net.SendToServer");
            _netToPlayer = Get<Action<string, ulong, byte[]>>("Net.SendToPlayer");
            _netToAll = Get<Action<string, byte[]>>("Net.SendToAll");
            _blockGet = Get<Func<long, string, string, string>>("Block.Get");
            _blockSet = Get<Action<long, string, string, string>>("Block.Set");
            _blockOnChanged = Get<Action<string, Action<long, string, string>>>("Block.OnChanged");
            _blockRemoveOnChanged = Get<Action<string, Action<long, string, string>>>("Block.RemoveOnChanged");
            _blockSync = Get<Action<long>>("Block.RequestSync");
            _chatRegister = Get<Action<string, int, bool, string, Func<ulong, string[], string>>>("Chat.Register");
            _chatUnregister = Get<Action<string>>("Chat.Unregister");
            _evSub = Get<Action<string, Action<object>>>("Events.Subscribe");
            _evUnsub = Get<Action<string, Action<object>>>("Events.Unsubscribe");
            _evPub = Get<Action<string, object>>("Events.Publish");
            _evToServer = Get<Action<string, string[]>>("Events.SendToServer");
            _evToAll = Get<Action<string, string[]>>("Events.SendToAll");
            _evToPlayer = Get<Action<string, ulong, string[]>>("Events.SendToPlayer");
            _evSender = Get<Func<ulong>>("Events.Sender");
            _notify = Get<Action<ulong, string, string, string[], int, string>>("Notify.Player");
            _setPublish = Get<Action<string, string>>("Settings.Publish");
            _setSubscribe = Get<Action<string, Action<string>>>("Settings.Subscribe");
            _useRegister = Get<Action<string, string, Action<long, int>>>("Use.Register");
            _useUnregister = Get<Action<string>>("Use.Unregister");
            _wikiOpen = Get<Func<string, bool>>("Wiki.Open");
            _wikiClose = Get<Action>("Wiki.Close");
            _wikiIsOpen = Get<Func<bool>>("Wiki.IsOpen");
            _wikiOnClosed = Get<Action<Action>>("Wiki.OnClosed");
            _wikiAddSource = Get<Action<Func<string, string>>>("Wiki.AddSource");
            _wikiAddTables = Get<Action<Func<string, string, string, string, string, string>>>("Wiki.AddTables");
            _wikiContext = Get<Func<string>>("Wiki.Context");
            _wikiReload = Get<Action>("Wiki.Reload");
            _wikiPlanet = Get<Func<string, bool, string[]>>("Wiki.Planet");
            _wikiTitle = Get<Func<string, bool, string>>("Wiki.Title");
            _wikiText = Get<Func<string, string, bool, string>>("Wiki.Text");
            _gridsNearby = Get<Func<VRageMath.Vector3D, double, long, long[]>>("Grids.Nearby");
            _gridsCapture = Get<Func<long, VRageMath.MatrixD, string>>("Grids.Capture");
            _gridsSpawn = Get<Func<string, VRageMath.MatrixD, long, int>>("Grids.Spawn");
            _gridsFits = Get<Func<string, VRageMath.MatrixD, long, bool>>("Grids.Fits");
            _screenRegister = Get<Action<string, int, Func<long, string>>>("Screen.Register");
            _screenRefresh = Get<Action<long>>("Screen.Refresh");
            _adminRegister = Get<Action<string, string, string>>("Admin.RegisterBlock");
            _adminUnregister = Get<Action<string>>("Admin.UnregisterBlock");
            _priceRegister = Get<Action<string, Func<string, string, long>>>("Price.Register");
            _priceHas = Get<Func<string, bool>>("Price.Has");
            _priceAsk = Get<Func<string, string, long>>("Price.Ask");
            _lightsSet = Get<Action<long, string, VRageMath.Color, float, float>>("Lights.Set");
            _lightsClear = Get<Action<long, string>>("Lights.Clear");
            _soundPlay = Get<Action<long, string, double>>("Sound.Play");
            _soundPlayAt = Get<Action<VRageMath.Vector3D, string, double>>("Sound.PlayAt");
            _repScale = Get<Action<int, int>>("Rep.DefineScale");
            _repTiers = Get<Action<List<MyTuple<string, int, float, int>>>>("Rep.DefineTiers");
            _repFaction = Get<Action<string, int, int, int, int>>("Rep.DefineFaction");
            _repRecovery = Get<Action<string, int, int>>("Rep.DefineRecovery");
            _repLink = Get<Action<string, string, float>>("Rep.DefineLink");
            _repClear = Get<Action>("Rep.ClearDefinitions");
            _repBetween = Get<Action<string, string, int>>("Rep.SetBetween");
            _repGet = Get<Func<long, string, int>>("Rep.Get");
            _repInfo = Get<Func<long, string, MyTuple<int, int, float, int>>>("Rep.Info");
            _repTierOf = Get<Func<int, int>>("Rep.TierOf");
            _repTierCount = Get<Func<int>>("Rep.TierCount");
            _repTier = Get<Func<int, MyTuple<string, int, int, float, int>>>("Rep.Tier");
            _repScaleGet = Get<Func<MyTuple<int, int>>>("Rep.Scale");
            _repLimits = Get<Func<string, MyTuple<int, int>>>("Rep.Limits");
            _repFactions = Get<Func<string[]>>("Rep.Factions");
            _repChange = Get<Func<long, string, int, string, int, int>>("Rep.Change");
            _repSet = Get<Action<long, string, int, string>>("Rep.Set");
            _repReset = Get<Action<long>>("Rep.ResetStart");
            _repPrice = Get<Func<long, string, long, bool, long>>("Rep.Price");
            _repRefuses = Get<Func<long, long, bool>>("Rep.Refuses");
            _repGateType = Get<Action<string, int>>("Rep.GateType");
            _repGateBlock = Get<Action<long, int>>("Rep.GateBlock");
            _repMinGoods = Get<Func<long, int>>("Rep.MinGoods");
            _accBalance = Get<Func<long, long>>("Account.Balance");
            _accCharge = Get<Func<long, long, string, bool>>("Account.Charge");
            _accPay = Get<Func<long, long, string, bool>>("Account.Pay");
            _accFactionBalance = Get<Func<string, long>>("Account.FactionBalance");
            _accFactionChange = Get<Func<string, long, string, bool>>("Account.FactionChange");
            _priceChanged = Get<Action<string>>("Price.Changed");
            _timeUtc = Get<Func<long>>("Time.Utc");
            _timeWorldDays = Get<Func<double>>("Time.WorldDays");
            _timeWorldDate = Get<Func<MyTuple<int, int, double>>>("Time.WorldDate");
            _timeWorldText = Get<Func<bool, string>>("Time.WorldText");
            _timeLocal = Get<Func<VRageMath.Vector3D, MyTuple<bool, double, float, bool>>>("Time.Local");
            _timeLocalText = Get<Func<VRageMath.Vector3D, bool, string>>("Time.LocalText");
            _timeCalendar = Get<Func<VRageMath.Vector3D, MyTuple<string, string>>>("Time.Calendar");
            _timeIsNight = Get<Func<VRageMath.Vector3D, bool>>("Time.IsNight");
            _hudSet = Get<Action<string, object>>("Hud.Set");
            _hudGet = Get<Func<string, object>>("Hud.Get");
            _hudKeys = Get<Func<string[]>>("Hud.Keys");
            _supplyProfile = Get<Action<string, string>>("Supply.Profile");
            _supplySource = Get<Action<string>>("Supply.RegisterSource");
            _supplyWatch = Get<Action<long, string>>("Supply.Watch");
            _supplyUnwatch = Get<Action<long>>("Supply.Unwatch");
            _supplyIsWatched = Get<Func<long, bool>>("Supply.IsWatched");
        }

        // ---------- info ----------

        /// <summary>True on the server (also in single player and for a hosting player).</summary>
        public bool IsServer { get { return _isServer != null ? _isServer() : MyAPIGateway.Session.IsServer; } }

        /// <summary>True where a player sits in front of the screen (not on a dedicated server).</summary>
        public bool IsPlayer { get { return _isPlayer != null ? _isPlayer() : !MyAPIGateway.Utilities.IsDedicated; } }

        /// <summary>Promote level of a player (0 none ... 5 owner). Works on the server for any player.</summary>
        /// <param name="steamId">Steam id of the player.</param>
        public MyPromoteLevel PromoteLevel(ulong steamId) { return _promoteLevel != null ? (MyPromoteLevel)_promoteLevel(steamId) : MyPromoteLevel.None; }

        // ---------- network ----------
        // Channel names must start with your mod name, e.g. "MyMod.sync".
        // On a hosting player (server and player in one) both directions arrive at the same handler.

        /// <summary>Registers a handler for a named channel; handler(senderSteamId, data). May be called before ready.</summary>
        /// <param name="channel">Channel name, starting with your mod name.</param>
        /// <param name="handler">Receives the sender's Steam id and the payload.</param>
        public void NetRegister(string channel, Action<ulong, byte[]> handler) { WhenReady(() => { if (_netRegister != null) _netRegister(channel, handler); }); }

        /// <summary>Removes the handler of a channel of this mod.</summary>
        /// <param name="channel">Channel name.</param>
        public void NetUnregister(string channel) { if (_netUnregister != null) _netUnregister(channel); }

        /// <summary>Sends data to the server's handler of the channel.</summary>
        /// <param name="channel">Channel name.</param>
        /// <param name="data">Payload; serialize it yourself.</param>
        public void NetSendToServer(string channel, byte[] data) { if (_netToServer != null) _netToServer(channel, data); }

        /// <summary>Server only: sends data to one player.</summary>
        /// <param name="channel">Channel name.</param>
        /// <param name="steamId">Receiving player.</param>
        /// <param name="data">Payload.</param>
        public void NetSendToPlayer(string channel, ulong steamId, byte[] data) { if (_netToPlayer != null) _netToPlayer(channel, steamId, data); }

        /// <summary>Server only: every player, including a hosting player.</summary>
        /// <param name="channel">Channel name.</param>
        /// <param name="data">Payload.</param>
        public void NetSendToAll(string channel, byte[] data) { if (_netToAll != null) _netToAll(channel, data); }

        // ---------- block settings ----------
        // Text per block, saved in the block and mirrored to all players.
        // Use your mod name as modKey. Null or "" removes the value.

        /// <summary>Value of a block setting, null if not set.</summary>
        /// <param name="entityId">Entity id of the terminal block.</param>
        /// <param name="modKey">Your mod name.</param>
        /// <param name="key">Setting name.</param>
        public string BlockGet(long entityId, string modKey, string key) { return _blockGet != null ? _blockGet(entityId, modKey, key) : null; }

        /// <summary>Sets a block setting. On a player this is a request the server checks (block access).</summary>
        /// <param name="entityId">Entity id of the terminal block.</param>
        /// <param name="modKey">Your mod name.</param>
        /// <param name="key">Setting name.</param>
        /// <param name="value">New value; null or "" removes it.</param>
        public void BlockSet(long entityId, string modKey, string key, string value) { if (_blockSet != null) _blockSet(entityId, modKey, key, value); }

        /// <summary>Handler(entityId, key, value) runs on the server and on players whenever a value of modKey changes.</summary>
        /// <param name="modKey">Your mod name.</param>
        /// <param name="handler">Receives entity id, key and new value (null = removed).</param>
        public void BlockOnChanged(string modKey, Action<long, string, string> handler) { WhenReady(() => { if (_blockOnChanged != null) _blockOnChanged(modKey, handler); }); }

        /// <summary>Removes a change handler.</summary>
        /// <param name="modKey">Your mod name.</param>
        /// <param name="handler">The handler given to <see cref="BlockOnChanged"/>.</param>
        public void BlockRemoveOnChanged(string modKey, Action<long, string, string> handler) { if (_blockRemoveOnChanged != null) _blockRemoveOnChanged(modKey, handler); }

        /// <summary>Player: asks the server for the current values of a block.</summary>
        /// <param name="entityId">Entity id of the terminal block.</param>
        public void BlockRequestSync(long entityId) { if (_blockSync != null) _blockSync(entityId); }

        /// <summary>Block setting as bool ("1" = true).</summary>
        /// <param name="entityId">Entity id of the terminal block.</param>
        /// <param name="modKey">Your mod name.</param>
        /// <param name="key">Setting name.</param>
        /// <param name="def">Value when not set.</param>
        public bool BlockGetBool(long entityId, string modKey, string key, bool def)
        {
            string v = BlockGet(entityId, modKey, key);
            return v == null ? def : v == "1";
        }

        /// <summary>Block setting as float (invariant culture).</summary>
        /// <param name="entityId">Entity id of the terminal block.</param>
        /// <param name="modKey">Your mod name.</param>
        /// <param name="key">Setting name.</param>
        /// <param name="def">Value when not set or not a number.</param>
        public float BlockGetFloat(long entityId, string modKey, string key, float def)
        {
            float f;
            string v = BlockGet(entityId, modKey, key);
            return v != null && float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f) ? f : def;
        }

        /// <summary>Stores a bool block setting as "1" or "0".</summary>
        /// <param name="entityId">Entity id of the terminal block.</param>
        /// <param name="modKey">Your mod name.</param>
        /// <param name="key">Setting name.</param>
        /// <param name="value">New value.</param>
        public void BlockSetBool(long entityId, string modKey, string key, bool value) { BlockSet(entityId, modKey, key, value ? "1" : "0"); }

        /// <summary>Stores a float block setting (round-trip format, invariant culture).</summary>
        /// <param name="entityId">Entity id of the terminal block.</param>
        /// <param name="modKey">Your mod name.</param>
        /// <param name="key">Setting name.</param>
        /// <param name="value">New value.</param>
        public void BlockSetFloat(long entityId, string modKey, string key, float value) { BlockSet(entityId, modKey, key, value.ToString("R", System.Globalization.CultureInfo.InvariantCulture)); }

        // ---------- chat commands ----------

        /// <summary>
        /// Chat command "/command args". handler(senderSteamId, args) returns the reply (or null).
        /// onServer = true runs the handler on the server (register it on every machine). May be called before ready.
        /// </summary>
        /// <param name="command">Command without "/"; one word.</param>
        /// <param name="level">Lowest promote level allowed to use it.</param>
        /// <param name="onServer">Run the handler on the server instead of locally.</param>
        /// <param name="help">One line for the "/syntrix" list.</param>
        /// <param name="handler">Receives sender and arguments, returns the reply or null.</param>
        public void ChatRegister(string command, MyPromoteLevel level, bool onServer, string help, Func<ulong, string[], string> handler)
        {
            WhenReady(() => { if (_chatRegister != null) _chatRegister(command, (int)level, onServer, help, handler); });
        }

        /// <summary>API 2. Removes a command of this mod.</summary>
        /// <param name="command">Command without "/".</param>
        public void ChatUnregister(string command) { if (_chatUnregister != null) _chatUnregister(command); }

        // ---------- events ----------
        // Named events, e.g. "MyMod.CoreDestroyed". Publish stays on this machine and may carry any
        // simple payload (no classes of your own). The Send variants travel over the network; their
        // payload arrives as string[] and EventSender tells who sent it while a handler runs.

        /// <summary>Subscribes to a named event. May be called before ready.</summary>
        /// <param name="eventName">Event name, e.g. "MyMod.CoreDestroyed".</param>
        /// <param name="handler">Receives the payload (string[] for network events).</param>
        public void Subscribe(string eventName, Action<object> handler) { WhenReady(() => { if (_evSub != null) _evSub(eventName, handler); }); }

        /// <summary>Removes a subscription.</summary>
        /// <param name="eventName">Event name.</param>
        /// <param name="handler">The subscribed handler.</param>
        public void Unsubscribe(string eventName, Action<object> handler) { if (_evUnsub != null) _evUnsub(eventName, handler); }

        /// <summary>On this machine only, to every subscribing mod.</summary>
        /// <param name="eventName">Event name.</param>
        /// <param name="payload">Any simple payload (no classes of your own mod).</param>
        public void Publish(string eventName, object payload) { if (_evPub != null) _evPub(eventName, payload); }

        /// <summary>API 2. To subscribers on the server (called on the server itself: delivered there).</summary>
        /// <param name="eventName">Event name.</param>
        /// <param name="args">Payload texts.</param>
        public void SendEventToServer(string eventName, params string[] args) { if (_evToServer != null) _evToServer(eventName, args); }

        /// <summary>API 2. Server only: to subscribers on the server and on every player, each machine once.</summary>
        /// <param name="eventName">Event name.</param>
        /// <param name="args">Payload texts.</param>
        public void SendEventToAll(string eventName, params string[] args) { if (_evToAll != null) _evToAll(eventName, args); }

        /// <summary>API 2. Server only: to subscribers on one player's machine.</summary>
        /// <param name="eventName">Event name.</param>
        /// <param name="steamId">Receiving player.</param>
        /// <param name="args">Payload texts.</param>
        public void SendEventToPlayer(string eventName, ulong steamId, params string[] args) { if (_evToPlayer != null) _evToPlayer(eventName, steamId, args); }

        /// <summary>API 2. Steam id of the sender of the network event being handled right now (0 = local event).</summary>
        public ulong EventSender { get { return _evSender != null ? _evSender() : 0; } }

        /// <summary>
        /// Screen notification in each player's own language. Server: steamId 0 = everybody.
        /// textKey is looked up in MyTexts (fallback if missing), args fill {0}, {1} ...;
        /// an argument made with TextArg(key) is translated as well.
        /// </summary>
        /// <param name="steamId">Receiving player; 0 = everybody (server) or this player (client).</param>
        /// <param name="textKey">Text key in MyTexts.</param>
        /// <param name="fallback">Text when the key is missing.</param>
        /// <param name="args">Format arguments; may be null.</param>
        /// <param name="ms">Display time in milliseconds.</param>
        /// <param name="font">Notification font, e.g. "White" or "Red".</param>
        public void Notify(ulong steamId, string textKey, string fallback, string[] args = null, int ms = 3000, string font = "White")
        {
            if (_notify != null)
                _notify(steamId, textKey, fallback, args, ms, font);
            else if (!MyAPIGateway.Utilities.IsDedicated)
                MyAPIGateway.Utilities.ShowNotification(SyntrixText.Get(textKey, fallback), ms, font);
        }

        /// <summary>Marks a notification argument as a text key that each player translates.</summary>
        /// <param name="key">Text key.</param>
        public static string TextArg(string key) { return TextArgPrefix + key; }

        // ---------- F action (API 3) ----------

        /// <summary>
        /// API 3. Own F action for a block type. The block needs a UseObjectsComponent with UseObjectTypeOverride
        /// "syntrix" in its definition (see Examples); K still opens the Vanilla terminal. Players only.
        /// </summary>
        /// <param name="blockKey">"TypeId/SubtypeId" without "MyObjectBuilder_", e.g. "TerminalBlock/MyPanel".</param>
        /// <param name="hintKey">Text key (MyTexts) of the hint shown while aiming at the block; "{0}" is the key, null = "Use".</param>
        /// <param name="handler">Runs on the player who pressed F: (block entity id, action 0).</param>
        public void UseRegister(string blockKey, string hintKey, Action<long, int> handler) { WhenReady(() => { if (_useRegister != null) _useRegister(blockKey, hintKey, handler); }); }

        /// <summary>API 3. Removes the F action of a block type (only the mod that registered it).</summary>
        /// <param name="blockKey">"TypeId/SubtypeId" as given to <see cref="UseRegister"/>.</param>
        public void UseUnregister(string blockKey) { if (_useUnregister != null) _useUnregister(blockKey); }

        // ---------- grid storage (API 4, server only) ----------

        /// <summary>API 4. Result of <see cref="GridsSpawn"/>: spawned, all entity ids kept.</summary>
        public const int GridSpawnedKeptIds = 0;
        /// <summary>API 4. Result of <see cref="GridsSpawn"/>: spawned with new entity ids (an id was taken).</summary>
        public const int GridSpawnedNewIds = 1;
        /// <summary>API 4. Result of <see cref="GridsSpawn"/>: the text is no grid text.</summary>
        public const int GridInvalid = -1;
        /// <summary>API 4. Result of <see cref="GridsSpawn"/>: the place is blocked by a grid or a character.</summary>
        public const int GridBlocked = -2;
        /// <summary>API 4. Result of <see cref="GridsSpawn"/>: not on the server.</summary>
        public const int GridNotServer = -3;
        /// <summary>API 4. Result of <see cref="GridsSpawn"/>: no framework.</summary>
        public const int GridNoFramework = -4;

        /// <summary>
        /// API 4. Grid groups (mechanical) whose centre lies within the radius, as the entity id of their main grid (most
        /// blocks). Groups with a static grid, grids without physics and the group of <paramref name="exceptGrid"/>
        /// (e.g. the station itself) are left out. Empty without the framework.
        /// </summary>
        /// <param name="centre">Centre in world coordinates.</param>
        /// <param name="radius">Radius in metres.</param>
        /// <param name="exceptGrid">A grid whose group is left out, 0 = none.</param>
        public long[] GridsNearby(VRageMath.Vector3D centre, double radius, long exceptGrid)
        {
            return _gridsNearby != null ? _gridsNearby(centre, radius, exceptGrid) : new long[0];
        }

        /// <summary>
        /// API 4, server. The grid group of a grid as text (blueprint with the block storage, relative to the frame, plus
        /// the outline). The grids stay in the world: save the text first (e.g. <see cref="SyntrixFiles"/>), then close
        /// the grids. Null if the grid does not exist.
        /// </summary>
        /// <param name="gridId">Any grid of the group.</param>
        /// <param name="frame">Reference frame, e.g. the world matrix of a connector.</param>
        public string GridsCapture(long gridId, VRageMath.MatrixD frame) { return _gridsCapture != null ? _gridsCapture(gridId, frame) : null; }

        /// <summary>
        /// API 4, server. Spawns a text of <see cref="GridsCapture"/> relative to the frame after a space check.
        /// Keeps the entity ids when all are free, otherwise all get new ones. Returns one of the Grid* constants.
        /// </summary>
        /// <param name="text">Grid text.</param>
        /// <param name="frame">Reference frame.</param>
        /// <param name="exceptGrid">A grid whose group does not count as obstacle, 0 = none.</param>
        public int GridsSpawn(string text, VRageMath.MatrixD frame, long exceptGrid) { return _gridsSpawn != null ? _gridsSpawn(text, frame, exceptGrid) : GridNoFramework; }

        /// <summary>API 4, server. True if the place of the grid text at the frame is free.</summary>
        /// <param name="text">Grid text.</param>
        /// <param name="frame">Reference frame.</param>
        /// <param name="exceptGrid">A grid whose group does not count as obstacle, 0 = none.</param>
        public bool GridsFits(string text, VRageMath.MatrixD frame, long exceptGrid) { return _gridsFits != null && _gridsFits(text, frame, exceptGrid); }

        // ---------- fixed screens (API 4, players only) ----------

        /// <summary>
        /// API 4. Content of a fixed screen of a block type. Syntrix calls the function about every 100 frames for each
        /// such block within 150 m and draws only when the text changed; the screen settings leave the K menu of the
        /// blocks with a text (API 6; before, of all blocks of the type). Build the text with <see cref="ScreenLine"/> and <see cref="ScreenTitle"/>; null or "" leaves the
        /// screen as it is. A null function removes the registration. Players only, may be called before ready.
        /// </summary>
        /// <param name="blockKey">"TypeId/SubtypeId" without "MyObjectBuilder_".</param>
        /// <param name="surface">Screen index of the block (0 = first).</param>
        /// <param name="content">Block entity id in, screen text out.</param>
        public void ScreenRegister(string blockKey, int surface, Func<long, string> content) { WhenReady(() => { if (_screenRegister != null) _screenRegister(blockKey, surface, content); }); }

        /// <summary>API 4. Draws the screens of a block again at the next pass.</summary>
        /// <param name="entityId">Block entity id.</param>
        public void ScreenRefresh(long entityId) { if (_screenRefresh != null) _screenRefresh(entityId); }

        /// <summary>
        /// API 4. One line of a fixed screen. Kinds: 'A' alarm bar, 'N' name, 'S' small, 'V' value, 'H' hint (bullet),
        /// 'F' foot (bottom line), 'G' gap. "{F}" in the text becomes the key of the use control.
        /// </summary>
        /// <param name="kind">Kind of the line.</param>
        /// <param name="text">Text (tabs and line breaks become spaces).</param>
        public static string ScreenLine(char kind, string text) { return kind + "\t" + Clean(text) + "\n"; }

        /// <summary>API 4. The title line of a fixed screen with an optional symbol (LCD texture subtype, null = none).</summary>
        /// <param name="symbol">LCD texture subtype or null.</param>
        /// <param name="text">Title.</param>
        public static string ScreenTitle(string symbol, string text) { return "T\t" + Clean(symbol) + "\t" + Clean(text) + "\n"; }

        private static string Clean(string text) { return (text ?? "").Replace('\t', ' ').Replace('\n', ' ').Replace("\r", ""); }

        // ---------- admin only blocks (API 4) ----------

        /// <summary>
        /// API 4. A block type for admins only: in the G menu and its search only while the local player is admin and,
        /// in survival, has creative tools on (Alt+F10).
        /// Call it on every machine; may be called before ready. Up to 1.4 the server also razed such a block built by a
        /// player without admin rank; since 1.5 it does not.
        /// </summary>
        /// <param name="blockKey">"TypeId/SubtypeId" without "MyObjectBuilder_".</param>
        /// <param name="noticeKey">No effect since 1.5.</param>
        /// <param name="fallback">No effect since 1.5.</param>
        public void AdminBlockRegister(string blockKey, string noticeKey, string fallback) { WhenReady(() => { if (_adminRegister != null) _adminRegister(blockKey, noticeKey, fallback); }); }

        /// <summary>API 4. Removes the admin rule of a block type (only the mod that registered it).</summary>
        /// <param name="blockKey">"TypeId/SubtypeId".</param>
        public void AdminBlockUnregister(string blockKey) { if (_adminUnregister != null) _adminUnregister(blockKey); }

        // ---------- price requests (API 4) ----------

        /// <summary>
        /// API 4. Registers this mod as price giver of a service (e.g. "Npc.Repair"). The giver gets the service name
        /// and the description (lines "key=value", see <see cref="PriceLine"/>) and returns the price, or -1 for
        /// "no price". Null removes it. Register where prices are asked (usually the server); may be called before ready.
        /// </summary>
        /// <param name="service">Service name.</param>
        /// <param name="giver">(service, description) → price or -1.</param>
        public void PriceRegister(string service, Func<string, string, long> giver) { WhenReady(() => { if (_priceRegister != null) _priceRegister(service, giver); }); }

        /// <summary>API 4. True if a price giver is registered for the service.</summary>
        /// <param name="service">Service name.</param>
        public bool PriceHas(string service) { return _priceHas != null && _priceHas(service); }

        /// <summary>
        /// API 4. Price of a service from its price giver; -1 if there is none, it has no price or the framework is
        /// missing. Then use your own price.
        /// </summary>
        /// <param name="service">Service name.</param>
        /// <param name="description">Lines "key=value", built with <see cref="PriceLine"/>.</param>
        public long PriceAsk(string service, string description) { return _priceAsk != null ? _priceAsk(service, description) : -1; }

        /// <summary>
        /// API 4. One line "key=value" of a price description. Common keys: station (entity id), faction (tag), player
        /// (identity id), blocks (count), "item:TypeId/SubtypeId" (amount), "block:TypeId/SubtypeId" (count).
        /// </summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value.</param>
        public static string PriceLine(string key, string value) { return Clean(key).Replace("=", "") + "=" + Clean(value) + "\n"; }

        // ---------- block lights (API 4, Syntrix 1.3.0) ----------

        /// <summary>
        /// API 4 (1.3.0). Colour and blinking of an emissive part of a block (status lights). Call it on the server: Syntrix
        /// keeps the state, sends it to every player (also to later ones) and paints it again when the game repaints the
        /// block (on, off, damage). Not saved; set it again after a restart. On a player it changes only that view.
        /// </summary>
        /// <param name="entityId">Block entity id.</param>
        /// <param name="part">Emissive material name, null = "Emissive" (the status lights of most blocks).</param>
        /// <param name="color">Colour.</param>
        /// <param name="emissivity">Brightness 0 to 1.</param>
        /// <param name="blink">Blink period in seconds (half on, half off), 0 = steady.</param>
        public void LightsSet(long entityId, string part, VRageMath.Color color, float emissivity = 1f, float blink = 0f)
        {
            if (_lightsSet != null) _lightsSet(entityId, part, color, emissivity, blink);
        }

        /// <summary>API 4 (1.3.0). Gives an emissive part (null = all parts set by mods) back to the game.</summary>
        /// <param name="entityId">Block entity id.</param>
        /// <param name="part">Emissive material name, null = all.</param>
        public void LightsClear(long entityId, string part = null) { if (_lightsClear != null) _lightsClear(entityId, part); }

        // ---------- short sounds (API 4, Syntrix 1.3.0) ----------

        /// <summary>
        /// API 4 (1.3.0). Plays a short sound at a block for every player within the radius (server), or only here (player).
        /// Cue names as in Audio.sbc without "Arc"/"Real", e.g. "HudUnable", "SoundBlockAlert2". Loops belong in a sound block.
        /// </summary>
        /// <param name="entityId">Block (or any entity) id; the sound follows it.</param>
        /// <param name="cue">Sound cue name.</param>
        /// <param name="radius">Radius in metres (1 to 200).</param>
        public void SoundPlay(long entityId, string cue, double radius = 30) { if (_soundPlay != null) _soundPlay(entityId, cue, radius); }

        /// <summary>API 4 (1.3.0). Plays a short sound at a place, like <see cref="SoundPlay"/>.</summary>
        /// <param name="position">Place in world coordinates.</param>
        /// <param name="cue">Sound cue name.</param>
        /// <param name="radius">Radius in metres (1 to 200).</param>
        public void SoundPlayAt(VRageMath.Vector3D position, string cue, double radius = 30) { if (_soundPlayAt != null) _soundPlayAt(position, cue, radius); }

        // ---------- reputation (API 5, Syntrix 1.4.0) ----------

        /// <summary>API 5. Event name of a reputation change; payload MyTuple&lt;long identity, string tag, int old, int new, string reason&gt;.</summary>
        public const string RepChangedEvent = "Syntrix.Rep.Changed";
        /// <summary>API 5. <see cref="RepChange"/> flag: pass a share on to linked factions (<see cref="RepDefineLink"/>).</summary>
        public const int RepPropagate = 1;
        /// <summary>API 5. <see cref="RepChange"/> flag: no notice of the amount (tier changes are still shown).</summary>
        public const int RepQuiet = 2;

        /// <summary>
        /// API 5. Server, one mod only (the faction mod): lowest and highest value of the scale; also the limits of every
        /// faction without own limits. Without it the scale is -1500 to 1500 (Vanilla). Players get every definition from
        /// the server. May be called before ready.
        /// </summary>
        /// <param name="min">Lowest value.</param>
        /// <param name="max">Highest value.</param>
        public void RepDefineScale(int min, int max) { WhenReady(() => { if (_repScale != null) _repScale(min, max); }); }

        /// <summary>
        /// API 5. Server: the tiers, each (text key of the name, lowest value, trade percent, goods level). The percent is
        /// the discount on buying and the markup on selling; goods level 0 means no trade, higher levels open better goods.
        /// </summary>
        /// <param name="tiers">Tiers in any order.</param>
        public void RepDefineTiers(List<MyTuple<string, int, float, int>> tiers) { WhenReady(() => { if (_repTiers != null) _repTiers(tiers); }); }

        /// <summary>
        /// API 5. Server: a faction. Syntrix gives each player the start value once (the first time the player is seen
        /// with this definition) and keeps the value between min and max, also against changes by the game or other mods.
        /// </summary>
        /// <param name="tag">Faction tag.</param>
        /// <param name="start">Start value, int.MinValue = none.</param>
        /// <param name="min">Lowest value (int.MinValue = scale).</param>
        /// <param name="max">Highest value (int.MaxValue = scale).</param>
        /// <param name="tradeFrom">Lowest tier index that trades (basic goods, 0 %) even if the tier itself does not; -1 = as the tier says.</param>
        public void RepDefineFaction(string tag, int start, int min = int.MinValue, int max = int.MaxValue, int tradeFrom = -1)
        {
            WhenReady(() => { if (_repFaction != null) _repFaction(tag, start, min, max, tradeFrom); });
        }

        /// <summary>API 5. Server: while a player is online the value rises by perHour points per hour, up to upTo.</summary>
        /// <param name="tag">Faction tag.</param>
        /// <param name="perHour">Points per online hour, 0 = off.</param>
        /// <param name="upTo">Value where it stops.</param>
        public void RepDefineRecovery(string tag, int perHour, int upTo) { WhenReady(() => { if (_repRecovery != null) _repRecovery(tag, perHour, upTo); }); }

        /// <summary>
        /// API 5. Server: a change at "from" made with <see cref="RepPropagate"/> also changes "to" by factor times the
        /// applied change (0.05 = 5 % the same way, -0.02 = 2 % the other way), inside the limits of "to". One step only.
        /// </summary>
        /// <param name="from">Faction tag that changes.</param>
        /// <param name="to">Faction tag that follows.</param>
        /// <param name="factor">Share, negative = the other way, 0 removes the link.</param>
        public void RepDefineLink(string from, string to, float factor) { WhenReady(() => { if (_repLink != null) _repLink(from, to, factor); }); }

        /// <summary>API 5. Server: forgets all definitions, before defining them again (for example after reloading a settings file).</summary>
        public void RepClearDefinitions() { WhenReady(() => { if (_repClear != null) _repClear(); }); }

        /// <summary>API 5. Server: reputation between two factions both ways, also on every player's machine (also later ones).</summary>
        /// <param name="tagA">First faction tag.</param>
        /// <param name="tagB">Second faction tag.</param>
        /// <param name="value">Value on the scale.</param>
        public void RepSetBetween(string tagA, string tagB, int value) { WhenReady(() => { if (_repBetween != null) _repBetween(tagA, tagB, value); }); }

        /// <summary>API 5. Value of a player with a faction; int.MinValue if the faction does not exist or the framework is missing.</summary>
        /// <param name="identity">Player identity id.</param>
        /// <param name="tag">Faction tag.</param>
        public int RepGet(long identity, string tag) { return _repGet != null ? _repGet(identity, tag) : int.MinValue; }

        /// <summary>
        /// API 5. Value, tier index (-1 = none), trade percent and goods level (0 = no trade) of a player with a faction.
        /// Works on the server and on the player's own machine.
        /// </summary>
        /// <param name="identity">Player identity id.</param>
        /// <param name="tag">Faction tag.</param>
        public MyTuple<int, int, float, int> RepInfo(long identity, string tag)
        {
            return _repInfo != null ? _repInfo(identity, tag) : new MyTuple<int, int, float, int>(int.MinValue, -1, 0f, 0);
        }

        /// <summary>API 5. Tier index of a value, -1 when no tiers are defined.</summary>
        /// <param name="value">Value on the scale.</param>
        public int RepTierOf(int value) { return _repTierOf != null ? _repTierOf(value) : -1; }

        /// <summary>API 5. Number of tiers.</summary>
        public int RepTierCount { get { return _repTierCount != null ? _repTierCount() : 0; } }

        /// <summary>API 5. Tier: text key of the name, lowest and highest value, trade percent, goods level.</summary>
        /// <param name="index">Tier index.</param>
        public MyTuple<string, int, int, float, int> RepTier(int index)
        {
            return _repTier != null ? _repTier(index) : new MyTuple<string, int, int, float, int>(null, 0, 0, 0f, 0);
        }

        /// <summary>API 5. Lowest and highest value of the scale.</summary>
        public MyTuple<int, int> RepScale { get { return _repScaleGet != null ? _repScaleGet() : new MyTuple<int, int>(-1500, 1500); } }

        /// <summary>API 5. Lowest and highest allowed value of a faction.</summary>
        /// <param name="tag">Faction tag.</param>
        public MyTuple<int, int> RepLimits(string tag) { return _repLimits != null ? _repLimits(tag) : new MyTuple<int, int>(-1500, 1500); }

        /// <summary>API 5. Tags of the defined factions, sorted; empty without framework.</summary>
        public string[] RepFactions() { return _repFactions != null ? _repFactions() : new string[0]; }

        /// <summary>
        /// API 5. Server: changes a player's value inside the faction's limits and returns the applied change. The player
        /// sees a notice of the amount (summed up over two seconds) unless <see cref="RepQuiet"/>, and always a notice
        /// when the tier changes.
        /// </summary>
        /// <param name="identity">Player identity id.</param>
        /// <param name="tag">Faction tag.</param>
        /// <param name="delta">Change.</param>
        /// <param name="reason">Reason for the event, e.g. "Npc.Kill".</param>
        /// <param name="flags"><see cref="RepPropagate"/>, <see cref="RepQuiet"/>.</param>
        public int RepChange(long identity, string tag, int delta, string reason, int flags = RepPropagate)
        {
            return _repChange != null ? _repChange(identity, tag, delta, reason, flags) : 0;
        }

        /// <summary>API 5. Server: sets a player's value inside the faction's limits (no notice of the amount).</summary>
        /// <param name="identity">Player identity id.</param>
        /// <param name="tag">Faction tag.</param>
        /// <param name="value">New value.</param>
        /// <param name="reason">Reason for the event.</param>
        public void RepSet(long identity, string tag, int value, string reason) { if (_repSet != null) _repSet(identity, tag, value, reason); }

        /// <summary>API 5. Server: gives a player the start values of all defined factions again.</summary>
        /// <param name="identity">Player identity id.</param>
        public void RepResetStart(long identity) { if (_repReset != null) _repReset(identity); }

        /// <summary>
        /// API 5. Handler for every reputation change (identity, tag, old, new, reason), on the server and on the player's
        /// own machine. May be called before ready.
        /// </summary>
        /// <param name="handler">Handler.</param>
        /// <returns>The subscription, for <see cref="Unsubscribe"/> with <see cref="RepChangedEvent"/>.</returns>
        public Action<object> RepOnChanged(Action<long, string, int, int, string> handler)
        {
            Action<object> h = o =>
            {
                if (o is MyTuple<long, string, int, int, string>)
                {
                    var t = (MyTuple<long, string, int, int, string>)o;
                    handler(t.Item1, t.Item2, t.Item3, t.Item4, t.Item5);
                }
            };
            Subscribe(RepChangedEvent, h);
            return h;
        }

        // ---------- reputation gate and prices (API 6, Syntrix 1.5.0) ----------

        /// <summary>
        /// API 6. Price for a player at a faction after the trade percent of the player's tier. playerBuys true: the
        /// percent is a discount (rounded up); false (the faction pays the player): a markup (rounded down). Without
        /// framework, faction or tier the price comes back unchanged.
        /// </summary>
        /// <param name="identity">Player identity id.</param>
        /// <param name="tag">Faction tag.</param>
        /// <param name="price">Base price.</param>
        /// <param name="playerBuys">True when the player pays.</param>
        public long RepPrice(long identity, string tag, long price, bool playerBuys) { return _repPrice != null ? _repPrice(identity, tag, price, playerBuys) : price; }

        /// <summary>
        /// API 6. True when the block belongs to a defined faction, the player is no member and the player's goods level
        /// with it is below what the block asks for (<see cref="RepGateBlock"/>, <see cref="RepGateType"/>, else 1).
        /// Server and the player's own machine.
        /// </summary>
        /// <param name="blockId">Block entity id.</param>
        /// <param name="identity">Player identity id.</param>
        public bool RepRefuses(long blockId, long identity) { return _repRefuses != null && _repRefuses(blockId, identity); }

        /// <summary>
        /// API 6. All machines: every block of this type ("MyObjectBuilder_StoreBlock" or "TypeId/SubtypeId") closes its
        /// terminal, F screen and store for players whose goods level with the owning faction is below minGoods. 0 removes
        /// the type. Blocks with Syntrix's F action (SyntrixUse) are gated anyway (goods level 1).
        /// </summary>
        /// <param name="type">Block type.</param>
        /// <param name="minGoods">Lowest goods level, 0 = off.</param>
        public void RepGateType(string type, int minGoods) { WhenReady(() => { if (_repGateType != null) _repGateType(type, minGoods); }); }

        /// <summary>API 6. Server: lowest goods level of one block, saved with the block; 0 = the type's value.</summary>
        /// <param name="blockId">Block entity id.</param>
        /// <param name="minGoods">Lowest goods level, 0 = default.</param>
        public void RepGateBlock(long blockId, int minGoods) { if (_repGateBlock != null) _repGateBlock(blockId, minGoods); }

        /// <summary>API 6. Lowest goods level a block asks for (its own, its type's, else 1).</summary>
        /// <param name="blockId">Block entity id.</param>
        public int RepMinGoods(long blockId) { return _repMinGoods != null ? _repMinGoods(blockId) : 1; }

        // ---------- accounts (API 6, Syntrix 1.5.0, server) ----------

        /// <summary>API 6. Event name of a balance change made through Syntrix; payload MyTuple&lt;long identity (0 = faction), string tag (null = player), long change, string reason&gt;.</summary>
        public const string AccountChangedEvent = "Syntrix.Account.Changed";

        /// <summary>API 6. Server: balance of an online player, -1 when not online or without framework.</summary>
        /// <param name="identity">Player identity id.</param>
        public long AccountBalance(long identity) { return _accBalance != null ? _accBalance(identity) : -1; }

        /// <summary>
        /// API 6. Server: takes the amount from an online player when the balance covers it (also counting what was taken
        /// earlier in the same frame). False and nothing taken otherwise.
        /// </summary>
        /// <param name="identity">Player identity id.</param>
        /// <param name="amount">Amount, not negative.</param>
        /// <param name="reason">Reason for the event, e.g. "MyMod.Repair".</param>
        public bool AccountCharge(long identity, long amount, string reason) { return _accCharge != null && _accCharge(identity, amount, reason); }

        /// <summary>API 6. Server: gives an online player the amount.</summary>
        /// <param name="identity">Player identity id.</param>
        /// <param name="amount">Amount, not negative.</param>
        /// <param name="reason">Reason for the event.</param>
        public bool AccountPay(long identity, long amount, string reason) { return _accPay != null && _accPay(identity, amount, reason); }

        /// <summary>API 6. Server: balance of a faction, -1 when it does not exist.</summary>
        /// <param name="tag">Faction tag.</param>
        public long AccountFactionBalance(string tag) { return _accFactionBalance != null ? _accFactionBalance(tag) : -1; }

        /// <summary>API 6. Server: changes a faction's balance; a negative change only when the balance covers it.</summary>
        /// <param name="tag">Faction tag.</param>
        /// <param name="delta">Change.</param>
        /// <param name="reason">Reason for the event.</param>
        public bool AccountFactionChange(string tag, long delta, string reason) { return _accFactionChange != null && _accFactionChange(tag, delta, reason); }

        // ---------- price changes (API 6) ----------

        /// <summary>API 6. Event name raised by <see cref="PriceChanged"/>; payload: the service name (string).</summary>
        public const string PriceChangedEvent = "Syntrix.Price.Changed";

        /// <summary>API 6. A price giver tells the askers that its prices of a service changed (raised where it is called).</summary>
        /// <param name="service">Service name, e.g. "Npc.Repair".</param>
        public void PriceChanged(string service) { if (_priceChanged != null) _priceChanged(service); }

        // ---------- world clock (API 6, Syntrix 1.5.0) ----------

        /// <summary>API 6. Event on the server: a new world day began, MyTuple&lt;int year, int day&gt;.</summary>
        public const string TimeNewDayEvent = "Syntrix.Time.NewDay";

        /// <summary>API 6. Real UTC time aligned to the server clock (local clock without framework).</summary>
        public DateTime TimeUtc { get { return _timeUtc != null ? new DateTime(_timeUtc(), DateTimeKind.Utc) : DateTime.UtcNow; } }

        /// <summary>API 6. World days since year 1, day 1, 00:00 (fraction = time of day); 0 without framework.</summary>
        public double TimeWorldDays { get { return _timeWorldDays != null ? _timeWorldDays() : 0; } }

        /// <summary>API 6. World date: year, day of the year from 1, hour 0 to 24.</summary>
        public MyTuple<int, int, double> TimeWorldDate() { return _timeWorldDate != null ? _timeWorldDate() : new MyTuple<int, int, double>(1, 1, 0); }

        /// <summary>API 6. World date and time as text in this machine's language.</summary>
        /// <param name="twelveHour">12 hour clock.</param>
        public string TimeWorldText(bool twelveHour = false) { return _timeWorldText != null ? _timeWorldText(twelveHour) : ""; }

        /// <summary>API 6. Local time at a place near a planet: valid, hours 0 to 24, sun height in degrees, night.</summary>
        /// <param name="position">Place in world coordinates.</param>
        public MyTuple<bool, double, float, bool> TimeLocal(VRageMath.Vector3D position)
        {
            return _timeLocal != null ? _timeLocal(position) : new MyTuple<bool, double, float, bool>(false, 0, 0f, false);
        }

        /// <summary>API 6. Local time as "HH:mm", "--:--" without one.</summary>
        /// <param name="position">Place in world coordinates.</param>
        /// <param name="twelveHour">12 hour clock.</param>
        public string TimeLocalText(VRageMath.Vector3D position, bool twelveHour = false) { return _timeLocalText != null ? _timeLocalText(position, twelveHour) : "--:--"; }

        /// <summary>API 6. Planet calendar at a place: name and date text, both null without one.</summary>
        /// <param name="position">Place in world coordinates.</param>
        public MyTuple<string, string> TimeCalendar(VRageMath.Vector3D position) { return _timeCalendar != null ? _timeCalendar(position) : new MyTuple<string, string>(null, null); }

        /// <summary>API 6. Night at a place by sun height (false without local time).</summary>
        /// <param name="position">Place in world coordinates.</param>
        public bool TimeIsNight(VRageMath.Vector3D position) { return _timeIsNight != null && _timeIsNight(position); }

        // ---------- HUD values (API 6, Syntrix 1.5.0, players only) ----------

        /// <summary>API 6. Event on the player's machine when a HUD value changed: MyTuple&lt;string key, object value&gt;
        /// (value double or string, null = removed).</summary>
        public const string HudEvent = "Syntrix.Hud";

        /// <summary>API 6. Hands a value to the HUD and other displays on the player's machine. Key with mod prefix
        /// ("MyMod.Speed"); value double (int, float, bool are turned into double), string, or null to remove it.
        /// Syntrix keeps the last value, so send only on change. Does nothing on a dedicated server.
        /// Clock values come from Syntrix itself: Time.Local, Time.Night, Time.SunHeight, Time.WorldYear,
        /// Time.WorldDay, Time.WorldHour, Time.Calendar, Time.CalendarDate.</summary>
        public void HudSet(string key, object value) { if (_hudSet != null) _hudSet(key, value); }

        /// <summary>API 6. Current HUD value of a key (double or string), null when there is none.</summary>
        public object HudGet(string key) { return _hudGet != null ? _hudGet(key) : null; }

        /// <summary>API 6. HUD value as a number; fallback when there is none or it is text.</summary>
        public double HudNumber(string key, double fallback = 0)
        {
            object v = HudGet(key);
            return v is double ? (double)v : fallback;
        }

        /// <summary>API 6. HUD value as text, null when there is none or it is a number.</summary>
        public string HudText(string key) { return HudGet(key) as string; }

        /// <summary>API 6. All keys that have a HUD value right now (e.g. to fill a display after docking).</summary>
        public string[] HudKeys() { return _hudKeys != null ? _hudKeys() : new string[0]; }

        // ---------- supply of grid groups (API 6, Syntrix 1.5.0, server) ----------

        /// <summary>
        /// API 6. Server: defines or replaces a supply profile (name with mod prefix, e.g. "MyMod.Base"). Settings are lines
        /// "key=value" (<see cref="SupplyLine"/>): CheckSeconds (5), NpcOnly (true: only blocks owned by an NPC),
        /// Reactors (true), ReactorFuel (10 pieces), WeaponMagazines (4 per weapon), StockPerWeapon (10 default magazines
        /// per weapon), RefillSeconds (120), RefillPerWeapon (1). Missing keys keep these defaults. May be called before ready.
        /// </summary>
        /// <param name="name">Profile name.</param>
        /// <param name="settings">Lines "key=value".</param>
        public void SupplyProfile(string name, string settings) { WhenReady(() => { if (_supplyProfile != null) _supplyProfile(name, settings); }); }

        /// <summary>API 6. One line "key=value" of a supply profile.</summary>
        /// <param name="key">Key.</param>
        /// <param name="value">Value (numbers with a dot, true/false).</param>
        public static string SupplyLine(string key, string value) { return Clean(key).Replace("=", "") + "=" + Clean(value) + "\n"; }

        /// <summary>
        /// API 6. A block type "TypeId/SubtypeId" whose inventory holds the stock of a supplied group (an ammo store):
        /// Syntrix fills it once, refills it slowly and loads the weapons from it; what lies inside is loot. Groups without
        /// such a block get an invisible stock. Call it where the server runs; may be called before ready.
        /// </summary>
        /// <param name="blockKey">"TypeId/SubtypeId" without "MyObjectBuilder_".</param>
        public void SupplyRegisterSource(string blockKey) { WhenReady(() => { if (_supplySource != null) _supplySource(blockKey); }); }

        /// <summary>
        /// API 6. Server: keeps the mechanical grid group of the anchor (a block or grid entity id) supplied with the profile:
        /// reactors on and fuelled, weapons loaded from the stock of the group, out of nothing and without conveyors. Lasts
        /// until <see cref="SupplyUnwatch"/>; not saved, so watch again after a restart (the stock is saved). Calling it
        /// again only changes the profile.
        /// </summary>
        /// <param name="anchorId">Block or grid entity id.</param>
        /// <param name="profile">Profile name.</param>
        public void SupplyWatch(long anchorId, string profile) { if (_supplyWatch != null) _supplyWatch(anchorId, profile); }

        /// <summary>API 6. Server: ends the supply of the anchor and forgets its invisible stock.</summary>
        /// <param name="anchorId">Block or grid entity id.</param>
        public void SupplyUnwatch(long anchorId) { if (_supplyUnwatch != null) _supplyUnwatch(anchorId); }

        /// <summary>API 6. Server: true while the anchor is supplied.</summary>
        /// <param name="anchorId">Block or grid entity id.</param>
        public bool SupplyIsWatched(long anchorId) { return _supplyIsWatched != null && _supplyIsWatched(anchorId); }

        // ---------- helpers without endpoint (1.3.0) ----------

        /// <summary>
        /// 1.3.0. Server: <see cref="Notify"/> to every player whose character is within the radius of the place, each in
        /// their own language. On a player: shown here if this player is within the radius.
        /// </summary>
        /// <param name="position">Place in world coordinates.</param>
        /// <param name="radius">Radius in metres.</param>
        /// <param name="textKey">Text key in MyTexts.</param>
        /// <param name="fallback">Text when the key is missing.</param>
        /// <param name="args">Format arguments; may be null.</param>
        /// <param name="ms">Display time in milliseconds.</param>
        /// <param name="font">Notification font.</param>
        public void NotifyNear(VRageMath.Vector3D position, double radius, string textKey, string fallback, string[] args = null, int ms = 3000, string font = "White")
        {
            foreach (var player in PlayersNear(position, radius))
            {
                if (IsServer)
                    Notify(player.SteamUserId, textKey, fallback, args, ms, font);
                else if (player.SteamUserId == MyAPIGateway.Multiplayer.MyId)
                    Notify(0, textKey, fallback, args, ms, font);
            }
        }

        /// <summary>1.3.0. Players with a living character within the radius of the place (character centre).</summary>
        /// <param name="position">Place in world coordinates.</param>
        /// <param name="radius">Radius in metres.</param>
        public static List<IMyPlayer> PlayersNear(VRageMath.Vector3D position, double radius)
        {
            var list = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(list, p => p.Character != null && !p.Character.IsDead
                && VRageMath.Vector3D.DistanceSquared(p.Character.WorldAABB.Center, position) <= radius * radius);
            return list;
        }

        /// <summary>
        /// 1.3.0. Players with a living character inside a box, e.g. a doorway in block coordinates: build it with
        /// <c>new MyOrientedBoundingBoxD(localBox, block.WorldMatrix)</c>. The character counts with its body radius.
        /// </summary>
        /// <param name="box">Box in world coordinates.</param>
        /// <param name="bodyRadius">Added around the character centre (metres); 0 = only the centre.</param>
        public static List<IMyPlayer> PlayersIn(VRageMath.MyOrientedBoundingBoxD box, double bodyRadius = 0.35)
        {
            var list = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(list, p =>
            {
                if (p.Character == null || p.Character.IsDead)
                    return false;
                var sphere = new VRageMath.BoundingSphereD(p.Character.WorldAABB.Center, bodyRadius);
                return bodyRadius > 0 ? box.Intersects(ref sphere) : box.Contains(ref sphere.Center);
            });
            return list;
        }

        // ---------- wiki (API 3) ----------

        /// <summary>API 3. Opens the wiki (players only); false if it could not open.</summary>
        /// <param name="page">Page id to show, null = the last page.</param>
        public bool WikiOpen(string page = null) { return _wikiOpen != null && _wikiOpen(page); }

        /// <summary>API 3. Closes the wiki window.</summary>
        public void WikiClose() { if (_wikiClose != null) _wikiClose(); }

        /// <summary>API 3. True while the wiki window is open.</summary>
        public bool WikiIsOpen { get { return _wikiIsOpen != null && _wikiIsOpen(); } }

        /// <summary>API 3. Runs when the wiki window closes (one handler per mod; null removes it).</summary>
        /// <param name="handler">Handler without arguments.</param>
        public void WikiOnClosed(Action handler) { WhenReady(() => { if (_wikiOnClosed != null) _wikiOnClosed(handler); }); }

        /// <summary>
        /// API 3. Generated pages: Syntrix calls the function when it builds the wiki, with the context as XML
        /// (&lt;SyxWiki&gt; with all &lt;System&gt; lists and one &lt;Page Id Parent Order Source&gt; stub per page),
        /// and merges the returned &lt;SyxWiki&gt; text like a wiki file (null or empty = nothing).
        /// </summary>
        /// <param name="source">Context XML in, wiki XML out; null removes it.</param>
        public void WikiAddSource(Func<string, string> source) { WhenReady(() => { if (_wikiAddSource != null) _wikiAddSource(source); }); }

        /// <summary>
        /// API 3. Automatic tables and lists (Auto="..." in a wiki file): (kind "Table" or "List", auto, system id,
        /// planet id, page id) → &lt;SyxWiki&gt;&lt;Page Id="_"&gt; with one &lt;Table&gt; or &lt;List&gt;, or null if the
        /// Auto name is not yours.
        /// </summary>
        /// <param name="tables">Filler; null removes it.</param>
        public void WikiAddTables(Func<string, string, string, string, string, string> tables) { WhenReady(() => { if (_wikiAddTables != null) _wikiAddTables(tables); }); }

        /// <summary>API 3. The context XML (systems and page stubs of all wiki files), null without the framework or on a server.</summary>
        public string WikiContext() { return _wikiContext != null ? _wikiContext() : null; }

        /// <summary>API 3. Builds the wiki again on its next opening (e.g. after generated pages changed).</summary>
        public void WikiReload() { if (_wikiReload != null) _wikiReload(); }

        /// <summary>
        /// API 3. A planet of the wiki for an exact or partial planet definition subtype (e.g. "Earth" finds "Sol-Earth"):
        /// [0] page id, [1] name, [2] image material ("" if none or not loaded), [3] image credit, [4] facts text,
        /// [5] short line with gravity, atmosphere and temperature. Null if no planet matches (players only).
        /// </summary>
        /// <param name="definition">Planet definition subtype or a part of it.</param>
        /// <param name="german">True for German texts.</param>
        public string[] WikiPlanet(string definition, bool german) { return _wikiPlanet != null ? _wikiPlanet(definition, german) : null; }

        /// <summary>API 3. Title of a wiki page in the language, null if the page does not exist (players only).</summary>
        /// <param name="page">Page id.</param>
        /// <param name="german">True for the German title.</param>
        public string WikiTitle(string page, bool german) { return _wikiTitle != null ? _wikiTitle(page, german) : null; }

        /// <summary>
        /// API 3. A text key in the wanted language, independent of the game language (the wiki shows both): read from
        /// Data/Localization/&lt;file&gt;.resx (English) or &lt;file&gt;.de.resx (German) of every loaded mod; null if no mod has it.
        /// </summary>
        /// <param name="file">Base name of the resx file, e.g. "MyTexts_GridCore".</param>
        /// <param name="key">Text key.</param>
        /// <param name="german">True for German.</param>
        public string WikiText(string file, string key, bool german) { return _wikiText != null ? _wikiText(file, key, german) : null; }

        // ---------- settings ----------

        private readonly Dictionary<string, string> _settings = new Dictionary<string, string>();

        /// <summary>
        /// API 2. Settings file in your mod's world storage folder (the folder ReadFileInWorldStorage uses
        /// for every type of your mod, so files written before stay valid).
        /// Server: reads the file; if it is missing or empty, uses defaultText, and writes it when the file is
        /// missing and writeDefault is true. onText runs at once, then Syntrix hands the text to every player.
        /// Player: onText runs when the server's text arrives (after joining and after every SettingsSave).
        /// The text is yours to parse (XML, ini ...). Keep it below 50,000 characters.
        /// </summary>
        /// <param name="file">File name in world storage.</param>
        /// <param name="defaultText">Text used when the file is missing or empty.</param>
        /// <param name="writeDefault">Write defaultText when the file is missing.</param>
        /// <param name="onText">Receives the settings text.</param>
        public void SettingsLoad(string file, string defaultText, bool writeDefault, Action<string> onText)
        {
            if (string.IsNullOrEmpty(file) || onText == null)
                return;
            if (MyAPIGateway.Session.IsServer)
            {
                bool exists = SyntrixFiles.ExistsWorld(file);
                string text = exists ? SyntrixFiles.ReadWorld(file) : null;
                if (string.IsNullOrWhiteSpace(text))
                {
                    text = defaultText ?? "";
                    // Only a missing file gets the default; an empty or unreadable one is left for the admin.
                    if (writeDefault && !exists && defaultText != null)
                        SyntrixFiles.WriteWorld(file, defaultText);
                }
                _settings[file] = text;
                try { onText(text); }
                catch (Exception e) { MyLog.Default.WriteLineAndConsole(ModName + ": ERROR in settings '" + file + "': " + e); }
                WhenReady(() => { string t; if (_setPublish != null && _settings.TryGetValue(file, out t)) _setPublish(file, t); });
            }
            else
            {
                WhenReady(() => { if (_setSubscribe != null) _setSubscribe(file, onText); });
            }
        }

        /// <summary>API 2. Server only: writes the file and hands the new text to every player (not to onText on the server).</summary>
        /// <param name="file">File name in world storage.</param>
        /// <param name="text">New settings text.</param>
        public void SettingsSave(string file, string text)
        {
            if (string.IsNullOrEmpty(file) || !MyAPIGateway.Session.IsServer)
                return;
            text = text ?? "";
            SyntrixFiles.WriteWorld(file, text);
            _settings[file] = text;
            if (_setPublish != null)
                _setPublish(file, text);
        }

        /// <summary>API 2. Server: last text loaded or saved for this file, null if none.</summary>
        /// <param name="file">File name in world storage.</param>
        public string SettingsText(string file)
        {
            string t;
            return file != null && _settings.TryGetValue(file, out t) ? t : null;
        }
    }

    /// <summary>
    /// API 2. Text files in your mod's storage folders. The folder depends on your mod and its script
    /// folder, not on the type, so files written earlier with your own types are found.
    /// Never throws; reading a missing file gives null.
    /// </summary>
    public static class SyntrixFiles
    {
        /// <summary>Reads a file of the world storage; null if missing or unreadable.</summary>
        /// <param name="file">File name.</param>
        public static string ReadWorld(string file)
        {
            try
            {
                if (!MyAPIGateway.Utilities.FileExistsInWorldStorage(file, typeof(SyntrixFiles)))
                    return null;
                using (var r = MyAPIGateway.Utilities.ReadFileInWorldStorage(file, typeof(SyntrixFiles)))
                    return r.ReadToEnd();
            }
            catch (Exception e) { MyLog.Default.WriteLineAndConsole("Syntrix files: cannot read world file " + file + ": " + e.Message); return null; }
        }

        /// <summary>Writes a file of the world storage; false on failure.</summary>
        /// <param name="file">File name.</param>
        /// <param name="text">Content.</param>
        public static bool WriteWorld(string file, string text)
        {
            try
            {
                using (var w = MyAPIGateway.Utilities.WriteFileInWorldStorage(file, typeof(SyntrixFiles)))
                    w.Write(text ?? "");
                return true;
            }
            catch (Exception e) { MyLog.Default.WriteLineAndConsole("Syntrix files: cannot write world file " + file + ": " + e.Message); return false; }
        }

        /// <summary>True if the file exists in the world storage.</summary>
        /// <param name="file">File name.</param>
        public static bool ExistsWorld(string file)
        {
            try { return MyAPIGateway.Utilities.FileExistsInWorldStorage(file, typeof(SyntrixFiles)); }
            catch (Exception) { return false; }
        }

        /// <summary>Deletes a file of the world storage if it exists.</summary>
        /// <param name="file">File name.</param>
        public static void DeleteWorld(string file)
        {
            try
            {
                if (MyAPIGateway.Utilities.FileExistsInWorldStorage(file, typeof(SyntrixFiles)))
                    MyAPIGateway.Utilities.DeleteFileInWorldStorage(file, typeof(SyntrixFiles));
            }
            catch (Exception e) { MyLog.Default.WriteLineAndConsole("Syntrix files: cannot delete world file " + file + ": " + e.Message); }
        }

        /// <summary>Reads a file of the local storage of this computer (player preferences, logs), not part of the world.</summary>
        /// <param name="file">File name.</param>
        public static string ReadLocal(string file)
        {
            try
            {
                if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(file, typeof(SyntrixFiles)))
                    return null;
                using (var r = MyAPIGateway.Utilities.ReadFileInLocalStorage(file, typeof(SyntrixFiles)))
                    return r.ReadToEnd();
            }
            catch (Exception e) { MyLog.Default.WriteLineAndConsole("Syntrix files: cannot read local file " + file + ": " + e.Message); return null; }
        }

        /// <summary>Writes a file of the local storage; false on failure.</summary>
        /// <param name="file">File name.</param>
        /// <param name="text">Content.</param>
        public static bool WriteLocal(string file, string text)
        {
            try
            {
                using (var w = MyAPIGateway.Utilities.WriteFileInLocalStorage(file, typeof(SyntrixFiles)))
                    w.Write(text ?? "");
                return true;
            }
            catch (Exception e) { MyLog.Default.WriteLineAndConsole("Syntrix files: cannot write local file " + file + ": " + e.Message); return false; }
        }
    }

    /// <summary>Text lookup in MyTexts with fallback.</summary>
    public static class SyntrixText
    {
        /// <summary>Translated text of a key; fallback (or the key) if it does not exist.</summary>
        /// <param name="key">Text key.</param>
        /// <param name="fallback">Text when the key is missing.</param>
        public static string Get(string key, string fallback = null)
        {
            if (!string.IsNullOrEmpty(key))
            {
                var id = MyStringId.GetOrCompute(key);
                if (MyTexts.Exists(id))
                    return MyTexts.GetString(id);
            }
            return fallback ?? key ?? "";
        }

        /// <summary>Translated text with {0}, {1} ... filled in; a broken format gives the unformatted text.</summary>
        /// <param name="key">Text key.</param>
        /// <param name="fallback">Text when the key is missing.</param>
        /// <param name="args">Format arguments.</param>
        public static string Format(string key, string fallback, params object[] args)
        {
            string text = Get(key, fallback);
            if (args == null || args.Length == 0)
                return text;
            try { return string.Format(text, args); }
            catch (FormatException) { return text; }
        }

        /// <summary>String id of a key (for terminal titles and tooltips).</summary>
        /// <param name="key">Text key.</param>
        public static MyStringId Id(string key) { return MyStringId.GetOrCompute(key); }
    }

    /// <summary>
    /// Writes wiki XML for <see cref="SyntrixApi.WikiAddSource"/> (pages) and <see cref="SyntrixApi.WikiAddTables"/>
    /// (one table or list): text is escaped, table cells are joined with "|" (links [[page|text]] stay whole).
    /// Example for a table: <c>new SyntrixWikiXml().Page("_").Table("2|3").Header(de, en).Row(de, en).ToString()</c>.
    /// </summary>
    public class SyntrixWikiXml
    {
        private readonly StringBuilder _sb = new StringBuilder("<SyxWiki>");
        private bool _page, _table, _list;

        /// <summary>Starts a page (ends the open one). For WikiAddTables use the id "_".</summary>
        /// <param name="id">Page id.</param>
        /// <param name="parent">Parent page id, null = top.</param>
        /// <param name="order">Order among the siblings.</param>
        /// <param name="titleDe">German title.</param>
        /// <param name="titleEn">English title.</param>
        /// <param name="source">Mod name shown as the source, null = the mod that returned the XML.</param>
        /// <param name="icons">Icons next to the title (material names or definition ids, separated by ';').</param>
        public SyntrixWikiXml Page(string id, string parent = null, int order = 0, string titleDe = null, string titleEn = null, string source = null, string icons = null)
        {
            EndPage();
            _sb.Append("<Page").Append(Attr("Id", id)).Append(Attr("Parent", parent)).Append(Attr("Order", order.ToString()))
               .Append(Attr("Source", source)).Append(Attr("Icons", icons)).Append('>');
            if (titleDe != null || titleEn != null)
                _sb.Append("<Title").Append(Attr("de", titleDe)).Append(Attr("en", titleEn)).Append(" />");
            _page = true;
            return this;
        }

        /// <summary>Heading on the open page.</summary>
        public SyntrixWikiXml Heading(string de, string en) { EndBlock(); _sb.Append("<Heading").Append(Attr("de", de)).Append(Attr("en", en)).Append(" />"); return this; }

        /// <summary>Text on the open page (blank lines separate paragraphs, "- " starts a bullet).</summary>
        public SyntrixWikiXml Text(string de, string en) { return Long("Text", de, en); }

        /// <summary>Note box on the open page.</summary>
        public SyntrixWikiXml Note(string de, string en) { return Long("Note", de, en); }

        /// <summary>Starts a table on the open page.</summary>
        /// <param name="widths">Relative column widths "2|1|3", null = equal.</param>
        public SyntrixWikiXml Table(string widths = null) { EndBlock(); _sb.Append("<Table").Append(Attr("Widths", widths)).Append('>'); _table = true; return this; }

        /// <summary>Header of the open table.</summary>
        public SyntrixWikiXml Header(string[] de, string[] en) { _sb.Append("<Header").Append(Attr("de", Cells(de))).Append(Attr("en", Cells(en))).Append(" />"); return this; }

        /// <summary>Row of the open table.</summary>
        public SyntrixWikiXml Row(string[] de, string[] en) { _sb.Append("<Row").Append(Attr("de", Cells(de))).Append(Attr("en", Cells(en))).Append(" />"); return this; }

        /// <summary>Starts a link list on the open page.</summary>
        /// <param name="columns">Columns (1 to 6), 0 = default.</param>
        public SyntrixWikiXml List(int columns = 0) { EndBlock(); _sb.Append("<List").Append(columns > 0 ? Attr("Columns", columns.ToString()) : "").Append('>'); _list = true; return this; }

        /// <summary>Entry of the open list.</summary>
        /// <param name="page">Page a click opens, null = none.</param>
        /// <param name="de">German text.</param>
        /// <param name="en">English text.</param>
        /// <param name="extraDe">Second German text in quiet colour, null = none.</param>
        /// <param name="extraEn">Second English text, null = none.</param>
        /// <param name="icons">Icons (material names or definition ids such as "MyObjectBuilder_Ore/Iron", ';'), null = none.</param>
        /// <param name="block">Block "Type/Subtype" whose icon is shown when icons is null.</param>
        public SyntrixWikiXml Entry(string page, string de, string en, string extraDe = null, string extraEn = null, string icons = null, string block = null)
        {
            _sb.Append("<Entry").Append(Attr("Page", page)).Append(Attr("Block", block)).Append(Attr("Icons", icons))
               .Append(Attr("de", de)).Append(Attr("en", en)).Append(Attr("ExtraDe", extraDe)).Append(Attr("ExtraEn", extraEn)).Append(" />");
            return this;
        }

        /// <summary>Problem for the wiki error page.</summary>
        public SyntrixWikiXml Error(string text) { EndPage(); _sb.Append("<Error>").Append(Escape(text)).Append("</Error>"); return this; }

        /// <summary>The finished &lt;SyxWiki&gt; text.</summary>
        public override string ToString()
        {
            EndPage();
            return _sb.ToString() + "</SyxWiki>";
        }

        private SyntrixWikiXml Long(string element, string de, string en)
        {
            EndBlock();
            _sb.Append('<').Append(element).Append('>');
            if (!string.IsNullOrEmpty(de))
                _sb.Append("<de>").Append(Escape(de)).Append("</de>");
            if (!string.IsNullOrEmpty(en))
                _sb.Append("<en>").Append(Escape(en)).Append("</en>");
            _sb.Append("</").Append(element).Append('>');
            return this;
        }

        private void EndBlock()
        {
            if (_table)
                _sb.Append("</Table>");
            if (_list)
                _sb.Append("</List>");
            _table = _list = false;
        }

        private void EndPage()
        {
            EndBlock();
            if (_page)
                _sb.Append("</Page>");
            _page = false;
        }

        private static string Cells(string[] cells)
        {
            if (cells == null)
                return null;
            var parts = new string[cells.Length];
            for (int i = 0; i < cells.Length; i++)
                parts[i] = string.IsNullOrEmpty(cells[i]) ? " " : cells[i].Replace("\r", "").Replace("\n", " ");
            return string.Join("|", parts);
        }

        private static string Attr(string name, string value)
        {
            return value == null ? "" : " " + name + "=\"" + Escape(value) + "\"";
        }

        /// <summary>Text for XML (&amp;, &lt;, &gt;, quotes).</summary>
        public static string Escape(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");
        }
    }

    /// <summary>Log lines with the mod name in front; optionally also into an own file in local storage.</summary>
    public class SyntrixLog
    {
        private readonly string _prefix;
        private readonly string _file;
        private TextWriter _writer;
        private bool _fileFailed;

        /// <summary>When true, <see cref="Dbg"/> writes its lines. A field for compatibility with API 1.</summary>
        public bool Debug;

        /// <summary>Log into the game log only.</summary>
        /// <param name="modName">Prefix of every line.</param>
        public SyntrixLog(string modName) { _prefix = modName + ": "; }

        /// <summary>API 2. Also writes every line (with time) to fileName in the mod's local storage.</summary>
        /// <param name="modName">Prefix of every line.</param>
        /// <param name="fileName">File name in local storage.</param>
        public SyntrixLog(string modName, string fileName) : this(modName) { _file = fileName; }

        /// <summary>Information line.</summary>
        /// <param name="text">Line text.</param>
        public void Info(string text) { Write(_prefix + text); }

        /// <summary>Warning line.</summary>
        /// <param name="text">Line text.</param>
        public void Warn(string text) { Write(_prefix + "WARNING " + text); }

        /// <summary>Error line with the exception.</summary>
        /// <param name="where">Where it happened.</param>
        /// <param name="e">The exception.</param>
        public void Error(string where, Exception e) { Write(_prefix + "ERROR in " + where + ": " + e); }

        /// <summary>Debug line, only when <see cref="Debug"/> is true.</summary>
        /// <param name="text">Line text.</param>
        public void Dbg(string text) { if (Debug) Write(_prefix + "debug " + text); }

        private void Write(string line)
        {
            MyLog.Default.WriteLineAndConsole(line);
            if (_file == null || _fileFailed)
                return;
            try
            {
                if (_writer == null)
                    _writer = MyAPIGateway.Utilities.WriteFileInLocalStorage(_file, typeof(SyntrixLog));
                _writer.WriteLine(DateTime.Now.ToString("HH:mm:ss.fff") + " " + line);
                _writer.Flush();
            }
            catch (Exception) { _fileFailed = true; }
        }

        /// <summary>API 2. Call in UnloadData when a log file is used.</summary>
        public void Close()
        {
            if (_writer == null)
                return;
            try { _writer.Flush(); _writer.Close(); }
            catch (Exception) { }
            _writer = null;
        }
    }

    /// <summary>API 2. Scale of a terminal slider.</summary>
    public enum SyntrixSliderScale
    {
        /// <summary>Even steps between minimum and maximum.</summary>
        Linear = 0,

        /// <summary>Logarithmic; minimum and maximum must be above zero.</summary>
        Logarithmic = 1,

        /// <summary>Logarithmic on both sides of zero, from -maximum to maximum; center is the linear part around zero.</summary>
        DualLogarithmic = 2,
    }

    /// <summary>
    /// Terminal controls, terminal properties and toolbar actions for one block type, created lazily: the
    /// first time a player opens the terminal or toolbar of such a block, or when your game logic calls
    /// EnsureCreated (needed on dedicated servers for toolbar actions on timers and buttons, and for
    /// terminal properties used by programmable blocks there).
    /// Adding controls in LoadData breaks the vanilla controls of the type.
    /// </summary>
    /// <typeparam name="TBlock">Block interface the controls belong to, e.g. IMyDoor.</typeparam>
    public static class SyntrixControls<TBlock> where TBlock : class, IMyTerminalBlock
    {
        private static readonly List<Func<IMyTerminalControl>> _controlFactories = new List<Func<IMyTerminalControl>>();
        private static readonly List<Func<IMyTerminalAction>> _actionFactories = new List<Func<IMyTerminalAction>>();
        private static readonly List<Func<IMyTerminalControl>> _propertyFactories = new List<Func<IMyTerminalControl>>();
        private static readonly List<IMyTerminalControl> _controls = new List<IMyTerminalControl>();
        private static readonly List<IMyTerminalAction> _actions = new List<IMyTerminalAction>();
        private static bool _hooked, _created;
        private static Func<IMyTerminalBlock, bool> _listControlsFor, _listActionsFor;

        /// <summary>Any control; the factory runs once, when the controls are created.</summary>
        /// <param name="factory">Makes the control (CreateControl and its settings).</param>
        public static void AddControl(Func<IMyTerminalControl> factory) { _controlFactories.Add(factory); Hook(); }

        /// <summary>Any toolbar action; the factory runs once, when the controls are created.</summary>
        /// <param name="factory">Makes the action (CreateAction and its settings).</param>
        public static void AddAction(Func<IMyTerminalAction> factory) { _actionFactories.Add(factory); Hook(); }

        /// <summary>
        /// API 2. Any terminal property (CreateProperty) for programmable blocks (GetValue / SetValue).
        /// Properties are always registered for TBlock, also in list mode, and never shown in the terminal.
        /// </summary>
        /// <param name="factory">Makes the property.</param>
        public static void AddProperty(Func<IMyTerminalControl> factory) { _propertyFactories.Add(factory); Hook(); }

        /// <summary>
        /// API 2. List mode: the controls and actions are not registered for TBlock with AddControl/AddAction.
        /// Instead they are put into the terminal list (and action list) of every block for which controlsFor
        /// (actionsFor) returns true, each time it is shown. Use it with a broad TBlock (e.g. IMyFunctionalBlock)
        /// to give one set of controls to several block types; each control's visible check picks the blocks.
        /// Call it before the first AddControl. Not on dedicated servers (no terminal there).
        /// </summary>
        /// <param name="controlsFor">Blocks that show the controls; null = none.</param>
        /// <param name="actionsFor">Blocks that offer the actions; null = none.</param>
        public static void ListMode(Func<IMyTerminalBlock, bool> controlsFor, Func<IMyTerminalBlock, bool> actionsFor)
        {
            _listControlsFor = controlsFor;
            _listActionsFor = actionsFor;
        }

        private static bool InList { get { return _listControlsFor != null || _listActionsFor != null; } }

        /// <summary>API 2. True once the controls of this type exist.</summary>
        public static bool Created { get { return _created; } }

        private static void Hook()
        {
            if (_hooked || MyAPIGateway.Utilities.IsDedicated)
                return;
            _hooked = true;
            MyAPIGateway.TerminalControls.CustomControlGetter += ControlGetter;
            MyAPIGateway.TerminalControls.CustomActionGetter += ActionGetter;
        }

        /// <summary>Call in UnloadData of your session component.</summary>
        public static void Unload()
        {
            if (_hooked)
            {
                MyAPIGateway.TerminalControls.CustomControlGetter -= ControlGetter;
                MyAPIGateway.TerminalControls.CustomActionGetter -= ActionGetter;
            }
            _hooked = false;
            _created = false;
            _listControlsFor = null;
            _listActionsFor = null;
            _controlFactories.Clear();
            _actionFactories.Clear();
            _propertyFactories.Clear();
            _controls.Clear();
            _actions.Clear();
        }

        /// <summary>Creates the controls, properties and actions now (once). Call it on dedicated servers.</summary>
        public static void EnsureCreated()
        {
            if (_created)
                return;
            _created = true;
            foreach (var f in _controlFactories)
            {
                var c = f();
                if (c == null) continue;
                if (!InList)
                    MyAPIGateway.TerminalControls.AddControl<TBlock>(c);
                _controls.Add(c);
            }
            foreach (var f in _propertyFactories)
            {
                var c = f();
                if (c != null)
                    MyAPIGateway.TerminalControls.AddControl<TBlock>(c);
            }
            foreach (var f in _actionFactories)
            {
                var a = f();
                if (a == null) continue;
                if (!InList)
                    MyAPIGateway.TerminalControls.AddAction<TBlock>(a);
                _actions.Add(a);
            }
        }

        /// <summary>API 2. Redraws the controls of this type (after values changed elsewhere).</summary>
        public static void Refresh()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;
            foreach (var c in _controls)
                c.UpdateVisual();
        }

        /// <summary>1.3.0. The block whose terminal was shown last (for lists whose entries depend on the block).</summary>
        public static IMyTerminalBlock CurrentBlock { get; private set; }

        private static void ControlGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            CurrentBlock = block;
            if (InList)
            {
                if (_listControlsFor != null && block is TBlock && _listControlsFor(block))
                {
                    EnsureCreated();
                    controls.AddRange(_controls);
                }
                return;
            }
            if (_created || !(block is TBlock))
                return;
            EnsureCreated();
            // AddControl takes effect from the next opening; show them right away as well.
            controls.AddRange(_controls);
        }

        private static void ActionGetter(IMyTerminalBlock block, List<IMyTerminalAction> actions)
        {
            if (InList)
            {
                if (_listActionsFor != null && block is TBlock && _listActionsFor(block))
                {
                    EnsureCreated();
                    actions.AddRange(_actions);
                }
                return;
            }
            if (_created || !(block is TBlock))
                return;
            EnsureCreated();
            actions.AddRange(_actions);
        }

        private static Func<IMyTerminalBlock, bool> Vis(Func<IMyTerminalBlock, bool> visible)
        {
            return b => b is TBlock && (visible == null || visible(b));
        }

        private static void Titles(IMyTerminalControlTitleTooltip c, string titleKey, string tooltipKey)
        {
            c.Title = MyStringId.GetOrCompute(titleKey);
            c.Tooltip = MyStringId.GetOrCompute(tooltipKey ?? titleKey);
        }

        private static void Common(IMyTerminalControl c, Func<IMyTerminalBlock, bool> visible, Func<IMyTerminalBlock, bool> enabled)
        {
            c.Visible = Vis(visible);
            if (enabled != null) c.Enabled = enabled;
        }

        // ---------- ready-made controls bound to Syntrix block settings ----------

        /// <summary>Check box bound to a bool block setting.</summary>
        /// <param name="api">Your docked SyntrixApi.</param>
        /// <param name="id">Unique control id, e.g. "MyMod_Auto".</param>
        /// <param name="titleKey">Text key of the title.</param>
        /// <param name="tooltipKey">Text key of the tooltip; null = title.</param>
        /// <param name="modKey">Your mod name.</param>
        /// <param name="key">Setting name.</param>
        /// <param name="def">Value when not set.</param>
        /// <param name="visible">Visible check; null = always.</param>
        /// <param name="enabled">Enabled check; null = always.</param>
        public static void Checkbox(SyntrixApi api, string id, string titleKey, string tooltipKey, string modKey, string key, bool def,
            Func<IMyTerminalBlock, bool> visible = null, Func<IMyTerminalBlock, bool> enabled = null)
        {
            Checkbox(id, titleKey, tooltipKey, b => api.BlockGetBool(b.EntityId, modKey, key, def),
                (b, v) => api.BlockSetBool(b.EntityId, modKey, key, v), visible, enabled);
        }

        /// <summary>Slider bound to a float block setting; the value is clamped to min..max.</summary>
        /// <param name="api">Your docked SyntrixApi.</param>
        /// <param name="id">Unique control id.</param>
        /// <param name="titleKey">Text key of the title.</param>
        /// <param name="tooltipKey">Text key of the tooltip; null = title.</param>
        /// <param name="modKey">Your mod name.</param>
        /// <param name="key">Setting name.</param>
        /// <param name="min">Smallest value.</param>
        /// <param name="max">Largest value.</param>
        /// <param name="def">Value when not set.</param>
        /// <param name="unit">Text after the value, e.g. " m".</param>
        /// <param name="visible">Visible check; null = always.</param>
        public static void Slider(SyntrixApi api, string id, string titleKey, string tooltipKey, string modKey, string key,
            float min, float max, float def, string unit = "", Func<IMyTerminalBlock, bool> visible = null)
        {
            Slider(id, titleKey, tooltipKey, min, max,
                b => api.BlockGetFloat(b.EntityId, modKey, key, def),
                (b, v) => api.BlockSetFloat(b.EntityId, modKey, key, Math.Max(min, Math.Min(max, v))),
                (b, sb) => sb.Append(Math.Round(api.BlockGetFloat(b.EntityId, modKey, key, def), 1)).Append(unit), visible);
        }

        /// <summary>API 2. Slider bound to a float block setting with a scale and an enabled check.</summary>
        /// <param name="api">Your docked SyntrixApi.</param>
        /// <param name="id">Unique control id.</param>
        /// <param name="titleKey">Text key of the title.</param>
        /// <param name="tooltipKey">Text key of the tooltip; null = title.</param>
        /// <param name="modKey">Your mod name.</param>
        /// <param name="key">Setting name.</param>
        /// <param name="min">Smallest value (DualLogarithmic: size of the linear part around zero).</param>
        /// <param name="max">Largest value.</param>
        /// <param name="def">Value when not set.</param>
        /// <param name="scale">Linear, logarithmic or dual logarithmic.</param>
        /// <param name="unit">Text after the value.</param>
        /// <param name="visible">Visible check; null = always.</param>
        /// <param name="enabled">Enabled check; null = always.</param>
        public static void Slider(SyntrixApi api, string id, string titleKey, string tooltipKey, string modKey, string key,
            float min, float max, float def, SyntrixSliderScale scale, string unit = "", Func<IMyTerminalBlock, bool> visible = null,
            Func<IMyTerminalBlock, bool> enabled = null)
        {
            float low = scale == SyntrixSliderScale.DualLogarithmic ? -max : min;
            Slider(id, titleKey, tooltipKey, b => min, b => max, scale, Math.Max(0f, min),
                b => api.BlockGetFloat(b.EntityId, modKey, key, def),
                (b, v) => api.BlockSetFloat(b.EntityId, modKey, key, Math.Max(low, Math.Min(max, v))),
                (b, sb) => sb.Append(Math.Round(api.BlockGetFloat(b.EntityId, modKey, key, def), 1)).Append(unit), visible, enabled);
        }

        /// <summary>Text box bound to a text block setting (trimmed).</summary>
        /// <param name="api">Your docked SyntrixApi.</param>
        /// <param name="id">Unique control id.</param>
        /// <param name="titleKey">Text key of the title.</param>
        /// <param name="tooltipKey">Text key of the tooltip; null = title.</param>
        /// <param name="modKey">Your mod name.</param>
        /// <param name="key">Setting name.</param>
        /// <param name="visible">Visible check; null = always.</param>
        public static void TextBox(SyntrixApi api, string id, string titleKey, string tooltipKey, string modKey, string key,
            Func<IMyTerminalBlock, bool> visible = null)
        {
            TextBox(id, titleKey, tooltipKey, b => api.BlockGet(b.EntityId, modKey, key) ?? "",
                (b, s) => api.BlockSet(b.EntityId, modKey, key, s.Trim()), visible);
        }

        /// <summary>API 2. Drop-down list bound to a block setting; items are (stored value, text key).</summary>
        /// <param name="api">Your docked SyntrixApi.</param>
        /// <param name="id">Unique control id.</param>
        /// <param name="titleKey">Text key of the title.</param>
        /// <param name="tooltipKey">Text key of the tooltip; null = title.</param>
        /// <param name="modKey">Your mod name.</param>
        /// <param name="key">Setting name.</param>
        /// <param name="items">Entries: stored value and text key.</param>
        /// <param name="def">Stored value when not set.</param>
        /// <param name="visible">Visible check; null = always.</param>
        public static void Combo(SyntrixApi api, string id, string titleKey, string tooltipKey, string modKey, string key,
            IList<MyTuple<string, string>> items, string def, Func<IMyTerminalBlock, bool> visible = null)
        {
            Combo(id, titleKey, tooltipKey, items, b => api.BlockGet(b.EntityId, modKey, key) ?? def,
                (b, v) => api.BlockSet(b.EntityId, modKey, key, v), visible);
        }

        /// <summary>API 2. On/off switch bound to a bool block setting.</summary>
        /// <param name="api">Your docked SyntrixApi.</param>
        /// <param name="id">Unique control id.</param>
        /// <param name="titleKey">Text key of the title.</param>
        /// <param name="tooltipKey">Text key of the tooltip; null = title.</param>
        /// <param name="modKey">Your mod name.</param>
        /// <param name="key">Setting name.</param>
        /// <param name="def">Value when not set.</param>
        /// <param name="visible">Visible check; null = always.</param>
        /// <param name="enabled">Enabled check; null = always.</param>
        public static void OnOff(SyntrixApi api, string id, string titleKey, string tooltipKey, string modKey, string key, bool def,
            Func<IMyTerminalBlock, bool> visible = null, Func<IMyTerminalBlock, bool> enabled = null)
        {
            OnOff(id, titleKey, tooltipKey, b => api.BlockGetBool(b.EntityId, modKey, key, def),
                (b, v) => api.BlockSetBool(b.EntityId, modKey, key, v), visible, enabled);
        }

        /// <summary>API 2. Colour picker bound to a block setting (stored as packed RGBA number).</summary>
        /// <param name="api">Your docked SyntrixApi.</param>
        /// <param name="id">Unique control id.</param>
        /// <param name="titleKey">Text key of the title.</param>
        /// <param name="tooltipKey">Text key of the tooltip; null = title.</param>
        /// <param name="modKey">Your mod name.</param>
        /// <param name="key">Setting name.</param>
        /// <param name="def">Colour when not set.</param>
        /// <param name="visible">Visible check; null = always.</param>
        /// <param name="enabled">Enabled check; null = always.</param>
        public static void ColorPicker(SyntrixApi api, string id, string titleKey, string tooltipKey, string modKey, string key,
            VRageMath.Color def, Func<IMyTerminalBlock, bool> visible = null, Func<IMyTerminalBlock, bool> enabled = null)
        {
            ColorPicker(id, titleKey, tooltipKey, b => ParseColor(api.BlockGet(b.EntityId, modKey, key), def),
                (b, v) => api.BlockSet(b.EntityId, modKey, key, v.PackedValue.ToString()), visible, enabled);
        }

        /// <summary>
        /// API 2. List box bound to a block setting. The selected stored values are kept as one text,
        /// separated by line breaks (one value without multiselect).
        /// </summary>
        /// <param name="api">Your docked SyntrixApi.</param>
        /// <param name="id">Unique control id.</param>
        /// <param name="titleKey">Text key of the title.</param>
        /// <param name="tooltipKey">Text key of the tooltip; null = title.</param>
        /// <param name="modKey">Your mod name.</param>
        /// <param name="key">Setting name.</param>
        /// <param name="multiselect">Allow several selected entries.</param>
        /// <param name="visibleRows">Shown rows.</param>
        /// <param name="items">Entries: stored value and text key.</param>
        /// <param name="visible">Visible check; null = always.</param>
        /// <param name="enabled">Enabled check; null = always.</param>
        public static void Listbox(SyntrixApi api, string id, string titleKey, string tooltipKey, string modKey, string key,
            bool multiselect, int visibleRows, IList<MyTuple<string, string>> items, Func<IMyTerminalBlock, bool> visible = null,
            Func<IMyTerminalBlock, bool> enabled = null)
        {
            Listbox(id, titleKey, tooltipKey, multiselect, visibleRows, b => items,
                b => { var v = api.BlockGet(b.EntityId, modKey, key); return string.IsNullOrEmpty(v) ? new string[0] : v.Split('\n'); },
                (b, v) => api.BlockSet(b.EntityId, modKey, key, v == null || v.Length == 0 ? null : string.Join("\n", v)), visible, enabled);
        }

        /// <summary>API 2. Terminal property (bool) bound to a block setting, for programmable blocks.</summary>
        /// <param name="api">Your docked SyntrixApi.</param>
        /// <param name="id">Property id used by scripts (GetValueBool / SetValueBool).</param>
        /// <param name="modKey">Your mod name.</param>
        /// <param name="key">Setting name.</param>
        /// <param name="def">Value when not set.</param>
        public static void PropertyBool(SyntrixApi api, string id, string modKey, string key, bool def)
        {
            Property<bool>(id, b => api.BlockGetBool(b.EntityId, modKey, key, def), (b, v) => api.BlockSetBool(b.EntityId, modKey, key, v));
        }

        /// <summary>API 2. Terminal property (float) bound to a block setting, for programmable blocks.</summary>
        /// <param name="api">Your docked SyntrixApi.</param>
        /// <param name="id">Property id used by scripts (GetValueFloat / SetValueFloat).</param>
        /// <param name="modKey">Your mod name.</param>
        /// <param name="key">Setting name.</param>
        /// <param name="def">Value when not set.</param>
        public static void PropertyFloat(SyntrixApi api, string id, string modKey, string key, float def)
        {
            Property<float>(id, b => api.BlockGetFloat(b.EntityId, modKey, key, def), (b, v) => api.BlockSetFloat(b.EntityId, modKey, key, v));
        }

        /// <summary>API 2. Terminal property (text) bound to a block setting, for programmable blocks.</summary>
        /// <param name="api">Your docked SyntrixApi.</param>
        /// <param name="id">Property id used by scripts (GetValue&lt;StringBuilder&gt;).</param>
        /// <param name="modKey">Your mod name.</param>
        /// <param name="key">Setting name.</param>
        public static void PropertyText(SyntrixApi api, string id, string modKey, string key)
        {
            Property<StringBuilder>(id, b => new StringBuilder(api.BlockGet(b.EntityId, modKey, key) ?? ""),
                (b, v) => api.BlockSet(b.EntityId, modKey, key, v != null ? v.ToString() : null));
        }

        /// <summary>API 2. Terminal property (colour) bound to a block setting, for programmable blocks.</summary>
        /// <param name="api">Your docked SyntrixApi.</param>
        /// <param name="id">Property id used by scripts (GetValueColor / SetValueColor).</param>
        /// <param name="modKey">Your mod name.</param>
        /// <param name="key">Setting name.</param>
        /// <param name="def">Colour when not set.</param>
        public static void PropertyColor(SyntrixApi api, string id, string modKey, string key, VRageMath.Color def)
        {
            Property<VRageMath.Color>(id, b => ParseColor(api.BlockGet(b.EntityId, modKey, key), def),
                (b, v) => api.BlockSet(b.EntityId, modKey, key, v.PackedValue.ToString()));
        }

        private static VRageMath.Color ParseColor(string text, VRageMath.Color def)
        {
            uint packed;
            if (text == null || !uint.TryParse(text, out packed))
                return def;
            var c = new VRageMath.Color();
            c.PackedValue = packed;
            return c;
        }

        // ---------- API 2: the same controls with your own getter and setter ----------

        /// <summary>Check box with your own getter and setter.</summary>
        /// <param name="id">Unique control id.</param>
        /// <param name="titleKey">Text key of the title.</param>
        /// <param name="tooltipKey">Text key of the tooltip; null = title.</param>
        /// <param name="getter">Current value of a block.</param>
        /// <param name="setter">Stores a new value.</param>
        /// <param name="visible">Visible check; null = always.</param>
        /// <param name="enabled">Enabled check; null = always.</param>
        public static void Checkbox(string id, string titleKey, string tooltipKey, Func<IMyTerminalBlock, bool> getter,
            Action<IMyTerminalBlock, bool> setter, Func<IMyTerminalBlock, bool> visible = null, Func<IMyTerminalBlock, bool> enabled = null)
        {
            AddControl(() =>
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, TBlock>(id);
                Titles(c, titleKey, tooltipKey);
                Common(c, visible, enabled);
                c.Getter = getter;
                c.Setter = setter;
                return c;
            });
        }

        /// <summary>API 2. Check box with own texts for on and off.</summary>
        /// <param name="id">Unique control id.</param>
        /// <param name="titleKey">Text key of the title.</param>
        /// <param name="tooltipKey">Text key of the tooltip; null = title.</param>
        /// <param name="getter">Current value of a block.</param>
        /// <param name="setter">Stores a new value.</param>
        /// <param name="visible">Visible check; null = always.</param>
        /// <param name="enabled">Enabled check; null = always.</param>
        /// <param name="onTextKey">Text key shown for on.</param>
        /// <param name="offTextKey">Text key shown for off.</param>
        public static void Checkbox(string id, string titleKey, string tooltipKey, Func<IMyTerminalBlock, bool> getter,
            Action<IMyTerminalBlock, bool> setter, Func<IMyTerminalBlock, bool> visible, Func<IMyTerminalBlock, bool> enabled,
            string onTextKey, string offTextKey)
        {
            AddControl(() =>
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, TBlock>(id);
                Titles(c, titleKey, tooltipKey);
                Common(c, visible, enabled);
                c.OnText = MyStringId.GetOrCompute(onTextKey);
                c.OffText = MyStringId.GetOrCompute(offTextKey);
                c.Getter = getter;
                c.Setter = setter;
                return c;
            });
        }

        /// <summary>On/off switch with your own getter and setter.</summary>
        /// <param name="id">Unique control id.</param>
        /// <param name="titleKey">Text key of the title.</param>
        /// <param name="tooltipKey">Text key of the tooltip; null = title.</param>
        /// <param name="getter">Current value of a block.</param>
        /// <param name="setter">Stores a new value.</param>
        /// <param name="visible">Visible check; null = always.</param>
        /// <param name="enabled">Enabled check; null = always.</param>
        public static void OnOff(string id, string titleKey, string tooltipKey, Func<IMyTerminalBlock, bool> getter,
            Action<IMyTerminalBlock, bool> setter, Func<IMyTerminalBlock, bool> visible = null, Func<IMyTerminalBlock, bool> enabled = null)
        {
            OnOff(id, titleKey, tooltipKey, getter, setter, visible, enabled, "SwitchText_On", "SwitchText_Off");
        }

        /// <summary>API 2. On/off switch with own texts for both sides.</summary>
        /// <param name="id">Unique control id.</param>
        /// <param name="titleKey">Text key of the title.</param>
        /// <param name="tooltipKey">Text key of the tooltip; null = title.</param>
        /// <param name="getter">Current value of a block.</param>
        /// <param name="setter">Stores a new value.</param>
        /// <param name="visible">Visible check; null = always.</param>
        /// <param name="enabled">Enabled check; null = always.</param>
        /// <param name="onTextKey">Text key of the on side.</param>
        /// <param name="offTextKey">Text key of the off side.</param>
        public static void OnOff(string id, string titleKey, string tooltipKey, Func<IMyTerminalBlock, bool> getter,
            Action<IMyTerminalBlock, bool> setter, Func<IMyTerminalBlock, bool> visible, Func<IMyTerminalBlock, bool> enabled,
            string onTextKey, string offTextKey)
        {
            AddControl(() =>
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, TBlock>(id);
                Titles(c, titleKey, tooltipKey);
                c.OnText = MyStringId.GetOrCompute(onTextKey);
                c.OffText = MyStringId.GetOrCompute(offTextKey);
                Common(c, visible, enabled);
                c.Getter = getter;
                c.Setter = setter;
                return c;
            });
        }

        /// <summary>Linear slider with your own getter, setter and value text.</summary>
        /// <param name="id">Unique control id.</param>
        /// <param name="titleKey">Text key of the title.</param>
        /// <param name="tooltipKey">Text key of the tooltip; null = title.</param>
        /// <param name="min">Smallest value.</param>
        /// <param name="max">Largest value.</param>
        /// <param name="getter">Current value of a block.</param>
        /// <param name="setter">Stores a new value.</param>
        /// <param name="writer">Writes the value text; null = number only.</param>
        /// <param name="visible">Visible check; null = always.</param>
        /// <param name="enabled">Enabled check; null = always.</param>
        public static void Slider(string id, string titleKey, string tooltipKey, float min, float max, Func<IMyTerminalBlock, float> getter,
            Action<IMyTerminalBlock, float> setter, Action<IMyTerminalBlock, StringBuilder> writer,
            Func<IMyTerminalBlock, bool> visible = null, Func<IMyTerminalBlock, bool> enabled = null)
        {
            AddControl(() =>
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, TBlock>(id);
                Titles(c, titleKey, tooltipKey);
                Common(c, visible, enabled);
                c.SetLimits(min, max);
                c.Getter = getter;
                c.Setter = setter;
                if (writer != null) c.Writer = writer;
                return c;
            });
        }

        /// <summary>API 2. Slider with limits per block and a scale.</summary>
        /// <param name="id">Unique control id.</param>
        /// <param name="titleKey">Text key of the title.</param>
        /// <param name="tooltipKey">Text key of the tooltip; null = title.</param>
        /// <param name="min">Smallest value of a block (DualLogarithmic: unused, -max is the low end).</param>
        /// <param name="max">Largest value of a block.</param>
        /// <param name="scale">Linear, logarithmic or dual logarithmic.</param>
        /// <param name="center">DualLogarithmic only: size of the linear part around zero.</param>
        /// <param name="getter">Current value of a block.</param>
        /// <param name="setter">Stores a new value.</param>
        /// <param name="writer">Writes the value text; null = number only.</param>
        /// <param name="visible">Visible check; null = always.</param>
        /// <param name="enabled">Enabled check; null = always.</param>
        public static void Slider(string id, string titleKey, string tooltipKey, Func<IMyTerminalBlock, float> min, Func<IMyTerminalBlock, float> max,
            SyntrixSliderScale scale, float center, Func<IMyTerminalBlock, float> getter, Action<IMyTerminalBlock, float> setter,
            Action<IMyTerminalBlock, StringBuilder> writer, Func<IMyTerminalBlock, bool> visible = null, Func<IMyTerminalBlock, bool> enabled = null)
        {
            AddControl(() =>
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, TBlock>(id);
                Titles(c, titleKey, tooltipKey);
                Common(c, visible, enabled);
                if (scale == SyntrixSliderScale.Logarithmic)
                    c.SetLogLimits(min, max);
                else if (scale == SyntrixSliderScale.DualLogarithmic)
                    c.SetDualLogLimits(b => -max(b), max, center);
                else
                    c.SetLimits(min, max);
                c.Getter = getter;
                c.Setter = setter;
                if (writer != null) c.Writer = writer;
                return c;
            });
        }

        /// <summary>Text box with your own getter and setter.</summary>
        /// <param name="id">Unique control id.</param>
        /// <param name="titleKey">Text key of the title.</param>
        /// <param name="tooltipKey">Text key of the tooltip; null = title.</param>
        /// <param name="getter">Current text of a block.</param>
        /// <param name="setter">Stores a new text.</param>
        /// <param name="visible">Visible check; null = always.</param>
        /// <param name="enabled">Enabled check; null = always.</param>
        public static void TextBox(string id, string titleKey, string tooltipKey, Func<IMyTerminalBlock, string> getter,
            Action<IMyTerminalBlock, string> setter, Func<IMyTerminalBlock, bool> visible = null, Func<IMyTerminalBlock, bool> enabled = null)
        {
            AddControl(() =>
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, TBlock>(id);
                Titles(c, titleKey, tooltipKey);
                Common(c, visible, enabled);
                c.Getter = b => new StringBuilder(getter(b) ?? "");
                c.Setter = (b, sb) => setter(b, sb.ToString());
                return c;
            });
        }

        /// <summary>Drop-down list with your own getter and setter; items are (stored value, text key).</summary>
        /// <param name="id">Unique control id.</param>
        /// <param name="titleKey">Text key of the title.</param>
        /// <param name="tooltipKey">Text key of the tooltip; null = title.</param>
        /// <param name="items">Entries: stored value and text key.</param>
        /// <param name="getter">Current stored value of a block.</param>
        /// <param name="setter">Stores a new value.</param>
        /// <param name="visible">Visible check; null = always.</param>
        /// <param name="enabled">Enabled check; null = always.</param>
        public static void Combo(string id, string titleKey, string tooltipKey, IList<MyTuple<string, string>> items,
            Func<IMyTerminalBlock, string> getter, Action<IMyTerminalBlock, string> setter,
            Func<IMyTerminalBlock, bool> visible = null, Func<IMyTerminalBlock, bool> enabled = null)
        {
            AddControl(() =>
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, TBlock>(id);
                Titles(c, titleKey, tooltipKey);
                Common(c, visible, enabled);
                c.ComboBoxContent = list =>
                {
                    for (int i = 0; i < items.Count; i++)
                        list.Add(new MyTerminalControlComboBoxItem { Key = i, Value = MyStringId.GetOrCompute(items[i].Item2) });
                };
                c.Getter = b =>
                {
                    string v = getter(b);
                    for (int i = 0; i < items.Count; i++)
                        if (items[i].Item1 == v)
                            return i;
                    return 0;
                };
                c.Setter = (b, i) => { if (i >= 0 && i < items.Count) setter(b, items[(int)i].Item1); };
                return c;
            });
        }

        /// <summary>
        /// 1.3.0. Drop-down list whose entries depend on the block (e.g. names found on the same grid). The entries are
        /// asked again each time the list is shown; entries are (stored value, text or text key).
        /// </summary>
        /// <param name="id">Unique control id.</param>
        /// <param name="titleKey">Text key of the title.</param>
        /// <param name="tooltipKey">Text key of the tooltip; null = title.</param>
        /// <param name="items">Entries of a block: stored value and text (or text key).</param>
        /// <param name="getter">Current stored value of a block.</param>
        /// <param name="setter">Stores a new value.</param>
        /// <param name="visible">Visible check; null = always.</param>
        /// <param name="enabled">Enabled check; null = always.</param>
        public static void Combo(string id, string titleKey, string tooltipKey, Func<IMyTerminalBlock, IList<MyTuple<string, string>>> items,
            Func<IMyTerminalBlock, string> getter, Action<IMyTerminalBlock, string> setter,
            Func<IMyTerminalBlock, bool> visible = null, Func<IMyTerminalBlock, bool> enabled = null)
        {
            Func<IMyTerminalBlock, IList<MyTuple<string, string>>> entries = b =>
            {
                IList<MyTuple<string, string>> list = null;
                if (b != null)
                {
                    try { list = items(b); }
                    catch (Exception e) { MyLog.Default.WriteLineAndConsole("Syntrix controls: ERROR in combo entries of " + id + ": " + e); }
                }
                return list ?? new List<MyTuple<string, string>>();
            };
            AddControl(() =>
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, TBlock>(id);
                Titles(c, titleKey, tooltipKey);
                Common(c, visible, enabled);
                c.ComboBoxContent = list =>
                {
                    var current = entries(CurrentBlock);
                    for (int i = 0; i < current.Count; i++)
                        list.Add(new MyTerminalControlComboBoxItem { Key = i, Value = MyStringId.GetOrCompute(current[i].Item2) });
                };
                c.Getter = b =>
                {
                    var current = entries(b);
                    string v = getter(b);
                    for (int i = 0; i < current.Count; i++)
                        if (current[i].Item1 == v)
                            return i;
                    return 0;
                };
                c.Setter = (b, i) =>
                {
                    var current = entries(b);
                    if (i >= 0 && i < current.Count)
                        setter(b, current[(int)i].Item1);
                };
                return c;
            });
        }

        /// <summary>API 2. Colour picker with your own getter and setter.</summary>
        /// <param name="id">Unique control id.</param>
        /// <param name="titleKey">Text key of the title.</param>
        /// <param name="tooltipKey">Text key of the tooltip; null = title.</param>
        /// <param name="getter">Current colour of a block.</param>
        /// <param name="setter">Stores a new colour.</param>
        /// <param name="visible">Visible check; null = always.</param>
        /// <param name="enabled">Enabled check; null = always.</param>
        public static void ColorPicker(string id, string titleKey, string tooltipKey, Func<IMyTerminalBlock, VRageMath.Color> getter,
            Action<IMyTerminalBlock, VRageMath.Color> setter, Func<IMyTerminalBlock, bool> visible = null, Func<IMyTerminalBlock, bool> enabled = null)
        {
            AddControl(() =>
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlColor, TBlock>(id);
                Titles(c, titleKey, tooltipKey);
                Common(c, visible, enabled);
                c.Getter = getter;
                c.Setter = setter;
                return c;
            });
        }

        /// <summary>
        /// API 2. List box with full control over the entries: content(block, items, selected) fills both lists,
        /// selected(block, selection) runs when the player changes the selection.
        /// </summary>
        /// <param name="id">Unique control id.</param>
        /// <param name="titleKey">Text key of the title.</param>
        /// <param name="tooltipKey">Text key of the tooltip; null = title.</param>
        /// <param name="multiselect">Allow several selected entries.</param>
        /// <param name="visibleRows">Shown rows.</param>
        /// <param name="content">Adds all entries and the selected ones.</param>
        /// <param name="selected">Receives the new selection.</param>
        /// <param name="visible">Visible check; null = always.</param>
        /// <param name="enabled">Enabled check; null = always.</param>
        public static void Listbox(string id, string titleKey, string tooltipKey, bool multiselect, int visibleRows,
            Action<IMyTerminalBlock, List<MyTerminalControlListBoxItem>, List<MyTerminalControlListBoxItem>> content,
            Action<IMyTerminalBlock, List<MyTerminalControlListBoxItem>> selected,
            Func<IMyTerminalBlock, bool> visible = null, Func<IMyTerminalBlock, bool> enabled = null)
        {
            AddControl(() =>
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, TBlock>(id);
                Titles(c, titleKey, tooltipKey);
                Common(c, visible, enabled);
                c.Multiselect = multiselect;
                c.VisibleRowsCount = Math.Max(1, visibleRows);
                c.ListContent = content;
                c.ItemSelected = selected;
                return c;
            });
        }

        /// <summary>API 2. List box over (stored value, text key) items with your own getter and setter of the selected values.</summary>
        /// <param name="id">Unique control id.</param>
        /// <param name="titleKey">Text key of the title.</param>
        /// <param name="tooltipKey">Text key of the tooltip; null = title.</param>
        /// <param name="multiselect">Allow several selected entries.</param>
        /// <param name="visibleRows">Shown rows.</param>
        /// <param name="items">Entries of a block: stored value and text key.</param>
        /// <param name="getter">Selected stored values of a block.</param>
        /// <param name="setter">Stores the new selection.</param>
        /// <param name="visible">Visible check; null = always.</param>
        /// <param name="enabled">Enabled check; null = always.</param>
        public static void Listbox(string id, string titleKey, string tooltipKey, bool multiselect, int visibleRows,
            Func<IMyTerminalBlock, IList<MyTuple<string, string>>> items, Func<IMyTerminalBlock, string[]> getter,
            Action<IMyTerminalBlock, string[]> setter, Func<IMyTerminalBlock, bool> visible = null, Func<IMyTerminalBlock, bool> enabled = null)
        {
            Listbox(id, titleKey, tooltipKey, multiselect, visibleRows,
                (b, list, selection) =>
                {
                    var entries = items(b);
                    if (entries == null)
                        return;
                    var chosen = getter(b) ?? new string[0];
                    foreach (var e in entries)
                    {
                        var item = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(e.Item2), MyStringId.GetOrCompute(e.Item2), e.Item1);
                        list.Add(item);
                        if (Array.IndexOf(chosen, e.Item1) >= 0)
                            selection.Add(item);
                    }
                },
                (b, selection) =>
                {
                    var values = new string[selection.Count];
                    for (int i = 0; i < selection.Count; i++)
                        values[i] = selection[i].UserData as string;
                    setter(b, values);
                }, visible, enabled);
        }

        /// <summary>Button without enabled check.</summary>
        /// <param name="id">Unique control id.</param>
        /// <param name="titleKey">Text key of the title.</param>
        /// <param name="tooltipKey">Text key of the tooltip; null = title.</param>
        /// <param name="pressed">Runs when pressed.</param>
        /// <param name="visible">Visible check; null = always.</param>
        public static void Button(string id, string titleKey, string tooltipKey, Action<IMyTerminalBlock> pressed,
            Func<IMyTerminalBlock, bool> visible = null)
        {
            Button(id, titleKey, tooltipKey, pressed, visible, null);
        }

        /// <summary>API 2. Button with an enabled check.</summary>
        /// <param name="id">Unique control id.</param>
        /// <param name="titleKey">Text key of the title.</param>
        /// <param name="tooltipKey">Text key of the tooltip; null = title.</param>
        /// <param name="pressed">Runs when pressed.</param>
        /// <param name="visible">Visible check; null = always.</param>
        /// <param name="enabled">Enabled check; null = always.</param>
        public static void Button(string id, string titleKey, string tooltipKey, Action<IMyTerminalBlock> pressed,
            Func<IMyTerminalBlock, bool> visible, Func<IMyTerminalBlock, bool> enabled)
        {
            AddControl(() =>
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, TBlock>(id);
                Titles(c, titleKey, tooltipKey);
                Common(c, visible, enabled);
                c.Action = pressed;
                return c;
            });
        }

        /// <summary>Text line in the terminal.</summary>
        /// <param name="id">Unique control id.</param>
        /// <param name="titleKey">Text key of the label.</param>
        /// <param name="visible">Visible check; null = always.</param>
        public static void Label(string id, string titleKey, Func<IMyTerminalBlock, bool> visible = null)
        {
            AddControl(() =>
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, TBlock>(id);
                c.Label = MyStringId.GetOrCompute(titleKey);
                c.Visible = Vis(visible);
                return c;
            });
        }

        /// <summary>Separator line in the terminal.</summary>
        /// <param name="id">Unique control id.</param>
        /// <param name="visible">Visible check; null = always.</param>
        public static void Separator(string id, Func<IMyTerminalBlock, bool> visible = null)
        {
            AddControl(() =>
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, TBlock>(id);
                c.Visible = Vis(visible);
                return c;
            });
        }

        /// <summary>
        /// API 2. Terminal property for programmable blocks (block.GetValue&lt;TValue&gt;(id) / SetValue).
        /// Common types: bool, float, VRageMath.Color, StringBuilder, long.
        /// </summary>
        /// <typeparam name="TValue">Value type of the property.</typeparam>
        /// <param name="id">Property id used by scripts.</param>
        /// <param name="getter">Current value of a block.</param>
        /// <param name="setter">Stores a new value; null = read only.</param>
        public static void Property<TValue>(string id, Func<IMyTerminalBlock, TValue> getter, Action<IMyTerminalBlock, TValue> setter)
        {
            AddProperty(() =>
            {
                var p = MyAPIGateway.TerminalControls.CreateProperty<TValue, TBlock>(id);
                p.Getter = getter;
                p.Setter = setter ?? ((b, v) => { });
                return p;
            });
        }

        /// <summary>Toolbar action (buttons, timers, event controllers). Runs where the toolbar is triggered.</summary>
        /// <param name="id">Unique action id.</param>
        /// <param name="nameKey">Text key of the action name.</param>
        /// <param name="icon">Icon path; null = start icon.</param>
        /// <param name="run">Runs the action.</param>
        /// <param name="enabled">Enabled check; null = always.</param>
        public static void Action(string id, string nameKey, string icon, Action<IMyTerminalBlock> run,
            Func<IMyTerminalBlock, bool> enabled = null)
        {
            Action(id, nameKey, icon, run, enabled, null);
        }

        /// <summary>API 2. Toolbar action with a status text on the toolbar slot.</summary>
        /// <param name="id">Unique action id.</param>
        /// <param name="nameKey">Text key of the action name.</param>
        /// <param name="icon">Icon path; null = start icon.</param>
        /// <param name="run">Runs the action.</param>
        /// <param name="enabled">Enabled check; null = always.</param>
        /// <param name="writer">Writes the status text of the toolbar slot; null = none.</param>
        public static void Action(string id, string nameKey, string icon, Action<IMyTerminalBlock> run,
            Func<IMyTerminalBlock, bool> enabled, Action<IMyTerminalBlock, StringBuilder> writer)
        {
            Action(id, nameKey, icon, run, enabled, writer, null, true);
        }

        /// <summary>API 2. Toolbar action with status text, toolbars where it is not offered, and the group setting.</summary>
        /// <param name="id">Unique action id.</param>
        /// <param name="nameKey">Text key of the action name.</param>
        /// <param name="icon">Icon path; null = start icon.</param>
        /// <param name="run">Runs the action.</param>
        /// <param name="enabled">Enabled check; null = always.</param>
        /// <param name="writer">Writes the status text of the toolbar slot; null = none.</param>
        /// <param name="invalidToolbars">Toolbar types that do not offer it (e.g. MyToolbarType.ButtonPanel); null = all offer it.</param>
        /// <param name="validForGroups">Offered for block groups.</param>
        public static void Action(string id, string nameKey, string icon, Action<IMyTerminalBlock> run,
            Func<IMyTerminalBlock, bool> enabled, Action<IMyTerminalBlock, StringBuilder> writer,
            IList<MyToolbarType> invalidToolbars, bool validForGroups)
        {
            AddAction(() =>
            {
                var a = MyAPIGateway.TerminalControls.CreateAction<TBlock>(id);
                a.Name = new StringBuilder(SyntrixText.Get(nameKey, nameKey));
                a.Icon = icon ?? @"Textures\GUI\Icons\Actions\Start.dds";
                a.Action = run;
                a.Enabled = b => b is TBlock && (enabled == null || enabled(b));
                if (writer != null) a.Writer = writer;
                if (invalidToolbars != null) a.InvalidToolbarTypes = new List<MyToolbarType>(invalidToolbars);
                a.ValidForGroups = validForGroups;
                return a;
            });
        }
    }
}
