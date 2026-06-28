using UnityEngine;
using VoidSpectre.Core.Math;

namespace VoidSpectreUnity.Conversion
{
    public static class Float3Conversion
    {
        public static Vector3 ToUnity(this Float3 v) => new(v.X, v.Y, v.Z);
        public static Float3 ToFloat3(this Vector3 v) => new(v.x, v.y, v.z);
    }
}
