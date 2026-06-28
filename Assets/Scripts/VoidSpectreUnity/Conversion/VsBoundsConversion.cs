using UnityEngine;
using VoidSpectre.Core.Math;

namespace VoidSpectreUnity.Conversion
{
    public static class VsBoundsConversion
    {
        public static Bounds ToUnity(this VsBounds b) => new(b.Center.ToUnity(), b.Size.ToUnity());

        public static VsBounds ToVsBounds(this Bounds b) => new(b.center.ToFloat3(), b.size.ToFloat3());
    }
}
