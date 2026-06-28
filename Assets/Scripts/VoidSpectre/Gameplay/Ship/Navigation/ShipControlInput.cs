using System;

namespace VoidSpectre.Gameplay.Ship.Navigation
{
    /// <summary>
    /// Per-tick actuator commands replayed by planning and execution.
    /// </summary>
    public sealed class ShipControlInput
    {
        public float[] TargetThrusterPower = Array.Empty<float>();
        public float[] TargetGimbalDegrees = Array.Empty<float>();

        public static ShipControlInput Zero(int thrusterCount)
        {
            return new ShipControlInput
            {
                TargetThrusterPower = new float[thrusterCount],
                TargetGimbalDegrees = new float[thrusterCount],
            };
        }

        public ShipControlInput Clone()
        {
            return new ShipControlInput
            {
                TargetThrusterPower = CloneArray(TargetThrusterPower),
                TargetGimbalDegrees = CloneArray(TargetGimbalDegrees),
            };
        }

        private static float[] CloneArray(float[] source)
        {
            if (source == null || source.Length == 0)
                return Array.Empty<float>();

            var copy = new float[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }
    }
}
