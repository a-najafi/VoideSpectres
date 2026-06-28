using VoidSpectre.Core;
using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Diagnostics;
using VoidSpectre.Core.Interfaces;

namespace VoidSpectre.Gameplay.Demo
{
    [AutoRegisterIgnore]
    public sealed class DemoTickLoggerSystem : ICoreUpdateSystem
    {
        public string Name => "Demo Tick Logger";
        public int Priority => 100;

        public void Update(SimulationContext context, float delta)
        {
            VsLog.Info($"[Tick:{context.DisplayName}] tier={context.CurrentTickTier} delta={delta:F3}s");
        }
    }
}
