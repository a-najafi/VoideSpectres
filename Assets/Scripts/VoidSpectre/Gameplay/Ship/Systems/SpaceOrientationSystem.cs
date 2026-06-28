using VoidSpectre.Core.Context;
using VoidSpectre.Core.Interfaces;
using VoidSpectre.Core.Math;
using VoidSpectre.Gameplay.Ship.Parts;
using VoidSpectre.Gameplay.Space;

namespace VoidSpectre.Gameplay.Ship.Systems
{
    [RunsInContext(ContextKind.Volume)]
    public sealed class SpaceOrientationSystem : ICoreUpdateSystem
    {
        public string Name => "Space Orientation";
        public int Priority => 25;

        public void Update(SimulationContext context, float delta)
        {
            foreach (var (ship, angular) in context.Components.GetAll<ShipAngularStateComponent>())
            {
                if (ShipPlanPlaybackQueries.IsStepSimPlayback(context, ship)) continue;
                if (!context.Components.TryGet(ship, out SpaceOrientationComponent orientation)) continue;
                if (!context.Components.TryGet(ship, out ShipAggregateComponent aggregate)) continue;
                if (aggregate.ApproximateMomentOfInertia <= 0f) continue;

                var inertia = aggregate.ApproximateMomentOfInertia;
                angular.AngularVelocityLocal += angular.AccumulatedTorqueLocal / inertia * delta;

                var angularSpeed = angular.AngularVelocityLocal.Magnitude;
                if (angularSpeed > 1e-6f)
                {
                    var deltaRot = FloatQuaternion.AngleAxis(
                        angularSpeed * VsMath.Rad2Deg * delta,
                        angular.AngularVelocityLocal.Normalized);
                    orientation.Value = orientation.Value * deltaRot;
                }

                angular.AngularVelocityLocal *= VsMath.Clamp01(1f - 0.15f * delta);
                angular.ClearTorque();
            }
        }
    }
}
