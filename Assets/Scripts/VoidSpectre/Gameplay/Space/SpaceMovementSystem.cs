using VoidSpectre.Core.Context;
using VoidSpectre.Core.Interfaces;
using VoidSpectre.Core.Math;
using VoidSpectre.Gameplay.Physics;
using VoidSpectre.Gameplay.Ship.Parts;
using VoidSpectre.Gameplay.Space;

namespace VoidSpectre.Gameplay.Space
{
    [RunsInContext(ContextKind.Volume)]
    public sealed class SpaceMovementSystem : ICoreUpdateSystem
    {
        public string Name => "Space Movement";
        public int Priority => 30;

        public void Update(SimulationContext context, float delta)
        {
            if (delta <= 0f) return;

            foreach (var (entity, move) in context.Components.GetAll<SpaceMoveComponent>())
            {
                if (context.Components.Has<ShipAggregateComponent>(entity) &&
                    Ship.Systems.ShipPlanPlaybackQueries.IsStepSimPlayback(context, entity))
                {
                    move.ClearForces();
                    continue;
                }
                if (!context.Components.TryGet(entity, out SpacePositionComponent position)) continue;
                if (!context.Components.TryGet(entity, out MassComponent mass)) continue;
                if (mass.Value <= 0f) continue;

                var velocity = move.Velocity;
                var netForce = SumActiveForces(move);
                velocity += netForce / mass.Value * delta;

                move.SetVelocity(velocity);
                position.Value += velocity * delta;
            }
        }

        private static Float3 SumActiveForces(SpaceMoveComponent move)
        {
            var net = Float3.Zero;
            var forces = move.ActiveForces;
            for (int i = 0; i < forces.Count; i++)
                net += forces[i].Force;
            return net;
        }
    }
}
