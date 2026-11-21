// Syntrix AutoLCD - MIT License, Copyright (c) 2026 Syntrix contributors.
// Operation compatible with Automatic LCDs 2 by MMaster; no code of it was taken.
using Sandbox.ModAPI;

namespace Syntrix.AutoLcd
{
    /// <summary>
    /// Shared constants and services of Syntrix AutoLCD: the Syntrix connection, the log and the server settings.
    /// Part of Syntrix since 1.5.0; chat, channels and settings are registered under the name "Syntrix-AutoLCD".
    /// Channels: Syntrix-AutoLCD.toServer (player to server: run argument, screen action, status request),
    /// Syntrix-AutoLCD.toPlayer (server to player: screen status). Events: SyxAutoLcd.RegisterCommand and
    /// SyxAutoLcd.RegisterCommandTimed (provider commands of other mods), SyxAutoLcd.Ready (published when AutoLCD is ready for providers).
    /// </summary>
    internal static class AutoLcdCore
    {
        public const string ModName = "Syntrix-AutoLCD";

        public const string ChannelToServer = "Syntrix-AutoLCD.toServer";

        public const string ChannelToPlayer = "Syntrix-AutoLCD.toPlayer";

        public const string SettingsFile = "Syntrix-AutoLcd.xml";

        public const string EventRegisterCommand = "SyxAutoLcd.RegisterCommand";

        /// <summary>Like <see cref="EventRegisterCommand"/> with the seconds between two runs:
        /// MyTuple&lt;string, double, Func&lt;IMyTerminalBlock, string[], List&lt;MyTuple&lt;string, string, float&gt;&gt;&gt;&gt;.</summary>
        public const string EventRegisterCommandTimed = "SyxAutoLcd.RegisterCommandTimed";

        public const string EventReady = "SyxAutoLcd.Ready";

        public const string ChatCommand = "syxlcd";

        public const char Separator = '\u001f';

        public static AutoLcdHost Syx = new AutoLcdHost();

        public static readonly AutoLcdLog Log = new AutoLcdLog();

        public static AutoLcdSettings Settings = AutoLcdSettings.CreateDefault();

        /// <summary>True while the screen service runs (server, docked, not paused by an admin).</summary>
        public static bool Paused;

        /// <summary>Seconds of game time since the session started.</summary>
        public static double Now
        {
            get { return MyAPIGateway.Session != null ? MyAPIGateway.Session.ElapsedPlayTime.TotalSeconds : 0; }
        }

        /// <summary>True if in-game scripts are enabled in this world.</summary>
        public static bool ScriptsEnabled
        {
            get
            {
                var settings = MyAPIGateway.Session != null ? MyAPIGateway.Session.SessionSettings : null;
                return settings != null && settings.EnableIngameScripts;
            }
        }
    }
}
