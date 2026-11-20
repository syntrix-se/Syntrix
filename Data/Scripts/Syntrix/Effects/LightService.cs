// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using System.Globalization;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;

namespace Syntrix.Effects
{
    /// <summary>
    /// Block lights (API 4, Syntrix 1.3.0): colour and blinking of the emissive parts of a block (status lights, material
    /// "Emissive" and others). Set on the server, the server keeps the state and sends it to every player, also to players
    /// who join later. Players apply it about six times a second within 300 m of the camera and put it back when the game
    /// paints the parts itself (on, off, damage, skin). Nothing is saved: after a restart the mod sets it again. Clearing
    /// gives the parts back to the game. On a player (not the server) a call changes only that player's view.
    /// </summary>
    public sealed class LightService
    {
        private const double ApplyDistance = 300;
        private const int Interval = 10;
        private const int Reassert = 60;

        private sealed class State
        {
            public Color Color;
            public float Emissivity;
            public float Blink;
            public int Phase = -1;
        }

        private readonly SyntrixCore _core;
        private readonly Dictionary<long, Dictionary<string, State>> _states = new Dictionary<long, Dictionary<string, State>>();
        private readonly List<long> _gone = new List<long>();
        private int _tick;

        /// <summary>Makes the service.</summary>
        public LightService(SyntrixCore core) { _core = core; }

        /// <summary>Starts: a player asks the server for all current lights.</summary>
        public void Start()
        {
            if (!_core.IsServer)
                _core.Net.RawToServer(new Packet { Kind = PacketKind.LightsRequest });
        }

        /// <summary>Forgets everything.</summary>
        public void Stop() { _states.Clear(); }

        /// <summary>Sets colour, brightness and blinking of an emissive part of a block.</summary>
        /// <param name="entityId">Block entity id.</param>
        /// <param name="part">Emissive material name, null or "" = "Emissive".</param>
        /// <param name="color">Colour.</param>
        /// <param name="emissivity">Brightness 0 to 1.</param>
        /// <param name="blink">Blink period in seconds (half on, half off), 0 = steady.</param>
        public void Set(long entityId, string part, Color color, float emissivity, float blink)
        {
            part = string.IsNullOrEmpty(part) ? "Emissive" : part;
            emissivity = MathHelper.Clamp(emissivity, 0f, 1f);
            blink = blink > 0f ? Math.Max(0.2f, Math.Min(10f, blink)) : 0f;
            Store(entityId, part, color, emissivity, blink);
            if (_core.IsServer)
                _core.Net.ToAll(SetPacket(entityId, part, color, emissivity, blink));
        }

        /// <summary>Gives a part (null = all parts of the block) back to the game.</summary>
        /// <param name="entityId">Block entity id.</param>
        /// <param name="part">Emissive material name, null = all.</param>
        public void Clear(long entityId, string part)
        {
            Remove(entityId, part);
            if (_core.IsServer)
                _core.Net.ToAll(new Packet { Kind = PacketKind.Lights, EntityId = entityId, Texts = new[] { part ?? "" } });
        }

        private static Packet SetPacket(long entityId, string part, Color color, float emissivity, float blink)
        {
            return new Packet
            {
                Kind = PacketKind.Lights,
                EntityId = entityId,
                Texts = new[]
                {
                    part, color.PackedValue.ToString(CultureInfo.InvariantCulture),
                    emissivity.ToString("R", CultureInfo.InvariantCulture), blink.ToString("R", CultureInfo.InvariantCulture),
                },
            };
        }

        private void Store(long entityId, string part, Color color, float emissivity, float blink)
        {
            Dictionary<string, State> parts;
            if (!_states.TryGetValue(entityId, out parts))
                _states[entityId] = parts = new Dictionary<string, State>();
            parts[part] = new State { Color = color, Emissivity = emissivity, Blink = blink };
        }

