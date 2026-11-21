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
    /// F (Manipulate) runs the registered action, K (OpenTerminal) the Vanilla terminal. A block without a registered
    /// action keeps its Vanilla use points (store, vending machine buttons ...): Syntrix makes the Vanilla use object of
    /// the same detector on the first use and hands every action on to it (1.5.0). So the reputation gate covers
    /// screens that are no terminal, such as the Vanilla store.
    /// </summary>
    [MyUseObject("syntrix")]
    public class SyntrixUseObject : MyUseObjectBase
    {
        private static readonly MyStringId DefaultHint = MyStringId.GetOrCompute("{0} Use");
        private static readonly MyStringId TerminalHint = MyStringId.GetOrCompute("NotificationHintPressToOpenControlPanel");
        private static readonly MyStringId StoreHint = MyStringId.GetOrCompute("NotificationHintPressToOpenStore");

        /// <summary>Vanilla detector name of the model dummy ("detector_store_1" gives "store"); null if none.</summary>
        private readonly string _vanillaName;
        private readonly Matrix _dummyMatrix;
        private IMyUseObject _vanilla;
        private bool _vanillaTried;

        /// <summary>Creates a use object for a model dummy of an entity.</summary>
        public SyntrixUseObject(IMyEntity owner, string dummyName, IMyModelDummy dummyData, uint shapeKey)
            : base(owner, dummyData)
        {
            var parts = (dummyName ?? "").ToLowerInvariant().Split('_');
            if (parts.Length >= 2 && parts[0] == "detector" && parts[1].Length > 0 && parts[1] != "syntrix")
                _vanillaName = parts[1];
            if (dummyData != null)
                _dummyMatrix = dummyData.Matrix;
        }

        public override UseActionEnum PrimaryAction { get { return UseActionEnum.Manipulate; } }
        public override UseActionEnum SecondaryAction { get { return UseActionEnum.OpenTerminal; } }
        public override UseActionEnum SupportedActions { get { return _vanilla != null ? _vanilla.SupportedActions | PrimaryAction | SecondaryAction : PrimaryAction | SecondaryAction; } }

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
                {
                    service.Use(block, 0);
                    return;
                }
            }
            var vanilla = Vanilla();
            if (vanilla != null)
                vanilla.Use(action, user);
            else if (action == UseActionEnum.Manipulate || action == UseActionEnum.OpenTerminal)
                MyAPIGateway.Gui.ShowTerminalPage(MyTerminalPageEnum.ControlPanel, character, block);
        }

        /// <summary>
        /// 1.5.0: the game's own use object of this detector (store, vending machine button, terminal ...), made on the
        /// first use without a registered action. The game makes it from the detector name; the detector is taken out
        /// again at once, so it never becomes a second use point. Only from Use, never while the game walks the use points.
        /// </summary>
        private IMyUseObject Vanilla()
        {
            var service = UseService.Instance;
            if (service != null && service.Has(Owner))
                return null;
            if (_vanillaTried)
                return _vanilla;
            _vanillaTried = true;
            if (_vanillaName == null || Owner == null)
                return null;
            var uses = Owner.Components.Get<MyUseObjectsComponentBase>();
            if (uses == null)
                return null;
            try
            {
                uint id = uses.AddDetector(_vanillaName, _dummyMatrix);
                _vanilla = uses.GetInteractiveObject(id);
                uses.RemoveDetector(id);
            }
            catch (Exception x)
            {
                Log.Error("Vanilla use object " + _vanillaName + " of " + UseService.KeyOf(Owner), x);
                _vanilla = null;
            }
            if (_vanilla is SyntrixUseObject)
                _vanilla = null;
            return _vanilla;
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
            if (_vanilla != null && (UseService.Instance == null || !UseService.Instance.Has(Owner)))
                return _vanilla.GetActionInfo(action);
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
