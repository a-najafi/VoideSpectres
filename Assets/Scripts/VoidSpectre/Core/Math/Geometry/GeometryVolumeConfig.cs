using System;
using Sirenix.Serialization;
using VoidSpectre.Core.Math;

namespace VoidSpectre.Core.Math.Geometry
{
    [Serializable]
    public struct GeometryVolumeConfig
    {
        [OdinSerialize] public GeometryShape Shape;
        [OdinSerialize] public Float3 LocalOffset;
        [OdinSerialize] public Float3 BoxSize;
        [OdinSerialize] public float CylinderRadius;
        [OdinSerialize] public float CylinderHeight;
        [OdinSerialize] public float SphereRadius;

        public GeometryVolume ToVolume() => new()
        {
            Shape = Shape,
            LocalOffset = LocalOffset,
            BoxSize = BoxSize,
            CylinderRadius = CylinderRadius,
            CylinderHeight = CylinderHeight,
            SphereRadius = SphereRadius,
        };

        public static GeometryVolumeConfig FromBox(Float3 localOffset, Float3 size) => new()
        {
            Shape = GeometryShape.Box,
            LocalOffset = localOffset,
            BoxSize = size,
        };

        public static GeometryVolumeConfig FromCylinder(Float3 localOffset, float radius, float height) => new()
        {
            Shape = GeometryShape.Cylinder,
            LocalOffset = localOffset,
            CylinderRadius = radius,
            CylinderHeight = height,
        };

        public static GeometryVolumeConfig FromSphere(Float3 localOffset, float radius) => new()
        {
            Shape = GeometryShape.Sphere,
            LocalOffset = localOffset,
            SphereRadius = radius,
        };
    }
}
