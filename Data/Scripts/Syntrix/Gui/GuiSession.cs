// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRageMath;

namespace Syntrix.Gui
{
    /// <summary>Frame hooks for the window service: overlays in Draw(), queued events in the update.</summary>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class GuiSession : MySessionComponentBase
    {
        /// <summary>Actions for the next update (things that must not run inside chat or input handlers).</summary>
        private static readonly List<Action> _later = new List<Action>();

        /// <summary>Queues an action for the next update or input pass.</summary>
        public static void Later(Action a)
        {
            if (a != null)
                _later.Add(a);
        }

        public override void UpdateAfterSimulation()
        {
            RunLater();
            GuiService.Flush();
        }

        public override void HandleInput()
        {
            RunLater();
            var s = GuiService.Instance;
            if (s != null)
                s.HandleKeys();
        }

        private static void RunLater()
        {
            if (_later.Count == 0)
                return;
            var list = _later.ToArray();
            _later.Clear();
            foreach (var a in list)
            {
                try { a(); }
                catch (Exception e) { Log.Error("GUI deferred action", e); }
            }
        }

        public override void Draw()
        {
            var s = GuiService.Instance;
            if (s != null)
                s.Draw();
        }

        protected override void UnloadData()
        {
            _later.Clear();
        }
    }
}
