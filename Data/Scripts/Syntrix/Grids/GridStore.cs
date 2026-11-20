// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using System.Globalization;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Syntrix.Grids
{
    /// <summary>
    /// Grid storage (API 4, server only): turns a mechanical grid group into a text and back. The text holds the
    /// blueprint (with the block storage of every block) relative to a reference frame and the outline of the group in
    /// that frame. The calling mod keeps the text itself (its own world storage) and removes the grids from the world.
    /// Spawning keeps the entity ids when all of them are free (states stored by block id stay valid), otherwise the
    /// whole collection gets new ids so that the links between subgrids stay consistent.
    /// </summary>
    public sealed class GridStore
    {
        /// <summary>First line of a grid text.</summary>
        public const string Header = "SyxGrid/1";

        /// <summary>Spawned, all ids kept.</summary>
        public const int SpawnedKeptIds = 0;
        /// <summary>Spawned with new ids.</summary>
        public const int SpawnedNewIds = 1;
        /// <summary>The text is no grid text.</summary>
        public const int Invalid = -1;
        /// <summary>The outline is blocked by a grid or a character.</summary>
        public const int Blocked = -2;
        /// <summary>Called on a client.</summary>
        public const int NotServer = -3;

        private readonly SyntrixCore _core;

        /// <summary>Makes the service.</summary>
        public GridStore(SyntrixCore core) { _core = core; }

        /// <summary>
        /// Main grids (most blocks) of the mechanical groups whose centre lies within the radius; without groups with a
        /// static grid, without grids without physics and without the group of <paramref name="exceptGrid"/>.
        /// </summary>
        public long[] Nearby(Vector3D centre, double radius, long exceptGrid)
        {
            var result = new List<long>();
            var seen = new HashSet<long>();
            var except = MyAPIGateway.Entities.GetEntityById(exceptGrid) as IMyCubeGrid;
            if (except != null)
                foreach (var g in Group(except))
                    seen.Add(g.EntityId);
            var sphere = new BoundingSphereD(centre, radius);
            foreach (var ent in MyAPIGateway.Entities.GetTopMostEntitiesInSphere(ref sphere))
            {
                var grid = ent as IMyCubeGrid;
                if (grid == null || grid.Physics == null || seen.Contains(grid.EntityId))
                    continue;
                var group = Group(grid);
                var box = BoundingBoxD.CreateInvalid();
                bool anyStatic = false;
                foreach (var g in group)
                {
                    seen.Add(g.EntityId);
                    box.Include(g.WorldAABB);
                    anyStatic |= g.IsStatic;
                }
                if (!anyStatic && Vector3D.Distance(box.Center, centre) <= radius)
                    result.Add(MainGrid(group).EntityId);
            }
            return result.ToArray();
        }

        /// <summary>The grid text of the group of a grid relative to the frame, null if the grid does not exist. The grids
        /// stay in the world: save the text first, then close the grids of the group.</summary>
        public string Capture(long gridId, MatrixD frame)
        {
            if (!_core.IsServer)
                return null;
            var grid = MyAPIGateway.Entities.GetEntityById(gridId) as IMyCubeGrid;
            if (grid == null || grid.Closed)
                return null;
            var inverse = MatrixD.Invert(frame);
            var grids = new List<MyObjectBuilder_CubeGrid>();
            var box = BoundingBoxD.CreateInvalid();
            var corners = new Vector3D[8];
            foreach (var g in Group(grid))
            {
                var ob = g.GetObjectBuilder(true) as MyObjectBuilder_CubeGrid;
                if (ob == null)
                    continue;
                var local = g.WorldMatrix * inverse;
                ob.PositionAndOrientation = new MyPositionAndOrientation(local);
                ob.LinearVelocity = new SerializableVector3(0, 0, 0);
                ob.AngularVelocity = new SerializableVector3(0, 0, 0);
                grids.Add(ob);
                new MyOrientedBoundingBoxD(g.LocalAABB, local).GetCorners(corners, 0);
                foreach (var c in corners)
                    box.Include(c);
            }
            if (grids.Count == 0)
                return null;
            string xml = MyAPIGateway.Utilities.SerializeToXML(new MyObjectBuilder_ShipBlueprintDefinition { CubeGrids = grids.ToArray() });
            return Header + "\n" + Vec(box.Min) + ";" + Vec(box.Max) + "\n" + xml;
        }

        /// <summary>Spawns a grid text relative to the frame. The group of <paramref name="exceptGrid"/> does not count as
        /// obstacle (e.g. the station). Returns <see cref="SpawnedKeptIds"/>, <see cref="SpawnedNewIds"/> or an error.</summary>
        public int Spawn(string text, MatrixD frame, long exceptGrid)
        {
            if (!_core.IsServer)
                return NotServer;
            BoundingBoxD outline;
            string xml;
            if (!Parse(text, out outline, out xml))
                return Invalid;
            var except = new List<IMyCubeGrid>();
            var stationGrid = MyAPIGateway.Entities.GetEntityById(exceptGrid) as IMyCubeGrid;
            if (stationGrid != null)
                except = Group(stationGrid);
            if (IsOccupied(new MyOrientedBoundingBoxD(outline, frame), except))
                return Blocked;
            MyObjectBuilder_ShipBlueprintDefinition blueprint;
            try { blueprint = MyAPIGateway.Utilities.SerializeFromXML<MyObjectBuilder_ShipBlueprintDefinition>(xml); }
            catch (Exception e)
            {
                Log.Error("grid text", e);
                return Invalid;
            }
            if (blueprint == null || blueprint.CubeGrids == null || blueprint.CubeGrids.Length == 0)
                return Invalid;
            foreach (var ob in blueprint.CubeGrids)
            {
                var local = ob.PositionAndOrientation.HasValue ? ob.PositionAndOrientation.Value.GetMatrix() : MatrixD.Identity;
                ob.PositionAndOrientation = new MyPositionAndOrientation(local * frame);
            }
            bool keep = IdsFree(blueprint.CubeGrids);
            if (!keep)
                MyAPIGateway.Entities.RemapObjectBuilderCollection(new List<MyObjectBuilder_EntityBase>(blueprint.CubeGrids));
            foreach (var ob in blueprint.CubeGrids)
                MyAPIGateway.Entities.CreateFromObjectBuilderParallel(ob, true);
            return keep ? SpawnedKeptIds : SpawnedNewIds;
        }

        /// <summary>True if the outline of the grid text at the frame is free.</summary>
        public bool Fits(string text, MatrixD frame, long exceptGrid)
        {
            BoundingBoxD outline;
            string xml;
            if (!Parse(text, out outline, out xml))
                return false;
            var stationGrid = MyAPIGateway.Entities.GetEntityById(exceptGrid) as IMyCubeGrid;
            return !IsOccupied(new MyOrientedBoundingBoxD(outline, frame), stationGrid != null ? Group(stationGrid) : new List<IMyCubeGrid>());
        }

        // ---------- helpers ----------

        /// <summary>Grids of the mechanical group.</summary>
        public static List<IMyCubeGrid> Group(IMyCubeGrid grid)
        {
            var list = new List<IMyCubeGrid>();
            grid.GetGridGroup(GridLinkTypeEnum.Mechanical).GetGrids(list);
            if (list.Count == 0)
                list.Add(grid);
            return list;
        }

        /// <summary>Grid with the most blocks.</summary>
        public static IMyCubeGrid MainGrid(List<IMyCubeGrid> group)
        {
            IMyCubeGrid best = group[0];
            var blocks = new List<IMySlimBlock>();
            int max = -1;
            foreach (var g in group)
            {
                blocks.Clear();
                g.GetBlocks(blocks);
                if (blocks.Count > max)
                {
                    max = blocks.Count;
                    best = g;
                }
            }
            return best;
        }

        private static string Vec(Vector3D v)
        {
            return v.X.ToString("R", CultureInfo.InvariantCulture) + ";" + v.Y.ToString("R", CultureInfo.InvariantCulture) + ";"
                + v.Z.ToString("R", CultureInfo.InvariantCulture);
        }

        private static bool Parse(string text, out BoundingBoxD outline, out string xml)
        {
            outline = BoundingBoxD.CreateInvalid();
            xml = null;
            if (string.IsNullOrEmpty(text) || !text.StartsWith(Header))
                return false;
            int a = text.IndexOf('\n') + 1;
            int b = a > 0 ? text.IndexOf('\n', a) : -1;
            if (b < 0)
                return false;
            var n = text.Substring(a, b - a).TrimEnd('\r').Split(';');
            if (n.Length != 6)
                return false;
            var v = new double[6];
            for (int i = 0; i < 6; i++)
                if (!double.TryParse(n[i], NumberStyles.Float, CultureInfo.InvariantCulture, out v[i]))
                    return false;
            outline = new BoundingBoxD(new Vector3D(v[0], v[1], v[2]), new Vector3D(v[3], v[4], v[5]));
            xml = text.Substring(b + 1);
            return true;
        }

        /// <summary>True when all grid ids and the set block ids are unique and free in the world.</summary>
        private static bool IdsFree(MyObjectBuilder_CubeGrid[] grids)
        {
            var ids = new HashSet<long>();
            foreach (var grid in grids)
            {
                if (grid.EntityId == 0 || !ids.Add(grid.EntityId) || MyAPIGateway.Entities.EntityExists(grid.EntityId))
                    return false;
                if (grid.CubeBlocks == null)
                    continue;
                foreach (var block in grid.CubeBlocks)
                    if (block.EntityId != 0 && (!ids.Add(block.EntityId) || MyAPIGateway.Entities.EntityExists(block.EntityId)))
                        return false;
            }
            return true;
        }

        private static bool IsOccupied(MyOrientedBoundingBoxD outline, List<IMyCubeGrid> except)
        {
            var box = outline.GetAABB();
            foreach (var ent in MyAPIGateway.Entities.GetTopMostEntitiesInBox(ref box))
            {
                var grid = ent as IMyCubeGrid;
                if (grid != null)
                {
                    if (except.Contains(grid) || grid.Physics == null)
                        continue;
                    var other = new MyOrientedBoundingBoxD(grid.LocalAABB, grid.WorldMatrix);
                    if (outline.Intersects(ref other))
                        return true;
                    continue;
                }
                var pos = ent.WorldMatrix.Translation;
                if (ent is IMyCharacter && outline.Contains(ref pos))
                    return true;
            }
            return false;
        }
    }
}
