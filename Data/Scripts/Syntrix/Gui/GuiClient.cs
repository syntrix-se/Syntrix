// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRageMath;

namespace Syntrix.Gui
{
    /// <summary>Elements of one docked mod, addressed by handle.</summary>
    public sealed class GuiClient
    {
        /// <summary>Name of the docked mod.</summary>
        public readonly string ModName;
        private readonly GuiService _service;
        private readonly Dictionary<int, GuiElement> _elements = new Dictionary<int, GuiElement>();
        private int _next = 1;
        private Action<int, int, int, string> _handler;

        /// <summary>Creates the client of one mod.</summary>
        public GuiClient(GuiService service, string modName)
        {
            _service = service;
            ModName = modName;
        }

        /// <summary>Sets the mod's event handler (handle, event, index, text).</summary>
        public void SetHandler(Action<int, int, int, string> handler)
        {
            _handler = handler;
        }

        /// <summary>Passes an event of an element to the mod's handler; errors in the handler are logged.</summary>
        public void Dispatch(int handle, int ev, int index, string text)
        {
            if (_handler == null || !_elements.ContainsKey(handle))
                return;
            try
            {
                _handler(handle, ev, index, text);
            }
            catch (Exception e)
            {
                Log.Error("GUI event handler of " + ModName, e);
            }
        }

        /// <summary>Element of this client by handle, null if unknown.</summary>
        public GuiElement Get(int handle)
        {
            GuiElement e;
            return _elements.TryGetValue(handle, out e) ? e : null;
        }

        /// <summary>Creates an element of the given kind below a parent (0 = none); returns its handle, 0 on failure.</summary>
        public int Create(int kind, int parent)
        {
            if (!_service.Usable)
                return 0;
            var e = GuiService.Make(kind);
            if (e == null)
            {
                Log.Warn(ModName + ": unknown element kind " + kind);
                return 0;
            }
            e.Owner = this;
            e.Handle = _next++;
            _elements[e.Handle] = e;
            var overlay = e as GuiOverlay;
            if (overlay != null)
                _service.AddOverlay(overlay);
            else if (!(e is GuiWindow) && parent > 0)
            {
                var p = Get(parent);
                if (p != null)
                    p.Add(e);
            }
            return e.Handle;
        }

        /// <summary>Removes an element and all its children.</summary>
        public void Remove(int handle)
        {
            var e = Get(handle);
            if (e == null)
                return;
            var w = e.Root as GuiWindow;
            if (w != null && w != e)
                w.Forget(e);
            if (e.Parent != null)
                e.Parent.Remove(e);
            Drop(e);
        }

        private void Drop(GuiElement e)
        {
            foreach (var c in e.Children.ToArray())
                Drop(c);
            if (e.Owner == this)
                _elements.Remove(e.Handle);
            var overlay = e as GuiOverlay;
            if (overlay != null)
                _service.RemoveOverlay(overlay);
            try { e.Destroyed(); }
            catch (Exception x) { Log.Error("GUI remove", x); }
            e.Owner = null;
            e.Handler = null;
        }

        /// <summary>Removes all children of an element.</summary>
        public void Clear(int handle)
        {
            var e = Get(handle);
            if (e == null)
                return;
            foreach (var c in e.Children.ToArray())
                Remove(c.Handle);
        }

        /// <summary>Moves an element below another one (parent 0 = detach); windows and overlays cannot be moved.</summary>
        public void SetParent(int handle, int parent)
        {
            var e = Get(handle);
            var p = Get(parent);
            if (e == null || e is GuiWindow || e is GuiOverlay)
                return;
            var w = e.Root as GuiWindow;
            if (w != null && w != e)
                w.Forget(e);
            if (p == null)
            {
                if (e.Parent != null)
                    e.Parent.Remove(e);
                return;
            }
            for (var x = p; x != null; x = x.Parent)
                if (x == e)
                    return;
            p.Add(e);
        }

        /// <summary>Drops all top-level elements of this client and its handler (mod unloads).</summary>
        public void Release()
        {
            foreach (var e in new List<GuiElement>(_elements.Values))
                if (e.Parent == null && e.Owner == this)
                    Drop(e);
            _elements.Clear();
            _handler = null;
        }

        // ---------- properties ----------

