// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using VRage;
using VRage.Utils;

namespace Syntrix
{
    /// <summary>
    /// One secure channel for all docked mods. Payloads are byte[] that each mod
    /// serializes itself (classes of other mods cannot cross assembly borders).
    /// Player to player traffic is not offered; everything goes through the server.
    /// </summary>
    public class Network
    {
        /// <summary>Receiver of raw mod data.</summary>
        public enum Target
        {
            /// <summary>The server.</summary>
            Server,
            /// <summary>One player.</summary>
            Player,
            /// <summary>Server and all players.</summary>
            All,
        }

        private readonly SyntrixCore _core;
        private readonly Dictionary<string, Action<ulong, byte[]>> _channels = new Dictionary<string, Action<ulong, byte[]>>();
        private readonly Dictionary<string, string> _owner = new Dictionary<string, string>();

        /// <summary>Makes the network service.</summary>
        public Network(SyntrixCore core) { _core = core; }

        /// <summary>Registers the secure message handler.</summary>
        public void Start() { MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(Protocol.NetChannel, OnMessage); }

        /// <summary>Unregisters the handler and forgets all channels.</summary>
        public void Stop()
        {
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(Protocol.NetChannel, OnMessage);
            _channels.Clear();
            _owner.Clear();
        }

        // ---------- mod channels ----------

        /// <summary>Handler for a raw data channel of a mod.</summary>
        public void RegisterChannel(string modName, string channel, Action<ulong, byte[]> handler)
        {
            if (string.IsNullOrEmpty(channel) || handler == null)
                return;
            string owner;
            if (_owner.TryGetValue(channel, out owner) && owner != modName)
            {
                Log.Warn("Net channel '" + channel + "' of " + modName + " is already used by " + owner + ", ignored.");
                return;
            }
            _owner[channel] = modName;
            _channels[channel] = handler;
        }

        /// <summary>Removes a raw data channel.</summary>
        public void UnregisterChannel(string modName, string channel)
        {
            string owner;
            if (channel != null && _owner.TryGetValue(channel, out owner) && owner == modName)
            {
                _owner.Remove(channel);
                _channels.Remove(channel);
            }
        }

        /// <summary>Sends raw data of a mod channel.</summary>
        public void SendModData(string channel, byte[] data, ulong steamId, Target target)
        {
            if (string.IsNullOrEmpty(channel))
                return;
            var p = new Packet { Kind = PacketKind.ModData, Channel = channel, Data = data };
            switch (target)
            {
                case Target.Server: ToServer(p); break;
                case Target.Player: ToPlayer(p, steamId); break;
                case Target.All: ToAll(p); break;
            }
        }

        /// <summary>Shows a notification: from the server on a player (0 = all players), on a client only there.</summary>
        public void SendNotify(ulong steamId, string key, string fallback, string[] args, int ms, string font)
        {
            if (!_core.IsServer)
            {
                ShowNotify(key, fallback, args, ms, font); // client: only for itself
                return;
            }
            var texts = new List<string> { key ?? "", fallback ?? "", font ?? "", ms.ToString() };
            if (args != null)
                texts.AddRange(args);
            var p = new Packet { Kind = PacketKind.Notify, Texts = texts.ToArray() };
            if (steamId == 0)
                ToAll(p);
            else
                ToPlayer(p, steamId);
        }

        /// <summary>Shows a notification on this machine only.</summary>
        public void ShowLocal(string key, string fallback, string[] args, int ms, string font)
        {
            ShowNotify(key, fallback, args, ms, font);
        }

        // ---------- sending ----------

        /// <summary>Sends a packet to the server (handled directly on the server itself).</summary>
        public void ToServer(Packet p)
        {
            if (_core.IsServer)
            {
                Handle(p, MyAPIGateway.Multiplayer.MyId, true);
                return;
            }
            MyAPIGateway.Multiplayer.SendMessageToServer(Protocol.NetChannel, MyAPIGateway.Utilities.SerializeToBinary(p), true);
        }

        /// <summary>Sends a packet to a player (handled directly when it is this machine).</summary>
        public void ToPlayer(Packet p, ulong steamId)
        {
            if (!_core.IsServer)
                return;
            if (_core.IsPlayer && steamId == MyAPIGateway.Multiplayer.MyId)
            {
                Handle(p, steamId, true);
                return;
            }
            MyAPIGateway.Multiplayer.SendMessageTo(Protocol.NetChannel, MyAPIGateway.Utilities.SerializeToBinary(p), steamId, true);
        }

        // Without local delivery (API 2 services decide themselves what happens on this machine).
        /// <summary>Sends a packet to the server over the network.</summary>
        public void RawToServer(Packet p)
        {
            if (!_core.IsServer)
                MyAPIGateway.Multiplayer.SendMessageToServer(Protocol.NetChannel, MyAPIGateway.Utilities.SerializeToBinary(p), true);
        }

        /// <summary>Sends a packet to all other machines.</summary>
        public void RawToOthers(Packet p)
        {
            if (_core.IsServer && MyAPIGateway.Multiplayer.MultiplayerActive)
                MyAPIGateway.Multiplayer.SendMessageToOthers(Protocol.NetChannel, MyAPIGateway.Utilities.SerializeToBinary(p), true);
        }

