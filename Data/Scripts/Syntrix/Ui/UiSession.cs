// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using VRage.Game.Components;

namespace Syntrix.Ui
{
    /// <summary>Frame hooks of the UI service (input, draw, after simulation). Does nothing without the service.</summary>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class UiSession : MySessionComponentBase
    {
        private const int MaxErrors = 10;
        private int _errors;
        private bool _failed;

        public override void UpdateAfterSimulation()
        {
            var s = UiService.Instance;
            if (s == null || _failed)
                return;
            try { s.Update(); }
            catch (Exception e) { Fail("update", e); }
        }

        public override void HandleInput()
        {
            var s = UiService.Instance;
            if (s == null || _failed)
                return;
            try { s.Input(); }
            catch (Exception e) { Fail("input", e); }
        }

        public override void Draw()
        {
            var s = UiService.Instance;
            if (s == null || _failed)
                return;
            try { s.Frame(); }
            catch (Exception e) { Fail("draw", e); }
        }

        private void Fail(string where, Exception e)
        {
            // A repeating error must not flood the log every frame
            _failed = ++_errors >= MaxErrors;
            Log.Error("UI " + where + (_failed ? " (UI frame work stopped for this session)" : ""), e);
        }
    }
}
