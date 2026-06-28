using System.Collections.Generic;
using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Interfaces;
using VoidSpectre.Core.Math;
using VoidSpectre.Gameplay.Space;

namespace VoidSpectre.Gameplay.Physics.Systems
{
    [RunsInContext(ContextKind.Volume)]
    public sealed class GravitySystem : ICoreUpdateSystem
    {
        public string Name => "Gravity";
        public int Priority => 10;

        private readonly List<WorldGeometryVolume> _sourceVolumes = new();
        private readonly List<WorldGeometryVolume> _affectedVolumes = new();

        public void Update(SimulationContext context, float delta)
        {
            foreach (var (affectedEntity, _) in context.Components.GetAll<GravityAffectedComponent>())
            {
                if (Ship.Systems.ShipPlanPlaybackQueries.IsStepSimPlayback(context, affectedEntity))
                    continue;
                if (!context.Components.TryGet(affectedEntity, out SpaceMoveComponent move)) continue;
                if (!context.Components.TryGet(affectedEntity, out MassComponent mass)) continue;
                if (mass.Value <= 0f) continue;

                PhysicsGeometryQuery.CollectWorldVolumes(context, affectedEntity, _affectedVolumes);
                if (_affectedVolumes.Count == 0) continue;

                foreach (var (sourceEntity, gravity) in context.Components.GetAll<GravitySourceComponent>())
                {
                    if (sourceEntity == affectedEntity) continue;
                    if (!context.Components.TryGet(sourceEntity, out SpacePositionComponent sourcePosition)) continue;

                    PhysicsGeometryQuery.CollectWorldVolumes(context, sourceEntity, _sourceVolumes);
                    if (_sourceVolumes.Count == 0) continue;

                    var surfaceDistance = PhysicsGeometryQuery.SurfaceToSurfaceDistance(_affectedVolumes, _sourceVolumes);
                    if (surfaceDistance <= gravity.IgnoreDistanceMeters)
                        continue;

                    var direction = (sourcePosition.Value - GetRepresentativePoint(_affectedVolumes)).Normalized;
                    if (direction.SqrMagnitude < 1e-8f)
                        continue;

                    var effectiveDistance = VsMath.Max(surfaceDistance, 1f);
                    var forceMagnitude = gravity.GravitationalParameter * mass.Value /
                                         (effectiveDistance * effectiveDistance);
                    move.AddForce(direction * forceMagnitude, sourceEntity.Id);
                }
            }
        }

        private static Float3 GetRepresentativePoint(IReadOnlyList<WorldGeometryVolume> volumes)
        {
            if (volumes.Count == 0) return Float3.Zero;

            var sum = Float3.Zero;
            for (int i = 0; i < volumes.Count; i++)
                sum += volumes[i].WorldCenter;
            return sum / volumes.Count;
        }
    }
}
