// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
using System;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Utils;

namespace Syntrix.AutoLcd
{
    /// <summary>
    /// AutoLCD inside Syntrix (1.5.0): the few framework calls of AutoLCD go straight to the running Syntrix services
    /// instead of through the dock file. Same names as the dock file, so the screen code stays as it was. Runs only in
    /// the active Syntrix copy (a newer copy runs its own AutoLCD).
    /// </summary>
    internal sealed class AutoLcdHost
    {
        private static SyntrixCore Core { get { return SyntrixCore.Instance; } }

        /// <summary>The Syntrix services are running in this copy.</summary>
        public bool IsReady { get { var c = Core; return c != null && c.Active && c.Started; } }

        public void Subscribe(string name, Action<object> handler) { Core.Events.Subscribe(name, handler); }

        public void Unsubscribe(string name, Action<object> handler) { Core.Events.Unsubscribe(name, handler); }

        public void Publish(string name, object payload) { Core.Events.Publish(name, payload); }

        public void NetRegister(string channel, Action<ulong, byte[]> handler) { Core.Net.RegisterChannel(AutoLcdCore.ModName, channel, handler); }

        public void NetUnregister(string channel) { Core.Net.UnregisterChannel(AutoLcdCore.ModName, channel); }

        public void NetSendToServer(string channel, byte[] data) { Core.Net.SendModData(channel, data, 0, Network.Target.Server); }

        public void NetSendToPlayer(string channel, ulong steamId, byte[] data) { Core.Net.SendModData(channel, data, steamId, Network.Target.Player); }

        public void ChatRegister(string command, MyPromoteLevel level, bool onServer, string help, Func<ulong, string[], string> handler)
        {
            Core.Chat.Register(AutoLcdCore.ModName, command, (int)level, onServer, help, handler);
        }

        public void ChatUnregister(string command) { Core.Chat.Unregister(AutoLcdCore.ModName, command); }

        public MyPromoteLevel PromoteLevel(ulong steamId) { return Core.PromoteLevel(steamId); }

        /// <summary>
        /// Server: reads the file from the world storage of Syntrix (writes defaultText when it is missing and
        /// writeDefault is true), runs onText and hands the text to every player. Player: onText runs when the
        /// server's text arrives.
        /// </summary>
        public void SettingsLoad(string file, string defaultText, bool writeDefault, Action<string> onText)
        {
            var core = Core;
            if (core.IsServer)
            {
                string text = null;
                bool exists = false;
                try
                {
                    exists = MyAPIGateway.Utilities.FileExistsInWorldStorage(file, typeof(AutoLcdHost));
                    if (exists)
                        using (var r = MyAPIGateway.Utilities.ReadFileInWorldStorage(file, typeof(AutoLcdHost)))
                            text = r.ReadToEnd();
                }
                catch (Exception e) { AutoLcdCore.Log.Error("settings " + file, e); }
                if (string.IsNullOrWhiteSpace(text))
                {
                    text = defaultText ?? "";
                    // Only a missing file gets the default; an empty or unreadable one is left for the admin.
                    if (writeDefault && !exists && defaultText != null)
                    {
                        try
                        {
                            using (var w = MyAPIGateway.Utilities.WriteFileInWorldStorage(file, typeof(AutoLcdHost)))
                                w.Write(defaultText);
                        }
                        catch (Exception e) { AutoLcdCore.Log.Error("settings " + file, e); }
                    }
                }
                try { onText(text); }
                catch (Exception e) { AutoLcdCore.Log.Error("settings " + file, e); }
                core.Settings.Publish(AutoLcdCore.ModName, file, text);
            }
            else
                core.Settings.Subscribe(AutoLcdCore.ModName, file, onText);
        }
    }

    /// <summary>Lines of AutoLCD in the game log.</summary>
    internal sealed class AutoLcdLog
    {
        private const string Prefix = "Syntrix AutoLCD: ";

        public void Info(string text) { MyLog.Default.WriteLine(Prefix + text); }

        public void Warn(string text) { MyLog.Default.WriteLine(Prefix + "WARNING " + text); }

        public void Error(string where, Exception e) { MyLog.Default.WriteLineAndConsole(Prefix + "ERROR in " + where + ": " + e); }
    }
}
