// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity.UseObject;
using VRage.Game.ModAPI;
using VRage.Input;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace Syntrix.Use
{
    /// <summary>
    /// The game makes one per use point of a block whose UseObjectsComponent has UseObjectTypeOverride "syntrix".
    /// F (Manipulate) runs the registered action, K (OpenTerminal) the Vanilla terminal. At a store block without a
    /// registered action F opens the Vanilla store (1.5.0), so a store with this use object is gated by reputation too.
    /// </summary>
    [MyUseObject("syntrix")]
    public class SyntrixUseObject : MyUseObjectBase
    {
        private static readonly MyStringId DefaultHint = MyStringId.GetOrCompute("{0} Use");
        private static readonly MyStringId TerminalHint = MyStringId.GetOrCompute("NotificationHintPressToOpenControlPanel");
        private static readonly MyStringId StoreHint = MyStringId.GetOrCompute("NotificationHintPressToOpenStore");

        /// <summary>Vanilla store use objects made on demand, per store block (players only).</summary>
        private static readonly Dictionary<long, IMyUseObject> _stores = new Dictionary<long, IMyUseObject>();

        /// <summary>Creates a use object for a model dummy of an entity.</summary>
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
            if (Refused(block as IMyCubeBlock))
                return;
            if (action == UseActionEnum.Manipulate)
            {
                var service = UseService.Instance;
                if (service != null && service.Has(block))
                    service.Use(block, 0);
                else if (!(block is IMyStoreBlock) || !OpenStore(block, character))
                    MyAPIGateway.Gui.ShowTerminalPage(MyTerminalPageEnum.ControlPanel, character, block);
            }
            else if (action == UseActionEnum.OpenTerminal)
                MyAPIGateway.Gui.ShowTerminalPage(MyTerminalPageEnum.ControlPanel, character, block);
        }

        /// <summary>
        /// 1.5.0: opens the Vanilla store screen through the game's own "store" use object. The game makes it from the
        /// detector name; it is taken out again at once, so it never becomes a second use point of the block.
        /// </summary>
        private static bool OpenStore(IMyEntity block, IMyCharacter character)
        {
            IMyUseObject store;
            if (!_stores.TryGetValue(block.EntityId, out store) || store.Owner != block)
            {
                var uses = block.Components.Get<MyUseObjectsComponentBase>();
                if (uses == null)
                    return false;
                uint id = uses.AddDetector("store", Matrix.Identity);
                store = uses.GetInteractiveObject(id);
                uses.RemoveDetector(id);
                if (store == null)
                    return false;
                if (_stores.Count > 200)
                    _stores.Clear();
                _stores[block.EntityId] = store;
            }
            store.Use(UseActionEnum.Manipulate, character);
            return true;
        }

        /// <summary>1.4.1: a faction that does not trade with the player keeps F and K closed and says why.</summary>
        private static bool Refused(IMyCubeBlock block)
        {
            var core = SyntrixCore.Instance;
            string name;
            if (core == null || core.Rep == null || !core.Rep.Refuses(block, MyAPIGateway.Session.Player.IdentityId, out name))
                return false;
            core.Net.ShowLocal("Syntrix_Rep_Refused", "{0} refuses you access: your reputation is too low.", new[] { name }, 3000, "Red");
            return true;
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
                d.Text = string.IsNullOrEmpty(hint) ? (Owner is IMyStoreBlock ? StoreHint : DefaultHint) : MyStringId.GetOrCompute(MyTexts.GetString(hint).Contains("{0}") ? MyTexts.GetString(hint) : "{0} " + MyTexts.GetString(hint));
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
