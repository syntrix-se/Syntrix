// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Syntrix.Gui
{
    /// <summary>
    /// Drawing helpers on the WF3 screen space API. Layout and drawing work in screen pixels; sizes from mods are
    /// reference pixels (1080 lines), multiplied by <see cref="K"/>. Text goes through the normalised API, so the
    /// font scale behaves like Vanilla on every resolution.
    /// </summary>
    public static class GuiDraw
    {
        /// <summary>White square (Textures\GUI\Blank.dds as LCD sprite), base of all plain areas.</summary>
        public static readonly MyStringId White = MyStringId.GetOrCompute("SquareSimple");

        /// <summary>Screen pixels per reference pixel (GUI height / 1080).</summary>
        public static float K = 1f;

        /// <summary>Layer used while drawing.</summary>
        public static MyScreenSpaceLayer Layer = MyScreenSpaceLayer.BelowHud;

        /// <summary>Current transparency factor (fade in and out of windows).</summary>
        public static float Alpha = 1f;

        /// <summary>Current clip rectangle in screen pixels (empty width = none).</summary>
        public static RectangleF Clip;

        private static readonly Dictionary<string, MyStringId> Ids = new Dictionary<string, MyStringId>();
        private static readonly Dictionary<string, bool> Available = new Dictionary<string, bool>();
        private static readonly StringBuilder Sb = new StringBuilder();

        /// <summary>Refreshes <see cref="K"/> from the current GUI size.</summary>
        public static void UpdateScale()
        {
            float h = MyScreenSpace.NormalizedSizeToPixels(new Vector2(0f, 1f)).Y;
            K = h > 1f ? h / 1080f : 1f;
        }

        /// <summary>Material id, created once per name (MyStringId never frees).</summary>
        public static MyStringId Id(string name)
        {
            MyStringId id;
            if (name == null)
                name = "";
            if (!Ids.TryGetValue(name, out id))
            {
                id = MyStringId.GetOrCompute(name);
                Ids[name] = id;
            }
            return id;
        }

        /// <summary>True if the material can be drawn (cached).</summary>
        public static bool Exists(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            bool ok;
            if (!Available.TryGetValue(name, out ok))
            {
                try
                {
                    ok = MyScreenSpaceSprite.IsMaterialAvailable(Id(name));
                }
                catch (Exception)
                {
                    ok = false;
                }
                Available[name] = ok;
            }
            return ok;
        }

        /// <summary>Forgets the material cache (after reloading).</summary>
        public static void ResetCache()
        {
            Available.Clear();
        }

        /// <summary>Colour with the current alpha.</summary>
        public static Color A(Color c)
        {
            return c * Alpha;
        }

        private static Rectangle R(RectangleF r)
        {
            int x = (int)Math.Round(r.X), y = (int)Math.Round(r.Y);
            int x2 = (int)Math.Round(r.X + r.Width), y2 = (int)Math.Round(r.Y + r.Height);
            return new Rectangle(x, y, Math.Max(0, x2 - x), Math.Max(0, y2 - y));
        }

        /// <summary>Plain rectangle.</summary>
        public static void Rect(RectangleF r, Color c)
        {
            if (c.A == 0 || r.Width <= 0f || r.Height <= 0f)
                return;
            MyScreenSpaceSprite.DrawPixels(White, R(r), null, A(c), 0f, Layer);
        }

        /// <summary>Frame of the given width (inside the rectangle).</summary>
        public static void Border(RectangleF r, Color c, float width)
        {
            if (c.A == 0 || width <= 0f)
                return;
            Rect(new RectangleF(r.X, r.Y, r.Width, width), c);
            Rect(new RectangleF(r.X, r.Y + r.Height - width, r.Width, width), c);
            Rect(new RectangleF(r.X, r.Y + width, width, r.Height - 2f * width), c);
            Rect(new RectangleF(r.X + r.Width - width, r.Y + width, width, r.Height - 2f * width), c);
        }

        /// <summary>Texture or material stretched into the rectangle.</summary>
        public static void Texture(string material, RectangleF r, Color c)
        {
            if (string.IsNullOrEmpty(material) || r.Width <= 0f || r.Height <= 0f)
                return;
            MyScreenSpaceSprite.DrawPixels(Id(material), R(r), null, A(c), 0f, Layer);
        }

        /// <summary>Texture or material, rotated around its centre.</summary>
        public static void TextureRotated(string material, RectangleF r, Color c, float rotation)
        {
            if (string.IsNullOrEmpty(material))
                return;
            var rect = R(r);
            MyScreenSpaceSprite.DrawPivoted(Id(material), rect, null, A(c), rotation, new Vector2(rect.Width / 2f, rect.Height / 2f), Layer);
        }

        /// <summary>Texture that keeps its aspect ratio inside the rectangle (centred).</summary>
        public static void TextureFit(string material, RectangleF r, Color c)
        {
            Vector2 size;
            try
            {
                size = MyScreenSpaceSprite.GetTextureSize(Id(material));
            }
            catch (Exception)
            {
                size = Vector2.Zero;
            }
            if (size.X <= 0f || size.Y <= 0f)
            {
                Texture(material, r, c);
                return;
            }
            float s = Math.Min(r.Width / size.X, r.Height / size.Y);
            float w = size.X * s, h = size.Y * s;
            Texture(material, new RectangleF(r.X + (r.Width - w) / 2f, r.Y + (r.Height - h) / 2f, w, h), c);
        }

        /// <summary>
        /// Vanilla nine-part texture "prefix_left_top" … "prefix_right_bottom" with corners of the given size
        /// (reference pixels); center is the name of the middle part.
        /// </summary>
        public static void Nine(string prefix, string center, RectangleF r, Color c, float corner)
        {
            float k = corner * K;
            if (r.Width < 2f * k || r.Height < 2f * k)
            {
                Texture(center, r, c);
                return;
            }
            float x1 = r.X + k, x2 = r.X + r.Width - k, y1 = r.Y + k, y2 = r.Y + r.Height - k;
            Texture(prefix + "_left_top", new RectangleF(r.X, r.Y, k, k), c);
            Texture(prefix + "_center_top", new RectangleF(x1, r.Y, x2 - x1, k), c);
            Texture(prefix + "_right_top", new RectangleF(x2, r.Y, k, k), c);
            Texture(prefix + "_left_center", new RectangleF(r.X, y1, k, y2 - y1), c);
            Texture(center, new RectangleF(x1, y1, x2 - x1, y2 - y1), c);
            Texture(prefix + "_right_center", new RectangleF(x2, y1, k, y2 - y1), c);
            Texture(prefix + "_left_bottom", new RectangleF(r.X, y2, k, k), c);
            Texture(prefix + "_center_bottom", new RectangleF(x1, y2, x2 - x1, k), c);
            Texture(prefix + "_right_bottom", new RectangleF(x2, y2, k, k), c);
        }

        /// <summary>Vanilla three-part texture (left, centre, right) with end parts of the given width in reference pixels.</summary>
        public static void Three(string left, string center, string right, RectangleF r, Color c, float end)
        {
            float k = Math.Min(end * K * r.Height / Math.Max(1f, 48f * K), r.Width / 2f);
            Texture(left, new RectangleF(r.X, r.Y, k, r.Height), c);
            Texture(center, new RectangleF(r.X + k, r.Y, r.Width - 2f * k, r.Height), c);
            Texture(right, new RectangleF(r.X + r.Width - k, r.Y, k, r.Height), c);
        }

        /// <summary>Three-part vertical texture (top, centre, bottom).</summary>
        public static void ThreeV(string top, string center, string bottom, RectangleF r, Color c, float end)
        {
            float k = Math.Min(end * K, r.Height / 2f);
            Texture(top, new RectangleF(r.X, r.Y, r.Width, k), c);
            Texture(center, new RectangleF(r.X, r.Y + k, r.Width, r.Height - 2f * k), c);
            Texture(bottom, new RectangleF(r.X, r.Y + r.Height - k, r.Width, k), c);
        }

        // ---------- text ----------

        /// <summary>Size of a text in screen pixels.</summary>
        public static Vector2 Measure(string font, string text, float scale)
        {
            if (string.IsNullOrEmpty(text))
                return new Vector2(0f, LineHeight(font, scale));
            Vector2 n = MyScreenSpaceText.Measure(font, text, scale);
            return MyScreenSpace.NormalizedSizeToPixels(n);
        }

        /// <summary>Width of a text in screen pixels.</summary>
        public static float Width(string font, string text, float scale)
        {
            if (string.IsNullOrEmpty(text))
                return 0f;
            return MyScreenSpace.NormalizedSizeToPixels(MyScreenSpaceText.Measure(font, text, scale)).X;
        }

        /// <summary>Line height in screen pixels.</summary>
        public static float LineHeight(string font, float scale)
        {
            return MyScreenSpace.NormalizedSizeToPixels(new Vector2(0f, MyScreenSpaceText.GetLineHeight(font, scale))).Y;
        }

        /// <summary>Text with its top left corner at a screen pixel position.</summary>
        public static void Text(string font, string text, Vector2 px, float scale, Color c)
        {
            if (string.IsNullOrEmpty(text) || c.A == 0)
                return;
            MyScreenSpaceText.Draw(font, text, MyScreenSpace.PixelsToNormalized(px), scale, A(c), MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP, float.PositiveInfinity, Layer);
        }

        /// <summary>One line of text inside a rectangle: align 0 left, 1 centre, 2 right; vertically centred. Cut with "…" if too long.</summary>
        public static void TextIn(string font, string text, RectangleF r, float scale, Color c, int align)
        {
            if (string.IsNullOrEmpty(text))
                return;
            text = Fit(font, text, scale, r.Width);
            float w = Width(font, text, scale);
            float h = LineHeight(font, scale);
            float x = align == 1 ? r.X + (r.Width - w) / 2f : align == 2 ? r.X + r.Width - w : r.X;
            Text(font, text, new Vector2(x, r.Y + (r.Height - h) / 2f), scale, c);
        }

        /// <summary>Text cut to a width with "…" at the end.</summary>
        public static string Fit(string font, string text, float scale, float width)
        {
            if (string.IsNullOrEmpty(text) || width <= 0f || Width(font, text, scale) <= width)
                return text;
            int lo = 0, hi = text.Length;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                if (Width(font, text.Substring(0, mid) + "…", scale) <= width)
                    lo = mid;
                else
                    hi = mid - 1;
            }
            return lo > 0 ? text.Substring(0, lo).TrimEnd() + "…" : "";
        }

        // ---------- clipping ----------

        /// <summary>Intersection of two rectangles (empty if they do not overlap).</summary>
        public static RectangleF Intersect(RectangleF a, RectangleF b)
        {
            float x1 = Math.Max(a.X, b.X), y1 = Math.Max(a.Y, b.Y);
            float x2 = Math.Min(a.X + a.Width, b.X + b.Width), y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);
            return new RectangleF(x1, y1, Math.Max(0f, x2 - x1), Math.Max(0f, y2 - y1));
        }

        /// <summary>True if the point lies inside the rectangle.</summary>
        public static bool Inside(RectangleF r, Vector2 p)
        {
            return p.X >= r.X && p.X < r.X + r.Width && p.Y >= r.Y && p.Y < r.Y + r.Height;
        }

        /// <summary>Starts clipping to a rectangle (intersected with the current clip); dispose to end it.</summary>
        public static IDisposable PushClip(RectangleF r)
        {
            var previous = Clip;
            var next = previous.Width > 0f || previous.Height > 0f ? Intersect(previous, r) : r;
            Clip = next;
            return new ClipScope(previous, MyScreenSpace.UsingScissorRectanglePixels(R(next), Layer));
        }

        private sealed class ClipScope : IDisposable
        {
            private readonly RectangleF _previous;
            private IDisposable _scope;

            /// <summary>Remembers the previous clip and the game's scissor scope.</summary>
            public ClipScope(RectangleF previous, IDisposable scope)
            {
                _previous = previous;
                _scope = scope;
            }

            /// <summary>Ends the scissor scope and restores the previous clip.</summary>
            public void Dispose()
            {
                if (_scope != null)
                {
                    _scope.Dispose();
                    _scope = null;
                }
                Clip = _previous;
            }
        }

        /// <summary>Parses "#RRGGBB" or "#RRGGBBAA"; false if not a colour.</summary>
        public static bool ParseColor(string s, out Color c)
        {
            c = Color.White;
            if (string.IsNullOrEmpty(s))
                return false;
            s = s.Trim();
            if (s.StartsWith("#"))
                s = s.Substring(1);
            if (s.Length != 6 && s.Length != 8)
                return GuiStyle.Named(s, out c);
            uint v;
            if (!uint.TryParse(s, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out v))
                return false;
            if (s.Length == 6)
                c = new Color((int)((v >> 16) & 255), (int)((v >> 8) & 255), (int)(v & 255), 255);
            else
                c = new Color((int)((v >> 24) & 255), (int)((v >> 16) & 255), (int)((v >> 8) & 255), (int)(v & 255));
            return true;
        }

        /// <summary>Colour as "#RRGGBB".</summary>
        public static string Hex(Color c)
        {
            return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2");
        }
    }
}
