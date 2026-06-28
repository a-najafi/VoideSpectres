using VoidSpectre.Core.Context;
using VoidSpectre.Core.Diagnostics;
using VoidSpectre.Core.Interfaces;

namespace VoidSpectre.Gameplay.Demo
{
    [RunsInContext(ContextKind.Interior)]
    public sealed class DemoShootSystem : IEventSystem<DemoShootEvent>
    {
        public string Name => "Demo Shoot (Interior)";
        public int Priority => 0;

        public void OnEvent(SimulationContext context, DemoShootEvent evt)
        {
            VsLog.Info($"[Interior:{context.DisplayName}] SHOT fired: {evt.Shooter} -> {evt.Target} (isolated to ship context)");
        }
    }
}
