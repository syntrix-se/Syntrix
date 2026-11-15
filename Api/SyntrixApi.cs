// Syntrix dock file (API version 1). Copy this file into your mod's script folder.
// Do not edit it; take a newer copy from the Syntrix repository instead.
//
// MIT License - Copyright (c) 2026 Syntrix contributors
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files, to deal in the Software without restriction,
// subject to including this notice. Full text: LICENSE in the Syntrix repository.
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND.
using System;
using System.Collections.Generic;
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
    /// (or BeforeStart) and Unload in UnloadData. Use the services after onReady fired.
    /// Every method is safe to call without the framework: it then does nothing.
    /// </summary>
    public class SyntrixApi
    {
        public const long Channel = 8346120579001L;
        public const int ApiVersion = 1;

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
            if (first && _onReady != null)
            {
                try { _onReady(); }
                catch (Exception e) { MyLog.Default.WriteLineAndConsole(ModName + ": ERROR in Syntrix onReady: " + e); }
            }
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
            _evSub = Get<Action<string, Action<object>>>("Events.Subscribe");
            _evUnsub = Get<Action<string, Action<object>>>("Events.Unsubscribe");
            _evPub = Get<Action<string, object>>("Events.Publish");
            _notify = Get<Action<ulong, string, string, string[], int, string>>("Notify.Player");
        }

        // Info
        public bool IsServer { get { return _isServer != null ? _isServer() : MyAPIGateway.Session.IsServer; } }
        public bool IsPlayer { get { return _isPlayer != null ? _isPlayer() : !MyAPIGateway.Utilities.IsDedicated; } }
        /// <summary>Promote level of a player (0 none ... 5 owner). Works on the server for any player.</summary>
        public MyPromoteLevel PromoteLevel(ulong steamId) { return _promoteLevel != null ? (MyPromoteLevel)_promoteLevel(steamId) : MyPromoteLevel.None; }

        // Network. Channel names must start with your mod name, e.g. "MyMod.sync".
        // On a hosting player (server and player in one) both directions arrive at the same handler.
        public void NetRegister(string channel, Action<ulong, byte[]> handler) { if (_netRegister != null) _netRegister(channel, handler); }
        public void NetUnregister(string channel) { if (_netUnregister != null) _netUnregister(channel); }
        public void NetSendToServer(string channel, byte[] data) { if (_netToServer != null) _netToServer(channel, data); }
        /// <summary>Server only.</summary>
        public void NetSendToPlayer(string channel, ulong steamId, byte[] data) { if (_netToPlayer != null) _netToPlayer(channel, steamId, data); }
        /// <summary>Server only: every player, including a hosting player.</summary>
        public void NetSendToAll(string channel, byte[] data) { if (_netToAll != null) _netToAll(channel, data); }

        // Block settings: text per block, saved in the block and mirrored to all players.
        // Use your mod name as modKey. Null or "" removes the value.
        public string BlockGet(long entityId, string modKey, string key) { return _blockGet != null ? _blockGet(entityId, modKey, key) : null; }
        public void BlockSet(long entityId, string modKey, string key, string value) { if (_blockSet != null) _blockSet(entityId, modKey, key, value); }
        /// <summary>Handler(entityId, key, value) runs on the server and on players whenever a value of modKey changes.</summary>
        public void BlockOnChanged(string modKey, Action<long, string, string> handler) { if (_blockOnChanged != null) _blockOnChanged(modKey, handler); }
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

        // Chat commands: "/command args". handler(senderSteamId, args) returns the reply (or null).
        // onServer = true runs the handler on the server (register it on every machine).
        public void ChatRegister(string command, MyPromoteLevel level, bool onServer, string help, Func<ulong, string[], string> handler)
        {
            if (_chatRegister != null) _chatRegister(command, (int)level, onServer, help, handler);
        }

        // Events between mods on the same machine. Payloads should be simple types.
        public void Subscribe(string eventName, Action<object> handler) { if (_evSub != null) _evSub(eventName, handler); }
        public void Unsubscribe(string eventName, Action<object> handler) { if (_evUnsub != null) _evUnsub(eventName, handler); }
        public void Publish(string eventName, object payload) { if (_evPub != null) _evPub(eventName, payload); }

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
            try { return string.Format(text, args); }
            catch (FormatException) { return text; }
        }

        public static MyStringId Id(string key) { return MyStringId.GetOrCompute(key); }
    }

    /// <summary>Log lines with the mod name in front.</summary>
    public class SyntrixLog
    {
        readonly string _prefix;
        public bool Debug;

        public SyntrixLog(string modName) { _prefix = modName + ": "; }

        public void Info(string text) { MyLog.Default.WriteLineAndConsole(_prefix + text); }
        public void Warn(string text) { MyLog.Default.WriteLineAndConsole(_prefix + "WARNING " + text); }
        public void Error(string where, Exception e) { MyLog.Default.WriteLineAndConsole(_prefix + "ERROR in " + where + ": " + e); }
        public void Dbg(string text) { if (Debug) MyLog.Default.WriteLineAndConsole(_prefix + "debug " + text); }
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

        public static void AddControl(Func<IMyTerminalControl> factory) { _controlFactories.Add(factory); Hook(); }
        public static void AddAction(Func<IMyTerminalAction> factory) { _actionFactories.Add(factory); Hook(); }

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

        // ---------- ready-made controls bound to Syntrix block settings ----------

        static Func<IMyTerminalBlock, bool> Vis(Func<IMyTerminalBlock, bool> visible)
        {
            return b => b is TBlock && (visible == null || visible(b));
        }

        public static void Checkbox(SyntrixApi api, string id, string titleKey, string tooltipKey, string modKey, string key, bool def,
            Func<IMyTerminalBlock, bool> visible = null, Func<IMyTerminalBlock, bool> enabled = null)
        {
            AddControl(() =>
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, TBlock>(id);
                c.Title = MyStringId.GetOrCompute(titleKey);
                c.Tooltip = MyStringId.GetOrCompute(tooltipKey ?? titleKey);
                c.Visible = Vis(visible);
                if (enabled != null) c.Enabled = enabled;
                c.Getter = b => api.BlockGetBool(b.EntityId, modKey, key, def);
                c.Setter = (b, v) => api.BlockSetBool(b.EntityId, modKey, key, v);
                return c;
            });
        }

        public static void Slider(SyntrixApi api, string id, string titleKey, string tooltipKey, string modKey, string key,
            float min, float max, float def, string unit = "", Func<IMyTerminalBlock, bool> visible = null)
        {
            AddControl(() =>
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, TBlock>(id);
                c.Title = MyStringId.GetOrCompute(titleKey);
                c.Tooltip = MyStringId.GetOrCompute(tooltipKey ?? titleKey);
                c.Visible = Vis(visible);
                c.SetLimits(min, max);
                c.Getter = b => api.BlockGetFloat(b.EntityId, modKey, key, def);
                c.Setter = (b, v) => api.BlockSetFloat(b.EntityId, modKey, key, Math.Max(min, Math.Min(max, v)));
                c.Writer = (b, sb) => sb.Append(Math.Round(api.BlockGetFloat(b.EntityId, modKey, key, def), 1)).Append(unit);
                return c;
            });
        }

        public static void TextBox(SyntrixApi api, string id, string titleKey, string tooltipKey, string modKey, string key,
            Func<IMyTerminalBlock, bool> visible = null)
        {
            AddControl(() =>
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, TBlock>(id);
                c.Title = MyStringId.GetOrCompute(titleKey);
                c.Tooltip = MyStringId.GetOrCompute(tooltipKey ?? titleKey);
                c.Visible = Vis(visible);
                c.Getter = b => new StringBuilder(api.BlockGet(b.EntityId, modKey, key) ?? "");
                c.Setter = (b, sb) => api.BlockSet(b.EntityId, modKey, key, sb.ToString().Trim());
                return c;
            });
        }

        public static void Button(string id, string titleKey, string tooltipKey, Action<IMyTerminalBlock> pressed,
            Func<IMyTerminalBlock, bool> visible = null)
        {
            AddControl(() =>
            {
                var c = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlButton, TBlock>(id);
                c.Title = MyStringId.GetOrCompute(titleKey);
                c.Tooltip = MyStringId.GetOrCompute(tooltipKey ?? titleKey);
                c.Visible = Vis(visible);
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
            AddAction(() =>
            {
                var a = MyAPIGateway.TerminalControls.CreateAction<TBlock>(id);
                a.Name = new StringBuilder(SyntrixText.Get(nameKey, nameKey));
                a.Icon = icon ?? @"Textures\GUI\Icons\Actions\Start.dds";
                a.Action = run;
                a.Enabled = b => b is TBlock && (enabled == null || enabled(b));
                a.ValidForGroups = true;
                return a;
            });
        }
    }
}