        /// <summary>Sets a bool property of an element; unknown handles are ignored.</summary>
        public void SetBool(int h, int prop, bool v) { var e = Get(h); if (e != null) e.SetBool(prop, v); }
        /// <summary>Sets an int property of an element; unknown handles are ignored.</summary>
        public void SetInt(int h, int prop, int v) { var e = Get(h); if (e != null) e.SetInt(prop, v); }
        /// <summary>Sets a float property of an element; unknown handles are ignored.</summary>
        public void SetFloat(int h, int prop, float v) { var e = Get(h); if (e != null) e.SetFloat(prop, v); }
        /// <summary>Sets a vector property of an element; unknown handles are ignored.</summary>
        public void SetVector(int h, int prop, Vector2 v) { var e = Get(h); if (e != null) e.SetVector(prop, v); }
        /// <summary>Sets a colour property of an element; unknown handles are ignored.</summary>
        public void SetColor(int h, int prop, Color v) { var e = Get(h); if (e != null) e.SetColor(prop, v); }
        /// <summary>Sets a text property of an element; unknown handles are ignored.</summary>
        public void SetText(int h, int prop, string v) { var e = Get(h); if (e != null) e.SetText(prop, v); }

        /// <summary>Reads a bool property; false if unknown.</summary>
        public bool GetBool(int h, int prop) { var e = Get(h); bool v; return e != null && e.GetBool(prop, out v) && v; }
        /// <summary>Reads an int property; 0 if unknown.</summary>
        public int GetInt(int h, int prop) { var e = Get(h); int v; return e != null && e.GetInt(prop, out v) ? v : 0; }
        /// <summary>Reads a float property; 0 if unknown.</summary>
        public float GetFloat(int h, int prop) { var e = Get(h); float v; return e != null && e.GetFloat(prop, out v) ? v : 0f; }
        /// <summary>Reads a vector property; zero if unknown.</summary>
        public Vector2 GetVector(int h, int prop) { var e = Get(h); Vector2 v; return e != null && e.GetVector(prop, out v) ? v : Vector2.Zero; }
        /// <summary>Reads a colour property; transparent if unknown.</summary>
        public Color GetColor(int h, int prop) { var e = Get(h); Color v; return e != null && e.GetColor(prop, out v) ? v : Color.Transparent; }
        /// <summary>Reads a text property; null if unknown.</summary>
        public string GetText(int h, int prop) { var e = Get(h); string v; return e != null && e.GetText(prop, out v) ? v : null; }

        // ---------- items ----------

        /// <summary>Adds an item to a list, tree, tabs or drop-down; returns its index, -1 on failure.</summary>
        public int ItemAdd(int h, string key, string parent, string text, string icons) { var e = Get(h); return e != null ? e.ItemAdd(key, parent, text, icons) : -1; }
        /// <summary>Sets a property of one item.</summary>
        public void ItemSet(int h, int index, int prop, string value) { var e = Get(h); if (e != null) e.ItemSet(index, prop, value); }
        /// <summary>Removes one item.</summary>
        public void ItemRemove(int h, int index) { var e = Get(h); if (e != null) e.ItemRemove(index); }
        /// <summary>Removes all items.</summary>
        public void ItemClear(int h) { var e = Get(h); if (e != null) e.ItemClear(); }
        /// <summary>Number of items, 0 if unknown.</summary>
        public int ItemCount(int h) { var e = Get(h); return e != null ? e.ItemCount : 0; }
        /// <summary>Index of the item with the key, -1 if none.</summary>
        public int ItemFind(int h, string key) { var e = Get(h); return e != null ? e.ItemFind(key) : -1; }

        // ---------- windows ----------

        /// <summary>Opens a window; false if the handle is no window or it could not open.</summary>
        public bool Open(int h)
        {
            var w = Get(h) as GuiWindow;
            return w != null && w.Open();
        }

        /// <summary>Closes a window.</summary>
        public void Close(int h)
        {
            var w = Get(h) as GuiWindow;
            if (w != null)
                w.Close();
        }

        /// <summary>Gives an element the keyboard focus within its window.</summary>
        public void Focus(int h)
        {
            var e = Get(h);
            if (e == null)
                return;
            var w = e.Root as GuiWindow;
            if (w != null)
                w.SetFocus(e);
        }

        /// <summary>Sets the draw callback of a canvas: (x, y, width, height in screen pixels), alpha.</summary>
        public void SetDraw(int h, Action<Vector4, float> draw)
        {
            var c = Get(h) as GuiCanvas;
            if (c != null)
                c.DrawCallback = draw;
        }

        /// <summary>Expands the parents of a tree item so it is visible.</summary>
        public void Expand(int h, int index)
        {
            var t = Get(h) as GuiTree;
            if (t != null)
                t.Reveal(index);
        }

        /// <summary>Scrolls a scroll area so that the target element is visible.</summary>
        public void ScrollTo(int h, int target)
        {
            var e = Get(h);
            var s = e as GuiScroll;
            var t = Get(target);
            if (s != null && t != null)
                s.ScrollTo(t);
        }
    }
}
