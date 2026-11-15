// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using RichHudFramework.UI;
using RichHudFramework.UI.Client;
using VRageMath;

namespace Syntrix.Ui
{
    /// <summary>
    /// UI state of one docked mod: its elements (by handle), its event dispatcher and its key binds.
    /// Handles are index (low 20 bits) plus generation, so a stale handle never reaches a new element.
    /// </summary>
    public sealed class UiClient
    {
        private const int IndexMask = 0xFFFFF;

        public readonly string ModName;
        private readonly UiService _service;
        private readonly List<UiNode> _nodes = new List<UiNode> { null };
        private readonly List<int> _generations = new List<int> { 0 };
        private readonly Stack<int> _free = new Stack<int>();
        private readonly List<UiNode> _roots = new List<UiNode>();

        /// <summary>Receives (handle, event, argument) for every event of this mod.</summary>
        public Action<int, int, string> Dispatcher;

        /// <summary>Mod asked for the cursor outside windows.</summary>
        public bool WantsCursor;

        // Key binds
        private readonly List<string> _bindNames = new List<string>();
        private readonly List<string> _bindKeys = new List<string>();
        private readonly HashSet<string> _subscribed = new HashSet<string>();
        private IBindGroup _group;
        private bool _bindsLoaded;
        private BindDefinition[] _saved;
        private string _pageTitle;
        private bool _pageMade;

        public UiClient(UiService service, string modName)
        {
            _service = service;
            ModName = modName;
        }

        public void Queue(UiNode node, int ev, string arg) { _service.Queue(this, node.Handle, ev, arg); }

        public UiNode Node(int handle)
        {
            int i = handle & IndexMask;
            if (i <= 0 || i >= _nodes.Count)
                return null;
            var n = _nodes[i];
            return n != null && n.Handle == handle ? n : null;
        }

        // ---------------------------------------------------------------- elements

        public int Create(int kind, int parent)
        {
            if (!_service.Ready || kind == (int)UiKind.Part || kind < 1 || kind > (int)UiKind.Slider)
                return 0;
            UiNode p = null;
            if (parent > 0)
            {
                p = Node(parent);
                if (p == null)
                    return 0;
            }
            if (Full)
                return 0;
            var n = new UiNode(this, NewHandle(), (UiKind)kind);
            var e = UiNode.Make(n);
            n.Init(e);
            _nodes[n.Handle & IndexMask] = n;
            Attach(n, p, parent);
            if (n.Window != null)
                _service.AddWindow(n);
            return n.Handle;
        }

        private bool Full { get { return _free.Count == 0 && _nodes.Count > IndexMask; } }

        private int NewHandle()
        {
            int i;
            if (_free.Count > 0)
                i = _free.Pop();
            else
            {
                i = _nodes.Count;
                _nodes.Add(null);
                _generations.Add(0);
            }
            int g = (_generations[i] % 2047) + 1;
            _generations[i] = g;
            return i | (g << 20);
        }

        private void Attach(UiNode n, UiNode p, int parentHandle)
        {
            n.Parent = p;
            if (p == null)
            {
                n.Element.Register(parentHandle < 0 ? HudMain.Root : HudMain.HighDpiRoot);
                _roots.Add(n);
                return;
            }
            if (p.Children == null)
                p.Children = new List<UiNode>();
            p.Children.Add(n);
            if (p.Scroll != null)
            {
                p.Scroll.Add(n.Element);
                n.Entry = p.Scroll.Collection[p.Scroll.Count - 1];
            }
            else if (p.Chain != null)
            {
                p.Chain.Add(n.Element);
                n.InChain = true;
            }
            else
                n.Element.Register(p.ContentParent);
        }

        private void Detach(UiNode n)
        {
            var p = n.Parent;
            if (p == null)
            {
                _roots.Remove(n);
                n.Element.Unregister();
                return;
            }
            if (p.Children != null)
                p.Children.Remove(n);
            if (n.Entry != null && p.Scroll != null)
                p.Scroll.Remove(n.Entry);
            else if (n.InChain && p.Chain != null)
            {
                var e = n.Element;
                p.Chain.Remove(c => c.Element == e);
            }
            else
                n.Element.Unregister();
            n.Entry = null;
            n.InChain = false;
            n.Parent = null;
        }

        /// <summary>Frees the handles of a node and its subtree (the Rich HUD elements go with their parent).</summary>
        private void Free(UiNode n)
        {
            if (n.Children != null)
                foreach (var c in n.Children)
                    Free(c);
            if (n.Parts != null)
                foreach (var part in n.Parts)
                    if (part != null)
                        Free(part);
            n.Dead = true;
            if (n.Window != null)
                _service.RemoveWindow(n);
            int i = n.Handle & IndexMask;
            if (i < _nodes.Count && _nodes[i] == n)
            {
                _nodes[i] = null;
                _free.Push(i);
            }
        }

