using UnityEngine;
using VoidSpectre.Core.Math;

namespace VoidSpectreUnity.Conversion
{
    public static class FloatQuaternionConversion
    {
        public static Quaternion ToUnity(this FloatQuaternion q) => new(q.X, q.Y, q.Z, q.W);

        public static FloatQuaternion ToFloatQuaternion(this Quaternion q) =>
            new(q.x, q.y, q.z, q.w);
    }
}
