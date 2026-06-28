using SysMath = System.Math;

namespace VoidSpectre.Core.Math
{
    public static class VsMath
    {
        public const float PI = (float)SysMath.PI;
        public const float Rad2Deg = 180f / PI;
        public const float Deg2Rad = PI / 180f;

        public static float Abs(float v) => SysMath.Abs(v);
        public static float Max(float a, float b) => SysMath.Max(a, b);
        public static float Min(float a, float b) => SysMath.Min(a, b);
        public static float Clamp(float v, float min, float max) => SysMath.Max(min, SysMath.Min(max, v));
        public static float Clamp01(float v) => Clamp(v, 0f, 1f);
        public static float Sqrt(float v) => (float)SysMath.Sqrt(v);
        public static float Acos(float v) => (float)SysMath.Acos(Clamp(v, -1f, 1f));
        public static float Atan2(float y, float x) => (float)SysMath.Atan2(y, x);

        public static float MoveTowards(float current, float target, float maxDelta)
        {
            if (Abs(target - current) <= maxDelta) return target;
            return current + SysMath.Sign(target - current) * maxDelta;
        }
    }
}
