using System.Collections.Generic;
using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Math;
using VoidSpectre.Core.Math.Geometry;
using VoidSpectre.Gameplay.Ship.Parts;
using VoidSpectre.Gameplay.Space;

namespace VoidSpectre.Gameplay.Physics
{
    public readonly struct WorldGeometryVolume
    {
        public readonly GeometryVolume Volume;
        public readonly Float3 WorldCenter;
        public readonly FloatQuaternion WorldRotation;

        public WorldGeometryVolume(GeometryVolume volume, Float3 worldCenter, FloatQuaternion worldRotation)
        {
            Volume = volume;
            WorldCenter = worldCenter;
            WorldRotation = worldRotation;
        }
    }

    public static class PhysicsGeometryQuery
    {
        public static void CollectWorldVolumes(
            SimulationContext context,
            ComponentStore.EntityId entity,
            List<WorldGeometryVolume> results)
        {
            results.Clear();

            if (context.Components.TryGet(entity, out SpacePositionComponent position) &&
                context.Components.TryGet(entity, out SpaceOrientationComponent orientation) &&
                context.Components.TryGet(entity, out GeometryVolumesComponent directVolumes))
            {
                AppendVolumes(results, directVolumes.Volumes, position.Value, orientation.Value, Float3.Zero, FloatQuaternion.Identity);
                if (results.Count > 0) return;
            }

            foreach (var (partEntity, part) in context.Components.GetAll<ShipPartComponent>())
            {
                if (part.ParentShip != entity) continue;
                if (!context.Components.TryGet(partEntity, out LocalTransformComponent local)) continue;
                if (!context.Components.TryGet(partEntity, out GeometryVolumesComponent partVolumes)) continue;

                var shipPosition = context.Components.TryGet(entity, out SpacePositionComponent shipPos)
                    ? shipPos.Value
                    : Float3.Zero;
                var shipRotation = context.Components.TryGet(entity, out SpaceOrientationComponent shipRot)
                    ? shipRot.Value
                    : FloatQuaternion.Identity;

                var partWorldCenter = shipPosition + shipRotation * local.LocalPosition;
                var partWorldRotation = shipRotation * local.LocalRotation;
                AppendVolumes(results, partVolumes.Volumes, partWorldCenter, partWorldRotation, Float3.Zero, FloatQuaternion.Identity);
            }
        }

        public static float SurfaceToSurfaceDistance(
            IReadOnlyList<WorldGeometryVolume> volumesA,
            IReadOnlyList<WorldGeometryVolume> volumesB)
        {
            if (volumesA.Count == 0 || volumesB.Count == 0)
                return float.MaxValue;

            float minDistance = float.MaxValue;
            for (int i = 0; i < volumesA.Count; i++)
            {
                for (int j = 0; j < volumesB.Count; j++)
                {
                    var distance = GeometryVolumeMath.SurfaceToSurfaceDistance(
                        volumesA[i].Volume,
                        volumesA[i].WorldCenter,
                        volumesA[i].WorldRotation,
                        volumesB[j].Volume,
                        volumesB[j].WorldCenter,
                        volumesB[j].WorldRotation);
                    if (distance < minDistance)
                        minDistance = distance;
                }
            }

            return minDistance;
        }

        private static void AppendVolumes(
            List<WorldGeometryVolume> results,
            List<GeometryVolume> volumes,
            Float3 parentCenter,
            FloatQuaternion parentRotation,
            Float3 localOffset,
            FloatQuaternion localRotation)
        {
            for (int i = 0; i < volumes.Count; i++)
            {
                var volume = volumes[i];
                var volumeCenter = parentCenter + parentRotation * (localOffset + volume.LocalOffset);
                var volumeRotation = parentRotation * localRotation;
                results.Add(new WorldGeometryVolume(volume, volumeCenter, volumeRotation));
            }
        }
    }
}
