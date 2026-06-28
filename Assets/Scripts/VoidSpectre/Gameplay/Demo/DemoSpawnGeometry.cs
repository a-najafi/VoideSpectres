using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Math;
using VoidSpectre.Core.Math.Geometry;
using VoidSpectre.Gameplay.Physics;
using VoidSpectre.Gameplay.Space;

namespace VoidSpectre.Gameplay.Demo
{
    public static class DemoSpawnGeometry
    {
        public const float RockSurfaceClearanceMeters = 50f;
        public const float ShipSurfaceClearanceMeters = 80f;

        private static readonly Float3 DefaultRockDirection = new(0.24f, 0.08f, 0.65f);

        public static bool TryGetSphereRadius(SimulationContext context, ComponentStore.EntityId entity, out float radius)
        {
            radius = 0f;
            if (context == null)
                return false;

            if (!context.Components.TryGet(entity, out GeometryVolumesComponent geometry))
                return false;

            if (geometry.Volumes == null || geometry.Volumes.Count == 0)
                return false;

            var volume = geometry.Volumes[0];
            if (volume.Shape != GeometryShape.Sphere)
                return false;

            radius = VsMath.Max(0f, volume.SphereRadius);
            return radius > 0f;
        }

        public static Float3 GetSpacePosition(SimulationContext context, ComponentStore.EntityId entity)
        {
            if (context != null &&
                context.Components.TryGet(entity, out SpacePositionComponent position))
                return position.Value;

            return Float3.Zero;
        }

        public static Float3 PositionOutsideSphere(
            Float3 sphereCenter,
            float sphereRadius,
            float bodyRadius,
            Float3 directionFromCenter,
            float surfaceClearanceMeters)
        {
            var direction = directionFromCenter.SqrMagnitude > 1e-6f
                ? directionFromCenter.Normalized
                : Float3.Forward;

            var distanceFromCenter = sphereRadius + bodyRadius + surfaceClearanceMeters;
            return sphereCenter + direction * distanceFromCenter;
        }

        public static Float3 DefaultSpaceRockDirection() =>
            DefaultRockDirection.Normalized;
    }
}
