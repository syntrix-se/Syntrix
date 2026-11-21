// Syntrix - shared services framework for Space Engineers mods.
// MIT License, see LICENSE in the repository root.
using System;
using VRage.Game.Components;

namespace Syntrix
{
    /// <summary>Frame hook of the services of API 4 (fixed screens, admin blocks).</summary>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class ServiceTick : MySessionComponentBase
    {
        public override void UpdateAfterSimulation()
        {
            var core = SyntrixCore.Instance;
            if (core == null || !core.Started)
                return;
            try
            {
                core.AdminBlocks.Tick();
                core.Lights.Tick();
                core.Rep.Tick();
                core.Accounts.Tick();
                core.Time.Tick();
                core.Supply.Tick();
                if (core.IsPlayer)
                {
                    core.Screens.Tick();
                    core.Sounds.Tick();
                    core.Rep.GateTick();
                }
            }
            catch (Exception e)
            {
                Log.Error("service tick", e);
            }
        }
    }
}
