using SysMath = System.Math;

namespace VoidSpectre.Core.Math.Geometry
{
    public static class GeometryVolumeMath
    {
        public static float GetVolume(GeometryVolume volume) => volume.Shape switch
        {
            GeometryShape.Cylinder => VsMath.PI * volume.CylinderRadius * volume.CylinderRadius *
                                      VsMath.Max(0f, volume.CylinderHeight),
            GeometryShape.Sphere => 4f / 3f * VsMath.PI * volume.SphereRadius * volume.SphereRadius * volume.SphereRadius,
            _ => VsMath.Max(0f, volume.BoxSize.X) * VsMath.Max(0f, volume.BoxSize.Y) *
                 VsMath.Max(0f, volume.BoxSize.Z),
        };

        public static VsBounds GetLocalBounds(GeometryVolume volume)
        {
            return volume.Shape switch
            {
                GeometryShape.Cylinder => new VsBounds(
                    volume.LocalOffset,
                    new Float3(
                        volume.CylinderRadius * 2f,
                        volume.CylinderRadius * 2f,
                        VsMath.Max(volume.CylinderHeight, volume.CylinderRadius * 2f))),
                GeometryShape.Sphere => new VsBounds(
                    volume.LocalOffset,
                    new Float3(
                        volume.SphereRadius * 2f,
                        volume.SphereRadius * 2f,
                        volume.SphereRadius * 2f)),
                _ => new VsBounds(volume.LocalOffset, volume.BoxSize),
            };
        }

        public static Float3 ClosestSurfacePoint(
            GeometryVolume volume,
            Float3 volumeWorldCenter,
            FloatQuaternion volumeWorldRotation,
            Float3 queryPointWorld)
        {
            var localPoint = volumeWorldRotation.Inverse() * (queryPointWorld - volumeWorldCenter);
            var localSurface = ClosestSurfacePointLocal(volume, localPoint);
            return volumeWorldCenter + volumeWorldRotation * localSurface;
        }

        public static float SurfaceToSurfaceDistance(
            GeometryVolume volumeA,
            Float3 centerA,
            FloatQuaternion rotationA,
            GeometryVolume volumeB,
            Float3 centerB,
            FloatQuaternion rotationB)
        {
            var pointOnA = ClosestSurfacePoint(volumeA, centerA, rotationA, centerB);
            var pointOnB = ClosestSurfacePoint(volumeB, centerB, rotationB, pointOnA);
            pointOnA = ClosestSurfacePoint(volumeA, centerA, rotationA, pointOnB);
            var delta = pointOnB - pointOnA;
            return VsMath.Max(0f, delta.Magnitude);
        }

        private static Float3 ClosestSurfacePointLocal(GeometryVolume volume, Float3 localPoint)
        {
            var offset = volume.LocalOffset;
            var relative = localPoint - offset;

            return volume.Shape switch
            {
                GeometryShape.Sphere => ClosestSphereSurface(offset, relative, volume.SphereRadius),
                GeometryShape.Cylinder => ClosestCylinderSurface(offset, relative, volume.CylinderRadius, volume.CylinderHeight),
                _ => ClosestBoxSurface(offset, relative, volume.BoxSize),
            };
        }

        private static Float3 ClosestBoxSurface(Float3 center, Float3 relative, Float3 size)
        {
            var half = size * 0.5f;
            var clamped = new Float3(
                VsMath.Clamp(relative.X, -half.X, half.X),
                VsMath.Clamp(relative.Y, -half.Y, half.Y),
                VsMath.Clamp(relative.Z, -half.Z, half.Z));

            if ((clamped - relative).SqrMagnitude > 1e-8f)
                return center + clamped;

            var abs = new Float3(VsMath.Abs(relative.X), VsMath.Abs(relative.Y), VsMath.Abs(relative.Z));
            var distances = new Float3(half.X - abs.X, half.Y - abs.Y, half.Z - abs.Z);
            var result = clamped;

            if (distances.X <= distances.Y && distances.X <= distances.Z)
                result.X = relative.X > 0f ? half.X : -half.X;
            else if (distances.Y <= distances.Z)
                result.Y = relative.Y > 0f ? half.Y : -half.Y;
            else
                result.Z = relative.Z > 0f ? half.Z : -half.Z;

            return center + result;
        }

        private static Float3 ClosestSphereSurface(Float3 center, Float3 relative, float radius)
        {
            var safeRadius = VsMath.Max(radius, 1e-4f);
            var direction = relative.Normalized;
            if (direction.SqrMagnitude < 1e-8f)
                direction = Float3.Up;
            return center + direction * safeRadius;
        }

        private static Float3 ClosestCylinderSurface(Float3 center, Float3 relative, float radius, float height)
        {
            var safeRadius = VsMath.Max(radius, 1e-4f);
            var halfHeight = VsMath.Max(height, 1e-4f) * 0.5f;
            var radial = new Float3(relative.X, relative.Y, 0f);
            var radialDistance = radial.Magnitude;
            var clampedZ = VsMath.Clamp(relative.Z, -halfHeight, halfHeight);

            if (radialDistance > 1e-6f)
            {
                if (VsMath.Abs(relative.Z) <= halfHeight)
                {
                    return center + new Float3(
                        radial.X / radialDistance * safeRadius,
                        radial.Y / radialDistance * safeRadius,
                        relative.Z);
                }

                var capCenter = center + new Float3(0f, 0f, relative.Z > 0f ? halfHeight : -halfHeight);
                var capRelative = new Float3(relative.X, relative.Y, relative.Z > 0f ? halfHeight : -halfHeight);
                var capRadial = new Float3(capRelative.X, capRelative.Y, 0f);
                if (capRadial.Magnitude <= safeRadius)
                    return capCenter + new Float3(capRelative.X, capRelative.Y, 0f);

                var capDir = capRadial.Normalized;
                return capCenter + new Float3(capDir.X * safeRadius, capDir.Y * safeRadius, 0f);
            }

            var z = relative.Z;
            if (VsMath.Abs(z) <= halfHeight)
                return center + new Float3(safeRadius, 0f, z);

            return center + new Float3(0f, 0f, z > 0f ? halfHeight : -halfHeight);
        }
    }
}
