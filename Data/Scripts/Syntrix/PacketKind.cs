// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.

namespace Syntrix
{
    /// <summary>Kinds of packets on the Syntrix network channel. Never change a published value.</summary>
    public enum PacketKind : byte
    {
        /// <summary>Raw data of a mod channel.</summary>
        ModData = 1,
        /// <summary>A player sets a block setting (server checks access).</summary>
        BlockSet = 2,
        /// <summary>A player asks for the settings of a block.</summary>
        BlockSyncRequest = 3,
        /// <summary>Settings of a block from the server.</summary>
        BlockValues = 4,
        /// <summary>A player runs a chat command on the server.</summary>
        ChatRun = 5,
        /// <summary>Answer of a chat command.</summary>
        ChatReply = 6,
        /// <summary>Notification to a player.</summary>
        Notify = 7,

        // API 2

        /// <summary>Event with string arguments.</summary>
        Event = 8,
        /// <summary>Settings text from the server.</summary>
        SettingsValue = 9,
        /// <summary>A player asks for a settings text.</summary>
        SettingsRequest = 10,

        // API 4 (Syntrix 1.3.0)

        /// <summary>Block lights from the server (set or clear).</summary>
        Lights = 11,
        /// <summary>A player asks for all current block lights.</summary>
        LightsRequest = 12,
        /// <summary>A short sound at a block or a place.</summary>
        Sound = 13,

        // API 5 (Syntrix 1.4.0)

        /// <summary>Reputation from the server (definitions, values, faction pairs).</summary>
        Reputation = 14,
        /// <summary>A player asks for the reputation definitions and its values.</summary>
        ReputationRequest = 15,
    }
}