        public void Remove(int handle)
        {
            var n = Node(handle);
            if (n == null || n.Kind == UiKind.Part)
                return;
            Detach(n);
            Free(n);
        }

        /// <summary>Removes all children the mod added to this element.</summary>
        public void Clear(int handle)
        {
            var n = Node(handle);
            if (n == null || n.Children == null || n.Children.Count == 0)
                return;
            var list = n.Children.ToArray();
            if (n.Scroll != null)
                n.Scroll.Clear();
            else if (n.Chain != null)
                n.Chain.Clear();
            foreach (var c in list)
            {
                if (n.Scroll == null && n.Chain == null)
                    c.Element.Unregister();
                c.Parent = null;
                c.Entry = null;
                c.InChain = false;
                Free(c);
            }
            n.Children.Clear();
        }

        public void SetParent(int handle, int parent)
        {
            var n = Node(handle);
            if (n == null || n.Kind == UiKind.Part || handle == parent)
                return;
            UiNode p = null;
            if (parent > 0)
            {
                p = Node(parent);
                if (p == null)
                    return;
                for (var a = p; a != null; a = a.Parent)
                    if (a == n)
                        return;
            }
            Detach(n);
            Attach(n, p, parent);
        }

        /// <summary>Handle of a fixed part (window header 1, body 2, border 3; panel or scroll box background 1).</summary>
        public int Part(int handle, int part)
        {
            var n = Node(handle);
            if (n == null || part < 1 || part > 3)
                return 0;
            if (n.Parts != null && n.Parts[part - 1] != null)
                return n.Parts[part - 1].Handle;
            var e = n.PartElement(part);
            if (e == null || Full)
                return 0;
            var pn = new UiNode(this, NewHandle(), UiKind.Part);
            pn.Init(e);
            pn.Parent = n;
            _nodes[pn.Handle & IndexMask] = pn;
            if (n.Parts == null)
                n.Parts = new UiNode[3];
            n.Parts[part - 1] = pn;
            return pn.Handle;
        }

        public void Listen(int handle, int ev, bool on)
        {
            var n = Node(handle);
            if (n != null)
                n.Listen(ev, on);
        }

        // Property access: a bad handle is ignored and reads as default. No allocations here.

        public void SetBool(int h, int p, bool v) { var n = Node(h); if (n != null) n.SetBool(p, v); }
        public void SetInt(int h, int p, int v) { var n = Node(h); if (n != null) n.SetInt(p, v); }
        public void SetFloat(int h, int p, float v) { var n = Node(h); if (n != null) n.SetFloat(p, v); }
        public void SetVector(int h, int p, Vector2 v) { var n = Node(h); if (n != null) n.SetVector(p, v); }
        public void SetColor(int h, int p, Color v) { var n = Node(h); if (n != null) n.SetColor(p, v); }
        public void SetText(int h, int p, string v) { var n = Node(h); if (n != null) n.SetText(p, v); }
        public bool GetBool(int h, int p) { var n = Node(h); return n != null && n.GetBool(p); }
        public int GetInt(int h, int p) { var n = Node(h); return n != null ? n.GetInt(p) : 0; }
        public float GetFloat(int h, int p) { var n = Node(h); return n != null ? n.GetFloat(p) : 0f; }
        public Vector2 GetVector(int h, int p) { var n = Node(h); return n != null ? n.GetVector(p) : Vector2.Zero; }
        public Color GetColor(int h, int p) { var n = Node(h); return n != null ? n.GetColor(p) : Color.Transparent; }
        public string GetText(int h, int p) { var n = Node(h); return n != null ? n.GetText(p) : null; }

        public void Focus(int handle)
        {
            var n = Node(handle);
            if (n != null && n.Window != null && n.Window.Visible)
                n.Window.GetWindowFocus();
        }

        public Vector2 Measure(int handle, string markup, float wrapWidth)
        {
            var n = Node(handle);
            var style = n != null ? n.Style : new UiMarkup.Style { Color = Color.White, Size = 1f, Font = -1 };
            return _service.Measure(markup, style, wrapWidth);
        }

        /// <summary>Removes every element of this mod.</summary>
        public void RemoveAll()
        {
            foreach (var n in _roots.ToArray())
                Remove(n.Handle);
            _roots.Clear();
        }

