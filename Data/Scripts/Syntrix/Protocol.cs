// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using ProtoBuf;

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
        public const int ApiVersion = 1;

        /// <summary>Oldest dock file version this build still accepts.</summary>
        public const int MinApiVersion = 1;

        /// <summary>Human readable version (semantic versioning).</summary>
        public const string VersionText = "1.0.0";

        public const string TagRegister = "syx.register";
        public const string TagHello = "syx.hello";
        public const string TagBye = "syx.bye";
        public const string TagInstance = "syx.instance";

        /// <summary>Prefix that marks a notification argument as a text key.</summary>
        public const char TextArgPrefix = '\u0001';

        // Limits for data sent by players.
        public const int MaxValueLength = 4096;
        public const int MaxValuesPerBlock = 256;
        public const int MaxPacketBytes = 60000;
    }

    public enum PacketKind : byte
    {
        ModData = 1,
        BlockSet = 2,
        BlockSyncRequest = 3,
        BlockValues = 4,
        ChatRun = 5,
        ChatReply = 6,
        Notify = 7,
    }

    [ProtoContract]
    public class Packet
    {
        [ProtoMember(1)] public PacketKind Kind;
        [ProtoMember(2)] public string Channel;
        [ProtoMember(3)] public byte[] Data;
        [ProtoMember(4)] public long EntityId;
        [ProtoMember(5)] public string[] Texts;
    }
}
