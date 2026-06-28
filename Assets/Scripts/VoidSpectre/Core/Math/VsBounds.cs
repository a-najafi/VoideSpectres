using System;
using SysMath = System.Math;
using Sirenix.Serialization;

namespace VoidSpectre.Core.Math
{
    [Serializable]
    public struct VsBounds
    {
        [OdinSerialize] public Float3 Center;
        [OdinSerialize] public Float3 Size;

        public VsBounds(Float3 center, Float3 size)
        {
            Center = center;
            Size = size;
        }

        public Float3 Min => Center - Size * 0.5f;
        public Float3 Max => Center + Size * 0.5f;

        public void Encapsulate(Float3 point)
        {
            var min = Min;
            var max = Max;
            min = new Float3(
                SysMath.Min(min.X, point.X),
                SysMath.Min(min.Y, point.Y),
                SysMath.Min(min.Z, point.Z));
            max = new Float3(
                SysMath.Max(max.X, point.X),
                SysMath.Max(max.Y, point.Y),
                SysMath.Max(max.Z, point.Z));
            Center = (min + max) * 0.5f;
            Size = max - min;
        }

        public void Encapsulate(VsBounds other)
        {
            Encapsulate(other.Min);
            Encapsulate(other.Max);
        }
    }
}
