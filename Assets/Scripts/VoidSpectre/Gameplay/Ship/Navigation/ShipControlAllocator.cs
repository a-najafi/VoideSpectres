using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Ship.Navigation
{
    /// <summary>
    /// Solves for thruster throttles: minimize weighted ||B·t − w||² subject to 0 ≤ t ≤ 1.
    /// </summary>
    public static class ShipControlAllocator
    {
        public const int DefaultMaxIterations = 64;
        private const int MaxIterations = DefaultMaxIterations;
        private const float StepSize = 0.15f;
        private const float ConvergenceEpsilon = 1e-4f;

        public static float[] Solve(
            ShipPlantModel plant,
            Float3 desiredForceBody,
            Float3 desiredTorqueBody,
            float forceWeight = 1f,
            float torqueWeight = 1f,
            int maxIterations = MaxIterations)
        {
            var n = plant.ThrusterCount;
            if (n == 0) return System.Array.Empty<float>();

            var throttle = new float[n];
            var gradient = new float[n];

            for (int i = 0; i < n; i++)
            {
                var projectedForce = Float3.Dot(plant.ForceAtFullThrottleBody[i], desiredForceBody);
                var projectedTorque = Float3.Dot(plant.TorqueAtFullThrottleBody[i], desiredTorqueBody);
                var scale = VsMath.Max(
                    plant.ForceAtFullThrottleBody[i].Magnitude,
                    plant.TorqueAtFullThrottleBody[i].Magnitude);
                if (scale > 1e-3f)
                    throttle[i] = VsMath.Clamp01((projectedForce + projectedTorque) / scale);
            }

            for (int iter = 0; iter < maxIterations; iter++)
            {
                plant.GetWrenchAtThrottle(throttle, out var forceBody, out var torqueBody);
                var forceError = desiredForceBody - forceBody;
                var torqueError = desiredTorqueBody - torqueBody;

                var cost = WeightedSquaredError(forceError, torqueError, forceWeight, torqueWeight);
                if (cost < ConvergenceEpsilon)
                    break;

                for (int i = 0; i < n; i++)
                {
                    var df = plant.ForceAtFullThrottleBody[i];
                    var dt = plant.TorqueAtFullThrottleBody[i];
                    gradient[i] =
                        -2f * forceWeight * Float3.Dot(df, forceError) -
                        2f * torqueWeight * Float3.Dot(dt, torqueError);
                }

                for (int i = 0; i < n; i++)
                    throttle[i] = VsMath.Clamp01(throttle[i] - StepSize * gradient[i]);
            }

            return throttle;
        }

        private static float WeightedSquaredError(
            Float3 forceError,
            Float3 torqueError,
            float forceWeight,
            float torqueWeight)
        {
            return forceWeight * forceError.SqrMagnitude + torqueWeight * torqueError.SqrMagnitude;
        }
    }
}
