// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
// Operation compatible with Automatic LCDs 2 by MMaster; no code of it was taken.
using System;
using System.Diagnostics;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace Syntrix.AutoLcd
{
    /// <summary>
    /// Session component of Syntrix AutoLCD. Server (and single player): settings, item catalogue, screen language,
    /// the screen service within the time budget, run arguments and actions. Every machine: own toolbar actions, the
    /// interception of "Run" on AutoLCD programmable blocks while scripts are off, the info text of screens.
    /// Part of Syntrix since 1.5.0: starts once the Syntrix services run in this copy (only the active copy).
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class AutoLcdSession : MySessionComponentBase
    {
        private readonly Stopwatch _watch = new Stopwatch();
        private bool _started;
        private bool _registered;
        private bool _isServer;
        private double _lastNow = double.NaN;

        public override void LoadData()
        {
            AutoLcdCore.Syx = new AutoLcdHost();
            AutoLcdCore.Paused = false;
            _isServer = MyAPIGateway.Session.IsServer;
            CommandRegistry.RegisterBuiltIn();
        }

        /// <summary>Channels, chat, settings, provider events and actions; once the Syntrix services run.</summary>
        private void Register()
        {
            _registered = true;
            AutoLcdCore.Syx.NetRegister(AutoLcdCore.ChannelToServer, RunInterceptor.OnServerMessage);
            AutoLcdCore.Syx.NetRegister(AutoLcdCore.ChannelToPlayer, RunInterceptor.OnPlayerMessage);
            AutoLcdCore.Syx.ChatRegister(AutoLcdCore.ChatCommand, MyPromoteLevel.None, true,
                "/" + AutoLcdCore.ChatCommand + " help | stats | scroll up/down \"screen\" [lines] | reload | pause | resume", ChatCommands.Handle);
            AutoLcdCore.Syx.SettingsLoad(AutoLcdCore.SettingsFile, AutoLcdSettings.DefaultText, true, ApplySettings);
            if (_isServer)
            {
                AutoLcdCore.Syx.Subscribe(AutoLcdCore.EventRegisterCommand, CommandRegistry.OnRegister);
                AutoLcdCore.Syx.Subscribe(AutoLcdCore.EventRegisterCommandTimed, CommandRegistry.OnRegisterTimed);
            }
            RunInterceptor.Load();
        }

        protected override void UnloadData()
        {
            try
            {
                if (_registered)
                    Unregister();
            }
            catch (Exception e)
            {
                AutoLcdCore.Log.Error("unload", e);
            }
            CommandRegistry.ClearProviders();
            BlockSource.Clear();
            FontMetrics.Clear();
            TagMatcher.Clear();
            JumpDriveCommands.Clear();
            ShipCommands.Clear();
            _started = false;
            _registered = false;
        }

        private void Unregister()
        {
            RunInterceptor.Unload();
            ScreenRegistry.Stop();
            if (!AutoLcdCore.Syx.IsReady)
                return;   // Syntrix already stopped its services
            if (_isServer)
            {
                AutoLcdCore.Syx.Unsubscribe(AutoLcdCore.EventRegisterCommand, CommandRegistry.OnRegister);
                AutoLcdCore.Syx.Unsubscribe(AutoLcdCore.EventRegisterCommandTimed, CommandRegistry.OnRegisterTimed);
            }
            AutoLcdCore.Syx.ChatUnregister(AutoLcdCore.ChatCommand);
            AutoLcdCore.Syx.NetUnregister(AutoLcdCore.ChannelToServer);
            AutoLcdCore.Syx.NetUnregister(AutoLcdCore.ChannelToPlayer);
        }

        /// <summary>Applies the settings text (server: read from the file; player: sent by the server).</summary>
        public static void ApplySettings(string text)
        {
            AutoLcdCore.Settings = AutoLcdSettings.Parse(text);
            if (!MyAPIGateway.Session.IsServer)
                return;
            LcdText.Load(AutoLcdCore.Settings);
            ItemCatalog.Build(AutoLcdCore.Settings);
            FontMetrics.Clear();
            TagMatcher.Clear();
            ScreenRegistry.DetectAll();
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                if (!_started)
                {
                    if (!AutoLcdCore.Syx.IsReady)
                        return;
                    Start();
                }
                CheckTime();
                RunInterceptor.Update();
                if (!_isServer || AutoLcdCore.Paused)
                    return;
                _watch.Restart();
                long budget = (long)(AutoLcdCore.Settings.MaxMsPerTick * Stopwatch.Frequency / 1000.0);
                ScreenRegistry.Update(AutoLcdCore.Now, _watch, budget);
                _watch.Stop();
            }
            catch (Exception e)
            {
                AutoLcdCore.Log.Error("update", e);
            }
        }

        /// <summary>
        /// The game time (ElapsedPlayTime) must not go back; if it does anyway, every remembered due time would
        /// hold its work until the old time is reached again. Then all of them start anew.
        /// </summary>
        private void CheckTime()
        {
            double now = AutoLcdCore.Now;
            if (!double.IsNaN(_lastNow) && now < _lastNow - 0.5)
            {
                AutoLcdCore.Log.Info("game time went back from " + _lastNow.ToString("0") + " s to " + now.ToString("0") + " s, due times reset");
                RunInterceptor.ResetTimes();
                BlockSource.Clear();
                JumpDriveCommands.Clear();
                ShipCommands.Clear();
                if (_isServer)
                    ScreenRegistry.ResetTimes();
            }
            _lastNow = now;
        }

        private void Start()
        {
            _started = true;
            Register();
            if (!_isServer)
                return;
            if (ItemCatalog.Items.Count == 0)
            {
                LcdText.Load(AutoLcdCore.Settings);
                ItemCatalog.Build(AutoLcdCore.Settings);
            }
            ScreenRegistry.Start();
            AutoLcdCore.Syx.Publish(AutoLcdCore.EventReady, AutoLcdCore.ModName);
        }
    }
}
