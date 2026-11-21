// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.ModAPI;

namespace Syntrix.Account
{
    /// <summary>
    /// Money of players and factions (API 6, Syntrix 1.5.0). Server only. Wraps the game's bank accounts so that every
    /// mod checks the balance the same way, never charges twice in one frame (the game applies a change a moment later)
    /// and every change raises one event.
    /// </summary>
    public sealed class AccountService
    {
        /// <summary>Event on the server: MyTuple&lt;long identity (0 = faction), string tag (null = player), long change, string reason&gt;.</summary>
        public const string ChangedEvent = "Syntrix.Account.Changed";

        private readonly SyntrixCore _core;
        private readonly List<IMyPlayer> _players = new List<IMyPlayer>();
        // Changes asked for in this frame, not yet visible in the game's balance.
        private readonly Dictionary<long, long> _pendingPlayers = new Dictionary<long, long>();
        private readonly Dictionary<long, long> _pendingFactions = new Dictionary<long, long>();

        /// <summary>Makes the service.</summary>
        public AccountService(SyntrixCore core) { _core = core; }

        /// <summary>Forgets the changes of the last frame (the game shows them by now).</summary>
        public void Tick()
        {
            _pendingPlayers.Clear();
            _pendingFactions.Clear();
        }

        /// <summary>Stops.</summary>
        public void Stop()
        {
            _pendingPlayers.Clear();
            _pendingFactions.Clear();
            _players.Clear();
        }

        private IMyPlayer Player(long identity)
        {
            _players.Clear();
            MyAPIGateway.Players.GetPlayers(_players, p => p.IdentityId == identity);
            var p0 = _players.Count > 0 ? _players[0] : null;
            _players.Clear();
            return p0;
        }

        /// <summary>Balance of an online player, -1 when the player is not online (the game shows no other balance).</summary>
        public long Balance(long identity)
        {
            if (!_core.IsServer)
                return -1;
            var p = Player(identity);
            long balance;
            if (p == null || !p.TryGetBalanceInfo(out balance))
                return -1;
            long pending;
            _pendingPlayers.TryGetValue(identity, out pending);
            return balance + pending;
        }

        /// <summary>Takes money from an online player if the balance covers it. False and nothing taken otherwise.</summary>
        public bool Charge(long identity, long amount, string reason)
        {
            if (amount < 0)
                return false;
            if (amount == 0)
                return true;
            long balance = Balance(identity);
            if (balance < amount)
                return false;
            return Change(identity, -amount, reason);
        }

        /// <summary>Gives money to an online player.</summary>
        public bool Pay(long identity, long amount, string reason)
        {
            if (amount < 0)
                return false;
            return amount == 0 || Change(identity, amount, reason);
        }

        private bool Change(long identity, long delta, string reason)
        {
            var p = Player(identity);
            if (p == null)
                return false;
            p.RequestChangeBalance(delta);
            long pending;
            _pendingPlayers.TryGetValue(identity, out pending);
            _pendingPlayers[identity] = pending + delta;
            _core.Events.Publish(ChangedEvent, new MyTuple<long, string, long, string>(identity, null, delta, reason ?? ""));
            return true;
        }

        /// <summary>Balance of a faction, -1 when it does not exist.</summary>
        public long FactionBalance(string tag)
        {
            if (!_core.IsServer)
                return -1;
            var f = MyAPIGateway.Session.Factions.TryGetFactionByTag(tag);
            long balance;
            if (f == null || !f.TryGetBalanceInfo(out balance))
                return -1;
            long pending;
            _pendingFactions.TryGetValue(f.FactionId, out pending);
            return balance + pending;
        }

        /// <summary>Changes a faction's balance; a negative change only if the balance covers it.</summary>
        public bool FactionChange(string tag, long delta, string reason)
        {
            if (!_core.IsServer || delta == 0)
                return delta == 0;
            var f = MyAPIGateway.Session.Factions.TryGetFactionByTag(tag);
            if (f == null)
                return false;
            if (delta < 0 && FactionBalance(tag) < -delta)
                return false;
            f.RequestChangeBalance(delta);
            long pending;
            _pendingFactions.TryGetValue(f.FactionId, out pending);
            _pendingFactions[f.FactionId] = pending + delta;
            _core.Events.Publish(ChangedEvent, new MyTuple<long, string, long, string>(0, f.Tag, delta, reason ?? ""));
            return true;
        }
    }
}
