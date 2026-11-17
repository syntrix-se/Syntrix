// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using ProtoBuf;

namespace Syntrix
{
    /// <summary>
    /// Packet of the Syntrix network channel. Public fields because ProtoBuf serialises them (deliberate
    /// exception from the property rule).
    /// </summary>
    [ProtoContract]
    public class Packet
    {
        /// <summary>What the packet carries.</summary>
        [ProtoMember(1)] public PacketKind Kind;
        /// <summary>Mod channel, mod name or event name.</summary>
        [ProtoMember(2)] public string Channel;
        /// <summary>Raw data.</summary>
        [ProtoMember(3)] public byte[] Data;
        /// <summary>Block entity id.</summary>
        [ProtoMember(4)] public long EntityId;
        /// <summary>Texts (keys, values, arguments).</summary>
        [ProtoMember(5)] public string[] Texts;
    }
}