        /// <summary>Rich HUD is gone: forget all elements and binds without touching Rich HUD.</summary>
        public void Forget()
        {
            for (int i = 1; i < _nodes.Count; i++)
            {
                var n = _nodes[i];
                if (n == null)
                    continue;
                n.Dead = true;
                _nodes[i] = null;
                _free.Push(i);
            }
            _roots.Clear();
            _group = null;
            _subscribed.Clear();
            _pageMade = false;
        }

        /// <summary>The mod undocks: elements and bind subscriptions go, the bind data is saved.</summary>
        public void Release()
        {
            if (_service.Ready)
            {
                SaveBinds();
                RemoveAll();
                if (_group != null)
                    _group.ClearSubscribers();
            }
            _subscribed.Clear();
            Dispatcher = null;
            WantsCursor = false;
            _bindNames.Clear();
            _bindKeys.Clear();
        }

        // ---------------------------------------------------------------- key binds

        /// <summary>Registers or changes the default of a bind; applied as soon as Rich HUD is ready.</summary>
        public void Bind(string name, string keys)
        {
            if (string.IsNullOrWhiteSpace(name))
                return;
            int i = _bindNames.IndexOf(name);
            if (i >= 0)
                _bindKeys[i] = keys ?? "";
            else
            {
                _bindNames.Add(name);
                _bindKeys.Add(keys ?? "");
            }
            if (_service.Ready)
                ApplyBind(name, keys ?? "");
            _service.WantPages();
        }

        public void BindPage(string title)
        {
            _pageTitle = string.IsNullOrWhiteSpace(title) ? ModName : title;
            _service.WantPages();
        }

        public string BindKeys(string name)
        {
            if (_group == null || name == null)
                return null;
            var b = _group.GetBind(name);
            if (b == null)
                return null;
            var combo = b.GetCombo();
            if (combo == null || combo.Count == 0)
                return "";
            var names = new string[combo.Count];
            for (int i = 0; i < combo.Count; i++)
                names[i] = BindManager.GetControlName(combo[i]);
            return string.Join("+", names);
        }

        /// <summary>Rich HUD became ready: make the group and all binds again.</summary>
        public void ApplyAllBinds()
        {
            for (int i = 0; i < _bindNames.Count; i++)
                ApplyBind(_bindNames[i], _bindKeys[i]);
        }

        private void ApplyBind(string name, string keys)
        {
            if (_group == null)
            {
                _group = BindManager.GetOrCreateGroup(ModName);
                if (!_bindsLoaded)
                {
                    _bindsLoaded = true;
                    _saved = _service.LoadBinds(ModName);
                }
            }
            var combo = Combo(keys);
            IBind bind = _group.GetBind(name);
            if (bind == null)
            {
                if (!_group.TryRegisterBind(name, out bind, combo))
                    bind = _group.GetBind(name);
                if (bind == null)
                    return;
            }
            else
                bind.TrySetCombo(combo, 0, false, true);
            if (_subscribed.Add(name))
            {
                string n = name;
                bind.NewPressed += (o, e) => _service.BindPressed(this, n);
            }
            if (_saved != null)
                foreach (var d in _saved)
                    if (d.name == name && d.controlNames != null)
                        bind.TrySetCombo(d.controlNames, 0, false, true);
        }

        private static List<ControlHandle> Combo(string keys)
        {
            var list = new List<ControlHandle>();
            if (string.IsNullOrWhiteSpace(keys))
                return list;
            foreach (var k in keys.Split('+'))
            {
                var t = k.Trim();
                if (t.Length > 0 && list.Count < BindManager.MaxBindLength)
                    list.Add(BindManager.GetControl(t));
            }
            return list;
        }

        /// <summary>Makes the rebind page in the Rich HUD terminal once binds and a title exist.</summary>
        public void MakePage()
        {
            if (_pageMade || _pageTitle == null || _group == null || _bindNames.Count == 0)
                return;
            _pageMade = true;
            var defaults = new BindDefinition[_bindNames.Count];
            for (int i = 0; i < _bindNames.Count; i++)
            {
                var keys = _bindKeys[i].Split('+');
                for (int k = 0; k < keys.Length; k++)
                    keys[k] = keys[k].Trim();
                defaults[i] = new BindDefinition(_bindNames[i], keys);
            }
            RichHudTerminal.Root.Enabled = true;
            var page = new RebindPage { Name = _pageTitle };
            page.Add(_group, defaults);
            RichHudTerminal.Root.Add(page);
        }

        public void SaveBinds()
        {
            if (_group == null || _bindNames.Count == 0)
                return;
            try
            {
                _service.SaveBinds(ModName, _group.GetBindDefinitions());
            }
            catch (Exception e)
            {
                Log.Warn("UI: key binds of " + ModName + " not saved: " + e.Message);
            }
        }

    }
}
