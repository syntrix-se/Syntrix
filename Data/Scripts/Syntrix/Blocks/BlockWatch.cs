// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.ModAPI;

namespace Syntrix.Blocks
{
    /// <summary>
    /// Watches all grids of the world and reports added blocks (API 4, used by the screen and admin block services).
    /// Started only when a service needs it; blocks that exist at that time are reported once as well.
    /// </summary>
    public sealed class BlockWatch
    {
        private readonly HashSet<long> _grids = new HashSet<long>();
        private readonly List<Action<IMySlimBlock>> _added = new List<Action<IMySlimBlock>>();
        private readonly List<IMySlimBlock> _blocks = new List<IMySlimBlock>();
        private bool _started;

        /// <summary>Runs for every block in the world now and for every block added later (also blocks of grids that enter
        /// the world).</summary>
        public void OnAdded(Action<IMySlimBlock> handler)
        {
            if (handler == null)
                return;
            _added.Add(handler);
            if (_started)
                ForEach(handler);
            else
                Start();
        }

        /// <summary>Runs the action for every block in the world now.</summary>
        public void ForEach(Action<IMySlimBlock> action)
        {
            var grids = new List<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(null, e =>
            {
                if (e is IMyCubeGrid)
                    grids.Add(e);
                return false;
            });
            foreach (var e in grids)
            {
                _blocks.Clear();
                ((IMyCubeGrid)e).GetBlocks(_blocks);
                foreach (var b in _blocks)
                {
                    try { action(b); }
                    catch (Exception ex) { Log.Error("block watch", ex); }
                }
            }
            _blocks.Clear();
        }

        private void Start()
        {
            if (_started)
                return;
            _started = true;
            var all = new List<IMyEntity>();
            MyAPIGateway.Entities.GetEntities(null, e =>
            {
                if (e is IMyCubeGrid)
                    all.Add(e);
                return false;
            });
            foreach (var e in all)
                Track(e);
            MyAPIGateway.Entities.OnEntityAdd += Track;
        }

        /// <summary>Unhooks everything.</summary>
        public void Stop()
        {
            if (_started)
                MyAPIGateway.Entities.OnEntityAdd -= Track;
            _started = false;
            _added.Clear();
            _grids.Clear();
        }

        private void Track(IMyEntity e)
        {
            var grid = e as IMyCubeGrid;
            if (grid == null || !_grids.Add(grid.EntityId))
                return;
            grid.OnBlockAdded += Added;
            grid.OnClose += Closed;
            // blocks of a grid that enters the world (pasted, spawned, streamed in) count as added
            if (_started && _added.Count > 0)
            {
                _blocks.Clear();
                grid.GetBlocks(_blocks);
                var list = _blocks.ToArray();
                _blocks.Clear();
                foreach (var b in list)
                    Added(b);
            }
        }

        private void Closed(IMyEntity e)
        {
            var grid = e as IMyCubeGrid;
            if (grid == null)
                return;
            grid.OnBlockAdded -= Added;
            grid.OnClose -= Closed;
            _grids.Remove(grid.EntityId);
        }

        private void Added(IMySlimBlock block)
        {
            foreach (var h in _added)
            {
                try { h(block); }
                catch (Exception ex) { Log.Error("block watch", ex); }
            }
        }

        /// <summary>Key "TypeId/SubtypeId" of a block (TypeId without MyObjectBuilder_).</summary>
        public static string KeyOf(IMySlimBlock block)
        {
            var id = block.BlockDefinition.Id;
            return id.TypeId.ToString().Replace("MyObjectBuilder_", "") + "/" + id.SubtypeName;
        }
    }
}
