using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;

namespace VoidSpectre.Core.Math.Geometry
{
    [Serializable]
    public struct GeometryVolume
    {
        [OdinSerialize] public GeometryShape Shape;
        [OdinSerialize] public Float3 LocalOffset;
        [ShowIf(nameof(IsBox))][OdinSerialize] public Float3 BoxSize;
        [ShowIf(nameof(IsCylinder))][OdinSerialize] public float CylinderRadius;
        [ShowIf(nameof(IsCylinder))][OdinSerialize] public float CylinderHeight;
        [ShowIf(nameof(IsSphere))][OdinSerialize] public float SphereRadius;

        public static GeometryVolume Box(Float3 localOffset, Float3 size) => new()
        {
            Shape = GeometryShape.Box,
            LocalOffset = localOffset,
            BoxSize = size,
        };

        public static GeometryVolume Cylinder(Float3 localOffset, float radius, float height) => new()
        {
            Shape = GeometryShape.Cylinder,
            LocalOffset = localOffset,
            CylinderRadius = radius,
            CylinderHeight = height, // Extends along local +Z; radius in local XY.
        };

        public static GeometryVolume Sphere(Float3 localOffset, float radius) => new()
        {
            Shape = GeometryShape.Sphere,
            LocalOffset = localOffset,
            SphereRadius = radius,
        };
        
        private bool IsBox() => Shape == GeometryShape.Box;
        private bool IsCylinder() => Shape == GeometryShape.Cylinder;
        private bool IsSphere() => Shape == GeometryShape.Sphere;
    }
}
