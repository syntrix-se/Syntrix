// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System.Collections.Generic;
using RichHudFramework;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using VRageMath;

namespace Syntrix.Ui
{
    /// <summary>
    /// Custom drawing of the UI service: line strips, regular polygons, rings and triangle meshes.
    /// Points are relative to the element's centre, in the units of its HUD space.
    /// </summary>
    public sealed class ShapeElement : HudElementBase
    {
        private readonly UiKind _kind;
        private readonly List<Vector2> _points = new List<Vector2>();
        private readonly List<int> _indices = new List<int>();
        private readonly List<Vector2> _absolute = new List<Vector2>();
        private readonly PolyBoard _polygon;
        private readonly PuncturedPolyBoard _ring;
        private Color _color = Color.White;
        private Material _material = Material.Default;

        /// <summary>Line thickness.</summary>
        public float Thickness = 2f;

        /// <summary>Line: connect the last point with the first.</summary>
        public bool Closed;

        /// <summary>Makes a shape element of the given kind (Line, Polygon, Ring or Mesh).</summary>
        /// <param name="kind">Shape kind.</param>
        public ShapeElement(UiKind kind) : base(null)
        {
            _kind = kind;
            if (kind == UiKind.Ring)
                _polygon = _ring = new PuncturedPolyBoard { Sides = 32, InnerRadius = 0.5f };
            else if (kind == UiKind.Polygon)
                _polygon = new PolyBoard { Sides = 32 };
            Size = new Vector2(64f, 64f);
        }

        /// <summary>Colour of the shape.</summary>
        public Color Color
        {
            get { return _color; }
            set
            {
                _color = value;
                if (_polygon != null)
                    _polygon.Color = value;
            }
        }

        /// <summary>Texture of the shape.</summary>
        public Material Material
        {
            get { return _material; }
            set
            {
                _material = value ?? Material.Default;
                if (_polygon != null)
                    _polygon.Material = _material;
            }
        }

        /// <summary>Polygon and ring: number of sides.</summary>
        public int Sides
        {
            get { return _polygon != null ? _polygon.Sides : 0; }
            set { if (_polygon != null) _polygon.Sides = MathHelper.Clamp(value, 3, 256); }
        }

        /// <summary>Ring: inner radius as part of the size (0..1).</summary>
        public float InnerRadius
        {
            get { return _ring != null ? _ring.InnerRadius : 0f; }
            set { if (_ring != null) _ring.InnerRadius = MathHelper.Clamp(value, 0f, 1f); }
        }

        /// <summary>Replaces points and (for meshes) triangle indices.</summary>
        /// <param name="points">Points relative to the centre; null clears.</param>
        /// <param name="indices">Mesh: three indices per triangle; ignored for lines.</param>
        public void SetPoints(Vector2[] points, int[] indices)
        {
            _points.Clear();
            _indices.Clear();
            if (points != null)
                _points.AddRange(points);
            if (indices != null)
                foreach (int i in indices)
                    if (i >= 0 && i < _points.Count)
                        _indices.Add(i);
            while (_indices.Count % 3 != 0)
                _indices.RemoveAt(_indices.Count - 1);
        }

        /// <inheritdoc/>
        protected override void Draw()
        {
            if (_color.A == 0)
                return;
            var matrix = HudSpace.PlaneToWorldRef;
            if (_polygon != null)
            {
                _polygon.Draw(UnpaddedSize, Position, matrix);
                return;
            }
            if (_kind == UiKind.Mesh)
            {
                if (_indices.Count < 3)
                    return;
                _absolute.Clear();
                foreach (var p in _points)
                    _absolute.Add(Position + p);
                var poly = new PolyMaterial
                {
                    textureID = _material.TextureID,
                    bbColor = _color.GetBbColor(),
                    texBounds = new BoundingBox2(Vector2.Zero, Vector2.One),
                };
                BillBoardUtils.AddTriangles(_indices, _absolute, ref poly, matrix);
                return;
            }
            if (_points.Count < 2)
                return;
            var mat = new BoundedQuadMaterial
            {
                textureID = _material.TextureID,
                bbColor = _color.GetBbColor(),
                texBounds = new BoundingBox2(Vector2.Zero, Vector2.One),
            };
            int segments = Closed && _points.Count > 2 ? _points.Count : _points.Count - 1;
            float half = Thickness * 0.5f;
            for (int i = 0; i < segments; i++)
            {
                var a = Position + _points[i];
                var b = Position + _points[(i + 1) % _points.Count];
                var dir = b - a;
                float length = dir.Length();
                if (length < 0.001f)
                    continue;
                var normal = new Vector2(-dir.Y, dir.X) * (half / length);
                var quad = new FlatQuad(b + normal, b - normal, a - normal, a + normal);
                BillBoardUtils.AddQuad(ref quad, ref mat, matrix, MaskingBox);
            }
        }
    }
}
