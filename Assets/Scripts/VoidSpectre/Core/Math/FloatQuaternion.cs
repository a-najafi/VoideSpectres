using System;
using SysMath = System.Math;
using Sirenix.Serialization;

namespace VoidSpectre.Core.Math
{
    [Serializable]
    public struct FloatQuaternion : IEquatable<FloatQuaternion>
    {
        [OdinSerialize] public float X;
        [OdinSerialize] public float Y;
        [OdinSerialize] public float Z;
        [OdinSerialize] public float W;

        public static readonly FloatQuaternion Identity = new(0f, 0f, 0f, 1f);

        public FloatQuaternion(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public static FloatQuaternion AngleAxis(float degrees, Float3 axis)
        {
            var normalized = axis.Normalized;
            var rad = degrees * (float)SysMath.PI / 180f * 0.5f;
            var sin = (float)SysMath.Sin(rad);
            return new FloatQuaternion(
                normalized.X * sin,
                normalized.Y * sin,
                normalized.Z * sin,
                (float)SysMath.Cos(rad));
        }

        public static FloatQuaternion LookRotation(Float3 forward, Float3 up)
        {
            var f = forward.Normalized;
            var u = up.Normalized;
            var r = Float3.Cross(u, f).Normalized;
            u = Float3.Cross(f, r);

            var m00 = r.X; var m01 = u.X; var m02 = f.X;
            var m10 = r.Y; var m11 = u.Y; var m12 = f.Y;
            var m20 = r.Z; var m21 = u.Z; var m22 = f.Z;

            var trace = m00 + m11 + m22;
            if (trace > 0f)
            {
                var s = (float)SysMath.Sqrt(trace + 1f) * 2f;
                return new FloatQuaternion(
                    (m21 - m12) / s,
                    (m02 - m20) / s,
                    (m10 - m01) / s,
                    0.25f * s);
            }

            if (m00 > m11 && m00 > m22)
            {
                var s = (float)SysMath.Sqrt(1f + m00 - m11 - m22) * 2f;
                return new FloatQuaternion(0.25f * s, (m01 + m10) / s, (m02 + m20) / s, (m21 - m12) / s);
            }

            if (m11 > m22)
            {
                var s = (float)SysMath.Sqrt(1f + m11 - m00 - m22) * 2f;
                return new FloatQuaternion((m01 + m10) / s, 0.25f * s, (m12 + m21) / s, (m02 - m20) / s);
            }

            var s2 = (float)SysMath.Sqrt(1f + m22 - m00 - m11) * 2f;
            return new FloatQuaternion((m02 + m20) / s2, (m12 + m21) / s2, 0.25f * s2, (m10 - m01) / s2);
        }

        public FloatQuaternion Inverse()
        {
            var inv = 1f / (X * X + Y * Y + Z * Z + W * W);
            return new FloatQuaternion(-X * inv, -Y * inv, -Z * inv, W * inv);
        }

        public static Float3 operator *(FloatQuaternion q, Float3 v)
        {
            var qx = q.X; var qy = q.Y; var qz = q.Z; var qw = q.W;
            var ix = qw * v.X + qy * v.Z - qz * v.Y;
            var iy = qw * v.Y + qz * v.X - qx * v.Z;
            var iz = qw * v.Z + qx * v.Y - qy * v.X;
            var iw = -qx * v.X - qy * v.Y - qz * v.Z;
            return new Float3(
                ix * qw + iw * -qx + iy * -qz - iz * -qy,
                iy * qw + iw * -qy + iz * -qx - ix * -qz,
                iz * qw + iw * -qz + ix * -qy - iy * -qx);
        }

        public static FloatQuaternion operator *(FloatQuaternion a, FloatQuaternion b) => new(
            a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
            a.W * b.Y + a.Y * b.W + a.Z * b.X - a.X * b.Z,
            a.W * b.Z + a.Z * b.W + a.X * b.Y - a.Y * b.X,
            a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z);

        public bool Equals(FloatQuaternion other) =>
            X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z) && W.Equals(other.W);

        public override bool Equals(object obj) => obj is FloatQuaternion other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z, W);
    }
}
