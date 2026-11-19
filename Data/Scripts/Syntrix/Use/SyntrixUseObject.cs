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
    /// The game makes one per use point of a block whose UseObjectsComponent has UseObjectTypeOverride "syntrix".
    /// F (Manipulate) runs the registered action, K (OpenTerminal) the Vanilla terminal.
    /// </summary>
    [MyUseObject("syntrix")]
    public class SyntrixUseObject : MyUseObjectBase
    {
        private static readonly MyStringId DefaultHint = MyStringId.GetOrCompute("{0} Use");
        private static readonly MyStringId TerminalHint = MyStringId.GetOrCompute("NotificationHintPressToOpenControlPanel");

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
