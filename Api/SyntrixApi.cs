// Syntrix dock file (API version 2, Syntrix 1.1.0). Copy this file into your mod's script folder.
// Do not edit it; take a newer copy from the Syntrix repository instead.
// Windows and HUD elements: also copy SyntrixUi.cs (optional second dock file).
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
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace Syntrix.Api
{
    /// <summary>
    /// Connection to the Syntrix framework. Create one per mod, call Load in LoadData
    /// (or BeforeStart) and Unload in UnloadData.
    /// Registrations (network channels, block change handlers, chat commands, event subscriptions,
    /// settings) may be made right after Load: the dock file keeps them and hands them over
    /// as soon as the framework is ready. Sending only works after onReady fired.
    /// Every method is safe to call without the framework: it then does nothing.
    /// </summary>
    public class SyntrixApi
    {
        public const long Channel = 8346120579001L;
        public const int ApiVersion = 2;

        /// <summary>Prefix for notification arguments that are text keys themselves.</summary>
        public const char TextArgPrefix = '\u0001';

        public bool IsReady { get; private set; }
        public int FrameworkApiVersion { get; private set; }
        public string FrameworkVersion { get; private set; }
        public string ModName { get; private set; }
        /// <summary>Set when the framework refused this dock file (too old).</summary>
        public string Error { get; private set; }

        IReadOnlyDictionary<string, Delegate> _endpoints;
        Action _onReady;
        bool _loaded, _warned;
        readonly List<Action> _pending = new List<Action>();

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
        public bool Has(string key) { return _endpoints != null && _endpoints.ContainsKey(key); }

        void SendRegistration()
        {
            MyAPIGateway.Utilities.SendModMessage(Channel,
                new MyTuple<string, int, string, Action<object>>("syx.register", ApiVersion, ModName, OnReply));
        }

        void OnMessage(object msg)
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

        void OnReply(object msg)
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
        void WhenReady(Action a)
        {
            if (IsReady)
                a();
            else if (_loaded)
                _pending.Add(a);
        }

        void OnSessionReady()
        {
            if (IsReady || _warned)
                return;
            _warned = true;
            string text = Error ?? (ModName + " needs the Syntrix framework mod, which is not loaded. Its scripted features are off.");
            MyLog.Default.WriteLineAndConsole(ModName + ": " + text);
            if (!MyAPIGateway.Utilities.IsDedicated && MyAPIGateway.Session.PromoteLevel >= MyPromoteLevel.Admin)
                MyAPIGateway.Utilities.ShowMessage(ModName, text);
        }

        T Get<T>(string key) where T : class
        {
            Delegate d;
            return _endpoints != null && _endpoints.TryGetValue(key, out d) ? d as T : null;
        }

        /// <summary>API 2. For the other dock files (SyntrixUi): runs when the endpoints were (re)bound or dropped.</summary>
        internal event Action EndpointsChanged;

        /// <summary>API 2. Endpoint by name for the other dock files, null if missing.</summary>
        internal T Endpoint<T>(string key) where T : class { return Get<T>(key); }

        void Changed()
        {
            if (EndpointsChanged == null)
                return;
            try { EndpointsChanged(); }
            catch (Exception e) { MyLog.Default.WriteLineAndConsole(ModName + ": ERROR in Syntrix dock file: " + e); }
        }

        // ---------- endpoints ----------

        Func<bool> _isServer, _isPlayer;
        Func<ulong, int> _promoteLevel;
        Action<string, Action<ulong, byte[]>> _netRegister;
        Action<string> _netUnregister;
        Action<string, byte[]> _netToServer, _netToAll;
        Action<string, ulong, byte[]> _netToPlayer;
        Func<long, string, string, string> _blockGet;
        Action<long, string, string, string> _blockSet;
        Action<string, Action<long, string, string>> _blockOnChanged, _blockRemoveOnChanged;
        Action<long> _blockSync;
        Action<string, int, bool, string, Func<ulong, string[], string>> _chatRegister;
        Action<string, Action<object>> _evSub, _evUnsub;
        Action<string, object> _evPub;
        Action<ulong, string, string, string[], int, string> _notify;
        // API 2
        Action<string> _chatUnregister;
        Action<string, string[]> _evToServer, _evToAll;
        Action<string, ulong, string[]> _evToPlayer;
        Func<ulong> _evSender;
        Action<string, string> _setPublish;
        Action<string, Action<string>> _setSubscribe;

        void Bind()
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
        }

        // Info
        public bool IsServer { get { return _isServer != null ? _isServer() : MyAPIGateway.Session.IsServer; } }
        public bool IsPlayer { get { return _isPlayer != null ? _isPlayer() : !MyAPIGateway.Utilities.IsDedicated; } }
        /// <summary>Promote level of a player (0 none ... 5 owner). Works on the server for any player.</summary>
        public MyPromoteLevel PromoteLevel(ulong steamId) { return _promoteLevel != null ? (MyPromoteLevel)_promoteLevel(steamId) : MyPromoteLevel.None; }

        // ---------- network ----------
        // Channel names must start with your mod name, e.g. "MyMod.sync".
        // On a hosting player (server and player in one) both directions arrive at the same handler.

        public void NetRegister(string channel, Action<ulong, byte[]> handler) { WhenReady(() => { if (_netRegister != null) _netRegister(channel, handler); }); }
        public void NetUnregister(string channel) { if (_netUnregister != null) _netUnregister(channel); }
        public void NetSendToServer(string channel, byte[] data) { if (_netToServer != null) _netToServer(channel, data); }
        /// <summary>Server only.</summary>
        public void NetSendToPlayer(string channel, ulong steamId, byte[] data) { if (_netToPlayer != null) _netToPlayer(channel, steamId, data); }
        /// <summary>Server only: every player, including a hosting player.</summary>
        public void NetSendToAll(string channel, byte[] data) { if (_netToAll != null) _netToAll(channel, data); }

        // ---------- block settings ----------
        // Text per block, saved in the block and mirrored to all players.
        // Use your mod name as modKey. Null or "" removes the value.

        public string BlockGet(long entityId, string modKey, string key) { return _blockGet != null ? _blockGet(entityId, modKey, key) : null; }
        public void BlockSet(long entityId, string modKey, string key, string value) { if (_blockSet != null) _blockSet(entityId, modKey, key, value); }
        /// <summary>Handler(entityId, key, value) runs on the server and on players whenever a value of modKey changes.</summary>
        public void BlockOnChanged(string modKey, Action<long, string, string> handler) { WhenReady(() => { if (_blockOnChanged != null) _blockOnChanged(modKey, handler); }); }
        public void BlockRemoveOnChanged(string modKey, Action<long, string, string> handler) { if (_blockRemoveOnChanged != null) _blockRemoveOnChanged(modKey, handler); }
        public void BlockRequestSync(long entityId) { if (_blockSync != null) _blockSync(entityId); }

        public bool BlockGetBool(long entityId, string modKey, string key, bool def)
        {
            string v = BlockGet(entityId, modKey, key);
            return v == null ? def : v == "1";
        }

        public float BlockGetFloat(long entityId, string modKey, string key, float def)
        {
            float f;
            string v = BlockGet(entityId, modKey, key);
            return v != null && float.TryParse(v, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out f) ? f : def;
        }

        public void BlockSetBool(long entityId, string modKey, string key, bool value) { BlockSet(entityId, modKey, key, value ? "1" : "0"); }
        public void BlockSetFloat(long entityId, string modKey, string key, float value) { BlockSet(entityId, modKey, key, value.ToString("R", System.Globalization.CultureInfo.InvariantCulture)); }

        // ---------- chat commands ----------
        // "/command args". handler(senderSteamId, args) returns the reply (or null).
        // onServer = true runs the handler on the server (register it on every machine).

        public void ChatRegister(string command, MyPromoteLevel level, bool onServer, string help, Func<ulong, string[], string> handler)
        {
            WhenReady(() => { if (_chatRegister != null) _chatRegister(command, (int)level, onServer, help, handler); });
        }

        /// <summary>API 2. Removes a command of this mod.</summary>
        public void ChatUnregister(string command) { if (_chatUnregister != null) _chatUnregister(command); }

        // ---------- events ----------
        // Named events, e.g. "MyMod.CoreDestroyed". Publish stays on this machine and may carry any
        // simple payload (no classes of your own). The Send variants travel over the network; their
        // payload arrives as string[] and EventSender tells who sent it while a handler runs.

        public void Subscribe(string eventName, Action<object> handler) { WhenReady(() => { if (_evSub != null) _evSub(eventName, handler); }); }
        public void Unsubscribe(string eventName, Action<object> handler) { if (_evUnsub != null) _evUnsub(eventName, handler); }
        /// <summary>On this machine only, to every subscribing mod.</summary>
        public void Publish(string eventName, object payload) { if (_evPub != null) _evPub(eventName, payload); }

        /// <summary>API 2. To subscribers on the server (called on the server itself: delivered there).</summary>
        public void SendEventToServer(string eventName, params string[] args) { if (_evToServer != null) _evToServer(eventName, args); }
        /// <summary>API 2. Server only: to subscribers on the server and on every player, each machine once.</summary>
        public void SendEventToAll(string eventName, params string[] args) { if (_evToAll != null) _evToAll(eventName, args); }
        /// <summary>API 2. Server only: to subscribers on one player's machine.</summary>
        public void SendEventToPlayer(string eventName, ulong steamId, params string[] args) { if (_evToPlayer != null) _evToPlayer(eventName, steamId, args); }
        /// <summary>API 2. Steam id of the sender of the network event being handled right now (0 = local event).</summary>
        public ulong EventSender { get { return _evSender != null ? _evSender() : 0; } }

        /// <summary>
        /// Screen notification in each player's own language. Server: steamId 0 = everybody.
        /// textKey is looked up in MyTexts (fallback if missing), args fill {0}, {1} ...;
        /// an argument made with TextArg(key) is translated as well.
        /// </summary>
        public void Notify(ulong steamId, string textKey, string fallback, string[] args = null, int ms = 3000, string font = "White")
        {
            if (_notify != null)
                _notify(steamId, textKey, fallback, args, ms, font);
            else if (!MyAPIGateway.Utilities.IsDedicated)
                MyAPIGateway.Utilities.ShowNotification(SyntrixText.Get(textKey, fallback), ms, font);
        }

        public static string TextArg(string key) { return TextArgPrefix + key; }

        // ---------- settings ----------

        readonly Dictionary<string, string> _settings = new Dictionary<string, string>();

        /// <summary>
        /// API 2. Settings file in your mod's world storage folder (the folder ReadFileInWorldStorage uses
        /// for every type of your mod, so files written before stay valid).
        /// Server: reads the file; if it is missing or empty, uses defaultText and writes it when
        /// writeDefault is true. onText runs at once, then Syntrix hands the text to every player.
        /// Player: onText runs when the server's text arrives (after joining and after every SettingsSave).
        /// The text is yours to parse (XML, ini ...). Keep it below 50,000 characters.
        /// </summary>
        public void SettingsLoad(string file, string defaultText, bool writeDefault, Action<string> onText)
        {
            if (string.IsNullOrEmpty(file) || onText == null)
                return;
            if (MyAPIGateway.Session.IsServer)
            {
                string text = SyntrixFiles.ReadWorld(file);
                if (string.IsNullOrWhiteSpace(text))
                {
                    text = defaultText ?? "";
                    if (writeDefault && defaultText != null)
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

        public static bool ExistsWorld(string file)
        {
            try { return MyAPIGateway.Utilities.FileExistsInWorldStorage(file, typeof(SyntrixFiles)); }
            catch (Exception) { return false; }
        }

        public static void DeleteWorld(string file)
        {
            try
            {
                if (MyAPIGateway.Utilities.FileExistsInWorldStorage(file, typeof(SyntrixFiles)))
                    MyAPIGateway.Utilities.DeleteFileInWorldStorage(file, typeof(SyntrixFiles));
            }
            catch (Exception e) { MyLog.Default.WriteLineAndConsole("Syntrix files: cannot delete world file " + file + ": " + e.Message); }
        }

        /// <summary>Local storage of this computer (player preferences, logs), not part of the world.</summary>
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

        public static string Format(string key, string fallback, params object[] args)
        {
            string text = Get(key, fallback);
            if (args == null || args.Length == 0)
                return text;
            try { return string.Format(text, args); }
            catch (FormatException) { return text; }
        }

        public static MyStringId Id(string key) { return MyStringId.GetOrCompute(key); }
    }

    /// <summary>Log lines with the mod name in front; optionally also into an own file in local storage.</summary>
    public class SyntrixLog
    {
        readonly string _prefix;
        readonly string _file;
        TextWriter _writer;
        bool _fileFailed;
        public bool Debug;

        public SyntrixLog(string modName) { _prefix = modName + ": "; }

        /// <summary>API 2. Also writes every line (with time) to fileName in the mod's local storage.</summary>
        public SyntrixLog(string modName, string fileName) : this(modName) { _file = fileName; }

        public void Info(string text) { Write(_prefix + text); }
        public void Warn(string text) { Write(_prefix + "WARNING " + text); }
        public void Error(string where, Exception e) { Write(_prefix + "ERROR in " + where + ": " + e); }
        public void Dbg(string text) { if (Debug) Write(_prefix + "debug " + text); }

        void Write(string line)
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

    /// <summary>
    /// Terminal controls and toolbar actions for one block type, created lazily: the first time a
    /// player opens the terminal or toolbar of such a block, or when your game logic calls
    /// EnsureCreated (needed on dedicated servers for toolbar actions on timers and buttons).
    /// Adding controls in LoadData breaks the vanilla controls of the type.
    /// </summary>
    public static class SyntrixControls<TBlock> where TBlock : class, IMyTerminalBlock
    {
        static readonly List<Func<IMyTerminalControl>> _controlFactories = new List<Func<IMyTerminalControl>>();
        static readonly List<Func<IMyTerminalAction>> _actionFactories = new List<Func<IMyTerminalAction>>();
        static readonly List<IMyTerminalControl> _controls = new List<IMyTerminalControl>();
        static readonly List<IMyTerminalAction> _actions = new List<IMyTerminalAction>();
        static bool _hooked, _created;

        /// <summary>Any control; the factory runs once, when the controls are created.</summary>
        public static void AddControl(Func<IMyTerminalControl> factory) { _controlFactories.Add(factory); Hook(); }
        public static void AddAction(Func<IMyTerminalAction> factory) { _actionFactories.Add(factory); Hook(); }

        /// <summary>API 2. True once the controls of this type exist.</summary>
        public static bool Created { get { return _created; } }

        static void Hook()
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
            _controlFactories.Clear();
            _actionFactories.Clear();
            _controls.Clear();
            _actions.Clear();
        }

        public static void EnsureCreated()
        {
            if (_created)
                return;
            _created = true;
            foreach (var f in _controlFactories)
            {
                var c = f();
                if (c == null) continue;
                MyAPIGateway.TerminalControls.AddControl<TBlock>(c);
                _controls.Add(c);
            }
            foreach (var f in _actionFactories)
            {
                var a = f();
                if (a == null) continue;
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

        static void ControlGetter(IMyTerminalBlock block, List<IMyTerminalControl> controls)
        {
            if (_created || !(block is TBlock))
                return;
            EnsureCreated();
            // AddControl takes effect from the next opening; show them right away as well.
            controls.AddRange(_controls);
        }

        static void ActionGetter(IMyTerminalBlock block, List<IMyTerminalAction> actions)
        {
            if (_created || !(block is TBlock))
                return;
            EnsureCreated();
            actions.AddRange(_actions);
        }

        static Func<IMyTerminalBlock, bool> Vis(Func<IMyTerminalBlock, bool> visible)
        {
            return b => b is TBlock && (visible == null || visible(b));
        }

        // ---------- ready-made controls bound to Syntrix block settings ----------

        public static void Checkbox(SyntrixApi api, string id, string titleKey, string tooltipKey, string modKey, string key, bool def,
            Func<IMyTerminalBlock, bool> visible = null, Func<IMyTerminalBlock, bool> enabled = null)
        {
            Checkbox(id, titleKey, tooltipKey, b => api.BlockGetBool(b.EntityId, modKey, key, def),
                (b, v) => api.BlockSetBool(b.EntityId, modKey, key, v), visible, enabled);
        }

        public static void Slider(SyntrixApi api, string id, string titleKey, string tooltipKey, string modKey, string key,
            float min, float max, float def, string unit = "", Func<IMyTerminalBlock, bool> visible = null)
        {
            Slider(id, titleKey, tooltipKey, min, max,
                b => api.BlockGetFloat(b.EntityId, modKey, key, def),
                (b, v) => api.BlockSetFloat(b.EntityId, modKey, key, Math.Max(min, Math.Min(max, v))),
                (b, sb) => sb.Append(Math.Round(api.BlockGetFloat(b.EntityId, modKey, key, def), 1)).Append(unit), visible);
        }

        public static void TextBox(SyntrixApi api, string id, string titleKey, string tooltipKey, string modKey, string key,
            Func<IMyTerminalBlock, bool> visible = null)
        {
            TextBox(id, titleKey, tooltipKey, b => api.BlockGet(b.EntityId, modKey, key) ?? "",
                (b, s) => api.BlockSet(b.EntityId, modKey, key, s.Trim()), visible);
        }

        /// <summary>API 2. Drop-down list bound to a block setting; items are (stored value, text key).</summary>
        public static void Combo(SyntrixApi api, string id, string titleKey, string tooltipKey, string modKey, string key,
            IList<MyTuple<string, string>> items, string def, Func<IMyTerminalBlock, bool> visible = null)
        {
            Combo(id, titleKey, tooltipKey, items, b => api.BlockGet(b.EntityId, modKey, key) ?? def,
                (b, v) => api.BlockSet(b.EntityId, modKey, key, v), visible);
        }

        // ---------- API 2: the same controls with your own getter and setter ----------

        public static void Checkbox(string id, string titleKey, string tooltipKey, Func<IMyTerminalBlock, bool> getter,
            Action<IMyTerminalBlock, bool> setter, Func<IMyTerminalBlock, bool> visible = null, Func<IMyTerminalBlock, bool> enabled = null)
        {
            AddControl(() =>
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, TBlock>(id);
                c.Title = MyStringId.GetOrCompute(titleKey);
                c.Tooltip = MyStringId.GetOrCompute(tooltipKey ?? titleKey);
                c.Visible = Vis(visible);
                if (enabled != null) c.Enabled = enabled;
                c.Getter = getter;
                c.Setter = setter;
                return c;
            });
        }

        public static void OnOff(string id, string titleKey, string tooltipKey, Func<IMyTerminalBlock, bool> getter,
            Action<IMyTerminalBlock, bool> setter, Func<IMyTerminalBlock, bool> visible = null, Func<IMyTerminalBlock, bool> enabled = null)
        {
            AddControl(() =>
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, TBlock>(id);
                c.Title = MyStringId.GetOrCompute(titleKey);
                c.Tooltip = MyStringId.GetOrCompute(tooltipKey ?? titleKey);
                c.OnText = MyStringId.GetOrCompute("SwitchText_On");
                c.OffText = MyStringId.GetOrCompute("SwitchText_Off");
                c.Visible = Vis(visible);
                if (enabled != null) c.Enabled = enabled;
                c.Getter = getter;
                c.Setter = setter;
                return c;
            });
        }

        public static void Slider(string id, string titleKey, string tooltipKey, float min, float max, Func<IMyTerminalBlock, float> getter,
            Action<IMyTerminalBlock, float> setter, Action<IMyTerminalBlock, StringBuilder> writer,
            Func<IMyTerminalBlock, bool> visible = null, Func<IMyTerminalBlock, bool> enabled = null)
        {
            AddControl(() =>
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, TBlock>(id);
                c.Title = MyStringId.GetOrCompute(titleKey);
                c.Tooltip = MyStringId.GetOrCompute(tooltipKey ?? titleKey);
                c.Visible = Vis(visible);
                if (enabled != null) c.Enabled = enabled;
                c.SetLimits(min, max);
                c.Getter = getter;
                c.Setter = setter;
                if (writer != null) c.Writer = writer;
                return c;
            });
        }

        public static void TextBox(string id, string titleKey, string tooltipKey, Func<IMyTerminalBlock, string> getter,
            Action<IMyTerminalBlock, string> setter, Func<IMyTerminalBlock, bool> visible = null, Func<IMyTerminalBlock, bool> enabled = null)
        {
            AddControl(() =>
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, TBlock>(id);
                c.Title = MyStringId.GetOrCompute(titleKey);
                c.Tooltip = MyStringId.GetOrCompute(tooltipKey ?? titleKey);
                c.Visible = Vis(visible);
                if (enabled != null) c.Enabled = enabled;
                c.Getter = b => new StringBuilder(getter(b) ?? "");
                c.Setter = (b, sb) => setter(b, sb.ToString());
                return c;
            });
        }

        public static void Combo(string id, string titleKey, string tooltipKey, IList<MyTuple<string, string>> items,
            Func<IMyTerminalBlock, string> getter, Action<IMyTerminalBlock, string> setter,
            Func<IMyTerminalBlock, bool> visible = null, Func<IMyTerminalBlock, bool> enabled = null)
        {
            AddControl(() =>
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCombobox, TBlock>(id);
                c.Title = MyStringId.GetOrCompute(titleKey);
                c.Tooltip = MyStringId.GetOrCompute(tooltipKey ?? titleKey);
                c.Visible = Vis(visible);
                if (enabled != null) c.Enabled = enabled;
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

        public static void Button(string id, string titleKey, string tooltipKey, Action<IMyTerminalBlock> pressed,
            Func<IMyTerminalBlock, bool> visible = null)
        {
            Button(id, titleKey, tooltipKey, pressed, visible, null);
        }

        /// <summary>API 2. Button with an enabled check.</summary>
        public static void Button(string id, string titleKey, string tooltipKey, Action<IMyTerminalBlock> pressed,
            Func<IMyTerminalBlock, bool> visible, Func<IMyTerminalBlock, bool> enabled)
        {
            AddControl(() =>
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, TBlock>(id);
                c.Title = MyStringId.GetOrCompute(titleKey);
                c.Tooltip = MyStringId.GetOrCompute(tooltipKey ?? titleKey);
                c.Visible = Vis(visible);
                if (enabled != null) c.Enabled = enabled;
                c.Action = pressed;
                return c;
            });
        }

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

        public static void Separator(string id, Func<IMyTerminalBlock, bool> visible = null)
        {
            AddControl(() =>
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, TBlock>(id);
                c.Visible = Vis(visible);
                return c;
            });
        }

        /// <summary>Toolbar action (buttons, timers, event controllers). Runs where the toolbar is triggered.</summary>
        public static void Action(string id, string nameKey, string icon, Action<IMyTerminalBlock> run,
            Func<IMyTerminalBlock, bool> enabled = null)
        {
            Action(id, nameKey, icon, run, enabled, null);
        }

        /// <summary>API 2. Toolbar action with a status text on the toolbar slot.</summary>
        public static void Action(string id, string nameKey, string icon, Action<IMyTerminalBlock> run,
            Func<IMyTerminalBlock, bool> enabled, Action<IMyTerminalBlock, StringBuilder> writer)
        {
            AddAction(() =>
            {
                var a = MyAPIGateway.TerminalControls.CreateAction<TBlock>(id);
                a.Name = new StringBuilder(SyntrixText.Get(nameKey, nameKey));
                a.Icon = icon ?? @"Textures\GUI\Icons\Actions\Start.dds";
                a.Action = run;
                a.Enabled = b => b is TBlock && (enabled == null || enabled(b));
                if (writer != null) a.Writer = writer;
                a.ValidForGroups = true;
                return a;
            });
        }
    }
}
