using VoidSpectre.Core.Context;
using VoidSpectre.Core.Diagnostics;
using VoidSpectre.Core.Interfaces;

namespace VoidSpectre.Gameplay.Demo
{
    [RunsInContext(ContextKind.Volume)]
    public sealed class DemoSectorShootListenerSystem : IEventSystem<DemoShootEvent>
    {
        public string Name => "Demo Sector Shoot Listener";
        public int Priority => 0;

        public void OnEvent(SimulationContext context, DemoShootEvent evt)
        {
            VsLog.Warning($"[Sector:{context.DisplayName}] UNEXPECTED shoot event received! Isolation broken.");
        }
    }
}