        /// <summary>Sends a packet to one player over the network.</summary>
        public void RawToPlayer(Packet p, ulong steamId)
        {
            if (_core.IsServer && steamId != MyAPIGateway.Multiplayer.MyId)
                MyAPIGateway.Multiplayer.SendMessageTo(Protocol.NetChannel, MyAPIGateway.Utilities.SerializeToBinary(p), steamId, true);
        }

        /// <summary>Server only: to every player, including a player on this machine.</summary>
        public void ToAll(Packet p)
        {
            if (!_core.IsServer)
                return;
            if (MyAPIGateway.Multiplayer.MultiplayerActive)
                MyAPIGateway.Multiplayer.SendMessageToOthers(Protocol.NetChannel, MyAPIGateway.Utilities.SerializeToBinary(p), true);
            if (_core.IsPlayer)
                Handle(p, MyAPIGateway.Multiplayer.MyId, true);
        }

        // ---------- receiving ----------

        private void OnMessage(ushort id, byte[] raw, ulong sender, bool fromServer)
        {
            try
            {
                // Players are limited; the server may send larger settings texts.
                if (raw == null || (!fromServer && raw.Length > Protocol.MaxPacketBytes))
                    return;
                var p = MyAPIGateway.Utilities.SerializeFromBinary<Packet>(raw);
                if (p != null)
                    Handle(p, sender, fromServer);
            }
            catch (Exception e)
            {
                Log.Error("net message from " + sender, e);
            }
        }

        private void Handle(Packet p, ulong sender, bool fromServer)
        {
            bool toServer = p.Kind == PacketKind.BlockSet || p.Kind == PacketKind.BlockSyncRequest || p.Kind == PacketKind.ChatRun
                || p.Kind == PacketKind.SettingsRequest || p.Kind == PacketKind.LightsRequest || p.Kind == PacketKind.ReputationRequest;
            bool toPlayer = p.Kind == PacketKind.BlockValues || p.Kind == PacketKind.ChatReply || p.Kind == PacketKind.Notify
                || p.Kind == PacketKind.SettingsValue || p.Kind == PacketKind.Lights || p.Kind == PacketKind.Sound
                || p.Kind == PacketKind.Reputation;

            if (toServer && !_core.IsServer)
                return;
            if (toPlayer && (!_core.IsPlayer || !fromServer))
                return;

            switch (p.Kind)
            {
                case PacketKind.ModData:
                    // Players accept mod data only from the server; the server from anyone.
                    if (!_core.IsServer && !fromServer)
                        return;
                    Action<ulong, byte[]> h;
                    if (p.Channel != null && _channels.TryGetValue(p.Channel, out h))
                    {
                        try { h(sender, p.Data); }
                        catch (Exception e) { Log.Error("net channel '" + p.Channel + "'", e); }
                    }
                    break;
                case PacketKind.BlockSet:
                case PacketKind.BlockSyncRequest:
                case PacketKind.BlockValues:
                    _core.Blocks.OnPacket(p, sender);
                    break;
                case PacketKind.ChatRun:
                case PacketKind.ChatReply:
                    _core.Chat.OnPacket(p, sender);
                    break;
                case PacketKind.Event:
                    _core.Events.OnPacket(p, sender, fromServer);
                    break;
                case PacketKind.SettingsRequest:
                case PacketKind.SettingsValue:
                    _core.Settings.OnPacket(p, sender, fromServer);
                    break;
                case PacketKind.Lights:
                case PacketKind.LightsRequest:
                    _core.Lights.OnPacket(p, sender);
                    break;
                case PacketKind.Reputation:
                case PacketKind.ReputationRequest:
                    _core.Rep.OnPacket(p, sender);
                    break;
                case PacketKind.Sound:
                    _core.Sounds.OnPacket(p);
                    break;
                case PacketKind.Notify:
                    if (p.Texts != null && p.Texts.Length >= 4)
                    {
                        int ms;
                        int.TryParse(p.Texts[3], out ms);
                        var args = new string[p.Texts.Length - 4];
                        Array.Copy(p.Texts, 4, args, 0, args.Length);
                        ShowNotify(p.Texts[0], p.Texts[1], args, ms, p.Texts[2]);
                    }
                    break;
            }
        }

        // ---------- localized notifications ----------

        /// <summary>Text of a key in the game language, or the fallback.</summary>
        public static string Resolve(string key, string fallback)
        {
            if (!string.IsNullOrEmpty(key))
            {
                var id = MyStringId.GetOrCompute(key);
                if (MyTexts.Exists(id))
                    return MyTexts.GetString(id);
            }
            return fallback ?? key ?? "";
        }

        /// <summary>Formats a text key with arguments (arguments may be text keys themselves).</summary>
        public static string Compose(string key, string fallback, string[] args)
        {
            string text = Resolve(key, fallback);
            if (args == null || args.Length == 0)
                return text;
            var resolved = new object[args.Length];
            for (int i = 0; i < args.Length; i++)
            {
                string a = args[i] ?? "";
                resolved[i] = a.Length > 1 && a[0] == Protocol.TextArgPrefix ? Resolve(a.Substring(1), a.Substring(1)) : a;
            }
            try { return string.Format(text, resolved); }
            catch (FormatException) { return text + " " + string.Join(" ", args); }
        }

        private static void ShowNotify(string key, string fallback, string[] args, int ms, string font)
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;
            MyAPIGateway.Utilities.ShowNotification(Compose(key, fallback, args), ms > 0 ? ms : 3000, string.IsNullOrEmpty(font) ? "White" : font);
        }
    }
}
