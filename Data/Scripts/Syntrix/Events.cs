// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

namespace Syntrix
{
    /// <summary>
    /// Publish/subscribe between mods by event name. Publish stays on this machine and passes the
    /// payload as it is. The Send variants (API 2) travel over the network as string[] and are
    /// delivered to the same subscribers on the target machine; Sender tells who sent them.
    /// </summary>
    public class EventBus
    {
        const long TargetServer = 1;
        const long TargetPlayer = 2;
        const int MaxNameLength = 128;

        readonly SyntrixCore _core;
        readonly Dictionary<string, List<Action<object>>> _subs = new Dictionary<string, List<Action<object>>>();
        ulong _sender;

        public EventBus(SyntrixCore core) { _core = core; }

        /// <summary>Sender of the network event being handled right now, 0 for local events.</summary>
        public ulong Sender { get { return _sender; } }

        public void Stop() { _subs.Clear(); }

        public void Subscribe(string name, Action<object> handler)
        {
            if (string.IsNullOrEmpty(name) || handler == null)
                return;
            List<Action<object>> list;
            if (!_subs.TryGetValue(name, out list))
                _subs[name] = list = new List<Action<object>>();
            if (!list.Contains(handler))
                list.Add(handler);
        }

        public void Unsubscribe(string name, Action<object> handler)
        {
            List<Action<object>> list;
            if (name != null && _subs.TryGetValue(name, out list))
                list.Remove(handler);
        }

        public void Publish(string name, object payload) { Dispatch(name, payload, 0); }

        void Dispatch(string name, object payload, ulong sender)
        {
            List<Action<object>> list;
            if (name == null || !_subs.TryGetValue(name, out list) || list.Count == 0)
                return;
            ulong previous = _sender;
            _sender = sender;
            try
            {
                foreach (var h in list.ToArray())
                {
                    try { h(payload); }
                    catch (Exception e) { Log.Error("event '" + name + "'", e); }
                }
            }
            finally { _sender = previous; }
        }

        // ---------- network (API 2) ----------

        static bool Valid(string name, string[] args)
        {
            if (string.IsNullOrEmpty(name) || name.Length > MaxNameLength)
                return false;
            return args == null || args.Length <= Protocol.MaxEventArgs;
        }

        static string[] Copy(string[] args)
        {
            if (args == null)
                return new string[0];
            var c = new string[args.Length];
            for (int i = 0; i < args.Length; i++)
                c[i] = args[i] ?? "";
            return c;
        }

        public void SendToServer(string name, string[] args)
        {
            if (!Valid(name, args))
                return;
            if (_core.IsServer)
            {
                Dispatch(name, Copy(args), 0);
                return;
            }
            _core.Net.RawToServer(new Packet { Kind = PacketKind.Event, Channel = name, EntityId = TargetServer, Texts = Copy(args) });
        }

        public void SendToAll(string name, string[] args)
        {
            if (!_core.IsServer || !Valid(name, args))
                return;
            var a = Copy(args);
            Dispatch(name, a, 0); // the server, and a hosting player on the same machine
            _core.Net.RawToOthers(new Packet { Kind = PacketKind.Event, Channel = name, EntityId = TargetPlayer, Texts = Copy(args) });
        }

        public void SendToPlayer(string name, ulong steamId, string[] args)
        {
            if (!_core.IsServer || !Valid(name, args))
                return;
            if (_core.IsPlayer && steamId == MyAPIGateway.Multiplayer.MyId)
            {
                Dispatch(name, Copy(args), 0);
                return;
            }
            _core.Net.RawToPlayer(new Packet { Kind = PacketKind.Event, Channel = name, EntityId = TargetPlayer, Texts = Copy(args) }, steamId);
        }

        public void OnPacket(Packet p, ulong sender, bool fromServer)
        {
            if (!Valid(p.Channel, p.Texts))
                return;
            if (p.EntityId == TargetServer && _core.IsServer)
                Dispatch(p.Channel, Copy(p.Texts), sender);
            else if (p.EntityId == TargetPlayer && _core.IsPlayer && !_core.IsServer && fromServer)
                Dispatch(p.Channel, Copy(p.Texts), sender);
        }
    }
}
