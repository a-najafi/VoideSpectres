using VoidSpectre.Core.Context;
using VoidSpectre.Core.Interfaces;
using VoidSpectre.Core.Math;
using VoidSpectre.Gameplay.Ship.Parts;
using VoidSpectre.Gameplay.Ship.Thrusters;

namespace VoidSpectre.Gameplay.Ship.Systems
{
    [RunsInContext(ContextKind.Volume)]
    public sealed class ThrusterPowerSystem : ICoreUpdateSystem
    {
        public string Name => "Thruster Power";
        public int Priority => 8;

        public void Update(SimulationContext context, float delta)
        {
            foreach (var (entity, thruster) in context.Components.GetAll<ThrusterComponent>())
            {
                if (!context.Components.Has<ShipPartComponent>(entity)) continue;
                if (ShipPlanPlaybackQueries.IsThrusterOnPlaybackShip(context, entity)) continue;

                if (thruster.TargetPower <= 0f)
                {
                    thruster.CurrentPower = 0f;
                    continue;
                }

                var rampRate = 1f / thruster.RampUpSeconds;
                thruster.CurrentPower = VsMath.MoveTowards(
                    thruster.CurrentPower,
                    thruster.TargetPower,
                    rampRate * delta);
            }
        }
    }
}
