// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.

namespace Syntrix
{
    /// <summary>Fixed numbers of the Syntrix protocol. Never change a published value.</summary>
    public static class Protocol
    {
        /// <summary>Mod message channel (SendModMessage) used for docking.</summary>
        public const long ApiChannel = 8346120579001L;

        /// <summary>Secure network channel shared by all docked mods.</summary>
        public const ushort NetChannel = 52741;

        /// <summary>API version this framework build offers.</summary>
        public const int ApiVersion = 5;

        /// <summary>Oldest dock file version this build still accepts.</summary>
        public const int MinApiVersion = 1;

        /// <summary>Human readable version (semantic versioning).</summary>
        public const string VersionText = "1.4.0";

        /// <summary>Message tag: a mod asks to dock.</summary>
        public const string TagRegister = "syx.register";
        /// <summary>Message tag: a Syntrix copy announces itself.</summary>
        public const string TagHello = "syx.hello";
        /// <summary>Message tag: a Syntrix copy steps back.</summary>
        public const string TagBye = "syx.bye";
        /// <summary>Message tag: the active Syntrix copy.</summary>
        public const string TagInstance = "syx.instance";

        /// <summary>Prefix that marks a notification argument as a text key.</summary>
        public const char TextArgPrefix = '\u0001';

        // Limits for data sent by players.

        /// <summary>Longest block setting value.</summary>
        public const int MaxValueLength = 4096;
        /// <summary>Most settings per block and mod.</summary>
        public const int MaxValuesPerBlock = 256;
        /// <summary>Largest network packet.</summary>
        public const int MaxPacketBytes = 60000;
        /// <summary>Most arguments of an event.</summary>
        public const int MaxEventArgs = 64;
        /// <summary>Longest settings text.</summary>
        public const int MaxSettingsLength = 50000;
    }
}
