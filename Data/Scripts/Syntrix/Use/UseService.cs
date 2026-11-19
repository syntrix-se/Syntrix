// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Entity.UseObject;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.ModAPI;
using VRage.Utils;

namespace Syntrix.Use
{
    /// <summary>
    /// Own F action for blocks (API 3). A block gets it in its definition: a UseObjectsComponent with
    /// UseObjectTypeOverride "syntrix" (see Examples). A docked mod registers what F does for a block type;
    /// K (terminal) opens the Vanilla terminal of the block as usual.
    /// </summary>
    public sealed class UseService
    {
        public static UseService Instance { get; private set; }

        private struct Entry
        {
            public string Mod;
            public string Hint;
            public Action<long, int> Handler;
        }

        private readonly Dictionary<string, Entry> _entries = new Dictionary<string, Entry>();

        public UseService()
        {
            Instance = this;
        }

        public void Stop()
        {
            _entries.Clear();
            if (Instance == this)
                Instance = null;
        }

        /// <summary>Registers the F action for a block type "TypeId/SubtypeId" (TypeId without MyObjectBuilder_).</summary>
        public void Register(string mod, string blockKey, string hint, Action<long, int> handler)
        {
            if (string.IsNullOrEmpty(blockKey) || handler == null)
                return;
            Entry old;
            if (_entries.TryGetValue(blockKey, out old) && old.Mod != mod)
                Log.Warn("F action of " + blockKey + " taken over by " + mod + " from " + old.Mod);
            _entries[blockKey] = new Entry { Mod = mod, Hint = hint, Handler = handler };
        }

        public void Unregister(string mod, string blockKey)
        {
            Entry old;
            if (_entries.TryGetValue(blockKey, out old) && old.Mod == mod)
                _entries.Remove(blockKey);
        }

        public static string KeyOf(IMyEntity block)
        {
            var b = block as IMyCubeBlock;
            if (b == null)
                return null;
            var id = b.BlockDefinition;
            return id.TypeId.ToString().Replace("MyObjectBuilder_", "") + "/" + id.SubtypeName;
        }

        public bool Has(IMyEntity block)
        {
            var key = KeyOf(block);
            return key != null && _entries.ContainsKey(key);
        }

        public string Hint(IMyEntity block)
        {
            Entry e;
            var key = KeyOf(block);
            return key != null && _entries.TryGetValue(key, out e) ? e.Hint : null;
        }

        /// <summary>Runs the F action of the block (local player only).</summary>
        public void Use(IMyEntity block, int action)
        {
            Entry e;
            var key = KeyOf(block);
            if (key == null || !_entries.TryGetValue(key, out e))
                return;
            try
            {
                e.Handler(block.EntityId, action);
            }
            catch (Exception x)
            {
                Log.Error("F action of " + key + " (" + e.Mod + ")", x);
            }
        }
    }

    /// <summary>
    /// The game makes one per use point of a block whose UseObjectsComponent has UseObjectTypeOverride "syntrix".
    /// F (Manipulate) runs the registered action, K (OpenTerminal) the Vanilla terminal.
    /// </summary>
    [MyUseObject("syntrix")]
    public class SyntrixUseObject : MyUseObjectBase
    {
        private static readonly MyStringId DefaultHint = MyStringId.GetOrCompute("{0} Use");
        private static readonly MyStringId TerminalHint = MyStringId.GetOrCompute("NotificationHintPressToOpenControlPanel");

        public SyntrixUseObject(IMyEntity owner, string dummyName, IMyModelDummy dummyData, uint shapeKey)
            : base(owner, dummyData)
        {
        }

        public override UseActionEnum PrimaryAction { get { return UseActionEnum.Manipulate; } }
        public override UseActionEnum SecondaryAction { get { return UseActionEnum.OpenTerminal; } }

        public override void Use(UseActionEnum action, IMyEntity user)
        {
            var character = user as IMyCharacter;
            if (MyAPIGateway.Utilities.IsDedicated || MyAPIGateway.Session == null || MyAPIGateway.Session.Player == null
                || MyAPIGateway.Session.Player.Character != character || character == null)
                return;
            var block = Owner;
            if (action == UseActionEnum.Manipulate)
            {
                var service = UseService.Instance;
                if (service != null && service.Has(block))
                    service.Use(block, 0);
                else
                    MyAPIGateway.Gui.ShowTerminalPage(MyTerminalPageEnum.ControlPanel, character, block);
            }
            else if (action == UseActionEnum.OpenTerminal)
                MyAPIGateway.Gui.ShowTerminalPage(MyTerminalPageEnum.ControlPanel, character, block);
        }

        public override MyActionDescription GetActionInfo(UseActionEnum action)
        {
            var d = new MyActionDescription();
            bool f = action == UseActionEnum.Manipulate;
            string hint = null;
            if (f && UseService.Instance != null)
                hint = UseService.Instance.Hint(Owner);
            if (f)
            {
                d.Text = string.IsNullOrEmpty(hint) ? DefaultHint : MyStringId.GetOrCompute(MyTexts.GetString(hint).Contains("{0}") ? MyTexts.GetString(hint) : "{0} " + MyTexts.GetString(hint));
                d.FormatParams = new object[] { "[" + Key("USE") + "]" };
            }
            else
            {
                d.Text = TerminalHint;
                d.FormatParams = new object[] { "[" + Key("TERMINAL") + "]" };
            }
            d.IsTextControlHint = true;
            return d;
        }

        private static string Key(string control)
        {
            var c = MyAPIGateway.Input.GetGameControl(MyStringId.GetOrCompute(control));
            return c != null ? c.GetControlButtonName(MyGuiInputDeviceEnum.Keyboard) : control;
        }
    }
}
