using VoidSpectre.Core.Context;
using VoidSpectre.Core.Interfaces;
using VoidSpectre.Core.Math;
using VoidSpectre.Gameplay.Physics;
using VoidSpectre.Gameplay.Ship.Parts;
using VoidSpectre.Gameplay.Ship.Thrusters;
using VoidSpectre.Gameplay.Space;

namespace VoidSpectre.Gameplay.Ship.Systems
{
    [RunsInContext(ContextKind.Volume)]
    public sealed class ThrusterForceSystem : ICoreUpdateSystem
    {
        public string Name => "Thruster Force";
        public int Priority => 15;

        public void Update(SimulationContext context, float delta)
        {
            foreach (var (ship, _) in context.Components.GetAll<ShipAggregateComponent>())
            {
                if (context.Components.TryGet(ship, out ShipAngularStateComponent angular))
                    angular.ClearTorque();
            }

            foreach (var (entity, part) in context.Components.GetAll<ShipPartComponent>())
            {
                if (ShipPlanPlaybackQueries.IsStepSimPlayback(context, part.ParentShip)) continue;
                if (!context.Components.TryGet(entity, out ThrusterComponent thruster)) continue;
                if (!context.Components.TryGet(entity, out LocalTransformComponent local)) continue;
                if (!context.Components.TryGet(part.ParentShip, out SpaceOrientationComponent orientation)) continue;
                if (!context.Components.TryGet(part.ParentShip, out ShipAggregateComponent aggregate)) continue;
                if (!context.Components.TryGet(part.ParentShip, out SpaceMoveComponent move)) continue;
                if (!context.Components.TryGet(part.ParentShip, out ShipAngularStateComponent angular)) continue;

                if (thruster.CurrentPower <= 0f || thruster.CurrentThrustNewtons <= 0f) continue;

                context.Components.TryGet(entity, out GimbalThrusterComponent gimbal);
                var shipLocalDir = ThrusterDirectionUtility.GetShipLocalThrustDirection(local, gimbal);
                var worldDir = orientation.Value * shipLocalDir;
                var worldForce = worldDir * thruster.CurrentThrustNewtons;
                move.AddForce(worldForce, entity.Id);

                var worldPoint = orientation.Value * local.LocalPosition;
                var comWorld = orientation.Value * aggregate.CenterOfMassLocal;
                var leverArm = worldPoint - comWorld;
                var torqueWorld = Float3.Cross(leverArm, worldForce);
                angular.AddTorque(orientation.Value.Inverse() * torqueWorld);
            }
        }
    }
}
