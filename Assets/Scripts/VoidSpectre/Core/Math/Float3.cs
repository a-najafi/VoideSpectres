using System;
using SysMath = System.Math;
using Sirenix.Serialization;

namespace VoidSpectre.Core.Math
{
    [Serializable]
    public struct Float3 : IEquatable<Float3>
    {
        [OdinSerialize] public float X;
        [OdinSerialize] public float Y;
        [OdinSerialize] public float Z;

        public static readonly Float3 Zero = new(0f, 0f, 0f);
        public static readonly Float3 Forward = new(0f, 0f, 1f);
        public static readonly Float3 Back = new(0f, 0f, -1f);
        public static readonly Float3 Up = new(0f, 1f, 0f);
        public static readonly Float3 Down = new(0f, -1f, 0f);
        public static readonly Float3 Left = new(-1f, 0f, 0f);
        public static readonly Float3 Right = new(1f, 0f, 0f);

        public Float3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float SqrMagnitude => X * X + Y * Y + Z * Z;
        public float Magnitude => (float)SysMath.Sqrt(SqrMagnitude);

        public Float3 Normalized
        {
            get
            {
                var mag = Magnitude;
                return mag > 1e-6f ? this / mag : Zero;
            }
        }

        public static float Dot(Float3 a, Float3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;

        public static Float3 Cross(Float3 a, Float3 b) => new(
            a.Y * b.Z - a.Z * b.Y,
            a.Z * b.X - a.X * b.Z,
            a.X * b.Y - a.Y * b.X);

        public static Float3 operator +(Float3 a, Float3 b) => new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Float3 operator -(Float3 a, Float3 b) => new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Float3 operator -(Float3 v) => new(-v.X, -v.Y, -v.Z);
        public static Float3 operator *(Float3 v, float s) => new(v.X * s, v.Y * s, v.Z * s);
        public static Float3 operator *(float s, Float3 v) => v * s;
        public static Float3 operator /(Float3 v, float s) => new(v.X / s, v.Y / s, v.Z / s);

        public bool Equals(Float3 other) => X.Equals(other.X) && Y.Equals(other.Y) && Z.Equals(other.Z);
        public override bool Equals(object obj) => obj is Float3 other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(X, Y, Z);
        public override string ToString() => $"({X:F2}, {Y:F2}, {Z:F2})";
    }
}
