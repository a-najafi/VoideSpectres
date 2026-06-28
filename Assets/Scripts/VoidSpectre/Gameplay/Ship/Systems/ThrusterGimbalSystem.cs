using VoidSpectre.Core.Context;
using VoidSpectre.Core.Interfaces;
using VoidSpectre.Core.Math;
using VoidSpectre.Gameplay.Ship.Parts;
using VoidSpectre.Gameplay.Ship.Thrusters;

namespace VoidSpectre.Gameplay.Ship.Systems
{
    [RunsInContext(ContextKind.Volume)]
    public sealed class ThrusterGimbalSystem : ICoreUpdateSystem
    {
        public string Name => "Thruster Gimbal";
        public int Priority => 5;

        public void Update(SimulationContext context, float delta)
        {
            foreach (var (entity, gimbal) in context.Components.GetAll<GimbalThrusterComponent>())
            {
                if (ShipPlanPlaybackQueries.IsThrusterOnPlaybackShip(context, entity)) continue;
                var maxStep = gimbal.MaxGimbalSpeedDegreesPerSecond * delta;
                gimbal.CurrentGimbalDegrees = VsMath.MoveTowards(
                    gimbal.CurrentGimbalDegrees,
                    gimbal.TargetGimbalDegrees,
                    maxStep);
            }
        }
    }
}
