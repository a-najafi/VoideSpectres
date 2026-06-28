using VoidSpectre.Core.Context;
using VoidSpectre.Core.Diagnostics;
using VoidSpectre.Core.Interfaces;

namespace VoidSpectre.Gameplay.Demo
{
    [RunsInContext(ContextKind.Interior)]
    public sealed class DemoInteriorPulseSystem : ICoreUpdateSystem
    {
        public string Name => "Demo Interior Pulse";
        public int Priority => 0;

        private float _pulse;

        public void Update(SimulationContext context, float delta)
        {
            _pulse += delta;
            if (_pulse < 2f) return;
            _pulse = 0f;

            int crewCount = 0;
            foreach (var _ in context.Components.GetAll<DemoCrewTagComponent>())
                crewCount++;

            VsLog.Info($"[Interior:{context.DisplayName}] Heartbeat — {crewCount} crew inside");
        }
    }
}