        private void Remove(long entityId, string part)
        {
            Dictionary<string, State> parts;
            if (!_states.TryGetValue(entityId, out parts))
                return;
            if (string.IsNullOrEmpty(part))
                parts.Clear();
            else
                parts.Remove(part);
            if (parts.Count == 0)
                _states.Remove(entityId);
            else
                foreach (var s in parts.Values)
                    s.Phase = -1;       // paint the remaining parts again after the game painted the block
            if (_core.IsPlayer)
                GiveBack(entityId);
        }

        /// <summary>Network packet (Lights to players, LightsRequest to the server).</summary>
        public void OnPacket(Packet p, ulong sender)
        {
            if (p.Kind == PacketKind.LightsRequest)
            {
                if (!_core.IsServer)
                    return;
                foreach (var pair in _states)
                    foreach (var part in pair.Value)
                        _core.Net.ToPlayer(SetPacket(pair.Key, part.Key, part.Value.Color, part.Value.Emissivity, part.Value.Blink), sender);
                return;
            }
            if (_core.IsServer || p.Texts == null || p.Texts.Length == 0)
                return;     // the server stored it already when it was set
            if (p.Texts.Length < 4)
            {
                Remove(p.EntityId, p.Texts[0]);
                return;
            }
            uint packed;
            float emissivity, blink;
            if (!uint.TryParse(p.Texts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out packed)
                || !float.TryParse(p.Texts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out emissivity)
                || !float.TryParse(p.Texts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out blink))
                return;
            Store(p.EntityId, string.IsNullOrEmpty(p.Texts[0]) ? "Emissive" : p.Texts[0], new Color { PackedValue = packed }, emissivity, blink);
        }

        /// <summary>Every frame.</summary>
        public void Tick()
        {
            if (_states.Count == 0 || ++_tick % Interval != 0)
                return;
            if (_core.IsServer && _tick % 600 == 0)
                DropClosed();
            if (!_core.IsPlayer)
                return;
            var camera = MyAPIGateway.Session?.Camera;
            if (camera == null)
                return;
            var eye = camera.WorldMatrix.Translation;
            double now = MyAPIGateway.Session.ElapsedPlayTime.TotalSeconds;
            bool reassert = _tick % Reassert == 0;
            foreach (var pair in _states)
            {
                var block = MyAPIGateway.Entities.GetEntityById(pair.Key) as IMyCubeBlock;
                if (block == null || block.Closed)
                    continue;       // not streamed in (yet): applied when it is there
                if (Vector3D.DistanceSquared(eye, block.WorldMatrix.Translation) > ApplyDistance * ApplyDistance)
                {
                    foreach (var s in pair.Value.Values)
                        s.Phase = -1;
                    continue;
                }
                foreach (var part in pair.Value)
                {
                    var s = part.Value;
                    int phase = s.Blink <= 0f || now % s.Blink < s.Blink * 0.5 ? 1 : 0;
                    if (phase == s.Phase && !reassert)
                        continue;
                    s.Phase = phase;
                    var color = phase == 1 ? s.Color : Color.Black;
                    float emissivity = phase == 1 ? s.Emissivity : 0f;
                    block.SetEmissiveParts(part.Key, color, emissivity);
                    block.SetEmissivePartsForSubparts(part.Key, color, emissivity);
                }
            }
        }

        private void DropClosed()
        {
            _gone.Clear();
            foreach (var id in _states.Keys)
                if (MyAPIGateway.Entities.GetEntityById(id) == null)
                    _gone.Add(id);
            foreach (var id in _gone)
            {
                _states.Remove(id);
                _core.Net.RawToOthers(new Packet { Kind = PacketKind.Lights, EntityId = id, Texts = new[] { "" } });
            }
        }

        private static void GiveBack(long entityId)
        {
            var block = MyAPIGateway.Entities.GetEntityById(entityId) as MyCubeBlock;
            if (block == null || block.Closed)
                return;
            try
            {
                if (block.UsesEmissivePreset)
                    block.CheckEmissiveState(true);
            }
            catch (Exception e)
            {
                Log.Error("block lights", e);
            }
        }
    }
}
