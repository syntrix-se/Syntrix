// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using System.Globalization;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Syntrix.Effects
{
    /// <summary>
    /// Short sounds at a block or a place (API 4, Syntrix 1.3.0). The server sends the sound to the players within the
    /// radius, each player plays it once in 3D at that place (sound cue name as in Audio.sbc, e.g. "HudUnable"; the game
    /// picks the Arcade or Realistic variant). Not for loops: those belong in a sound block. On a player (not the server)
    /// a call plays only there.
    /// </summary>
    public sealed class SoundService
    {
        private const double MaxRadius = 200;
        private const int MaxPlaying = 32;

        private readonly SyntrixCore _core;
        private readonly List<MyEntity3DSoundEmitter> _playing = new List<MyEntity3DSoundEmitter>();
        private readonly List<IMyPlayer> _players = new List<IMyPlayer>();
        private int _tick;

        /// <summary>Makes the service.</summary>
        public SoundService(SyntrixCore core) { _core = core; }

        /// <summary>Stops all sounds.</summary>
        public void Stop()
        {
            foreach (var e in _playing)
            {
                try { e.StopSound(true); } catch (Exception) { }
            }
            _playing.Clear();
        }

        /// <summary>Plays a sound at a block for the players within the radius.</summary>
        /// <param name="entityId">Block (or any entity) id; the sound follows it.</param>
        /// <param name="cue">Sound cue name.</param>
        /// <param name="radius">Radius in metres (1 to 200).</param>
        public void Play(long entityId, string cue, double radius)
        {
            var entity = MyAPIGateway.Entities.GetEntityById(entityId);
            if (entity == null || string.IsNullOrEmpty(cue))
                return;
            Send(new Packet { Kind = PacketKind.Sound, EntityId = entityId, Texts = new[] { cue } }, entity.WorldMatrix.Translation, radius);
        }

        /// <summary>Plays a sound at a place for the players within the radius.</summary>
        /// <param name="position">Place in world coordinates.</param>
        /// <param name="cue">Sound cue name.</param>
        /// <param name="radius">Radius in metres (1 to 200).</param>
        public void PlayAt(Vector3D position, string cue, double radius)
        {
            if (string.IsNullOrEmpty(cue))
                return;
            var texts = new[]
            {
                cue, position.X.ToString("R", CultureInfo.InvariantCulture), position.Y.ToString("R", CultureInfo.InvariantCulture),
                position.Z.ToString("R", CultureInfo.InvariantCulture),
            };
            Send(new Packet { Kind = PacketKind.Sound, Texts = texts }, position, radius);
        }

        private void Send(Packet p, Vector3D position, double radius)
        {
            if (!_core.IsServer)
            {
                OnPacket(p);        // player: only here
                return;
            }
            radius = Math.Max(1, Math.Min(MaxRadius, radius));
            _players.Clear();
            MyAPIGateway.Players.GetPlayers(_players);
            foreach (var player in _players)
            {
                var at = player.Character != null ? player.Character.WorldMatrix.Translation
                    : player.Controller?.ControlledEntity?.Entity?.WorldMatrix.Translation;
                if (at.HasValue && Vector3D.DistanceSquared(at.Value, position) <= radius * radius)
                    _core.Net.ToPlayer(p, player.SteamUserId);
            }
            _players.Clear();
        }

        /// <summary>Sound packet from the server (or a local call on a player).</summary>
        public void OnPacket(Packet p)
        {
            if (!_core.IsPlayer || p.Texts == null || p.Texts.Length == 0 || _playing.Count >= MaxPlaying)
                return;
            MyEntity3DSoundEmitter emitter;
            if (p.EntityId != 0)
            {
                var entity = MyAPIGateway.Entities.GetEntityById(p.EntityId) as MyEntity;
                if (entity == null)
                    return;
                emitter = new MyEntity3DSoundEmitter(entity);
            }
            else
            {
                double x, y, z;
                if (p.Texts.Length < 4
                    || !double.TryParse(p.Texts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out x)
                    || !double.TryParse(p.Texts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out y)
                    || !double.TryParse(p.Texts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out z))
                    return;
                emitter = new MyEntity3DSoundEmitter(null);
                emitter.SetPosition(new Vector3D(x, y, z));
            }
            try
            {
                emitter.PlaySound(new MySoundPair(p.Texts[0]));
                _playing.Add(emitter);
            }
            catch (Exception e)
            {
                Log.Error("sound '" + p.Texts[0] + "'", e);
            }
        }

        /// <summary>Every frame (players): drops finished sounds.</summary>
        public void Tick()
        {
            if (_playing.Count == 0 || ++_tick % 10 != 0)
                return;
            for (int i = _playing.Count - 1; i >= 0; i--)
            {
                var e = _playing[i];
                if (!e.IsPlaying)
                {
                    try { e.Cleanup(); } catch (Exception) { }
                    _playing.RemoveAt(i);
                }
                else
                    e.Update();
            }
        }
    }
}
