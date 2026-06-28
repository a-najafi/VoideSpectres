using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Ship.Navigation
{
    /// <summary>
    /// Differential throttle solver for ships with four fixed forward main engines in a + cross
    /// (top / bottom / left / right). Matches Assets/Design/Ship.asset layout.
    /// </summary>
    public static class ShipQuadMainEngineAllocator
    {
        public struct QuadMainLayout
        {
            public int Top;
            public int Bottom;
            public int Left;
            public int Right;
        }

        public static bool TryClassify(ShipPlantModel plant, out QuadMainLayout layout)
        {
            layout = default;
            if (plant == null || plant.ThrusterCount != 4)
                return false;

            for (int i = 0; i < 4; i++)
            {
                if (plant.HasGimbal[i])
                    return false;

                if (Float3.Dot(plant.ThrustDirectionBody[i], Float3.Forward) < 0.85f)
                    return false;
            }

            var top = -1;
            var bottom = -1;
            var left = -1;
            var right = -1;
            var topY = float.NegativeInfinity;
            var bottomY = float.PositiveInfinity;
            var leftX = float.PositiveInfinity;
            var rightX = float.NegativeInfinity;

            for (int i = 0; i < 4; i++)
            {
                var pos = plant.ThrusterLocalPosition[i];
                if (pos.Y > topY)
                {
                    topY = pos.Y;
                    top = i;
                }

                if (pos.Y < bottomY)
                {
                    bottomY = pos.Y;
                    bottom = i;
                }

                if (pos.X < leftX)
                {
                    leftX = pos.X;
                    left = i;
                }

                if (pos.X > rightX)
                {
                    rightX = pos.X;
                    right = i;
                }
            }

            if (top < 0 || bottom < 0 || left < 0 || right < 0)
                return false;

            if (top == bottom || left == right)
                return false;

            layout = new QuadMainLayout
            {
                Top = top,
                Bottom = bottom,
                Left = left,
                Right = right,
            };
            return true;
        }

        public static float[] SolveRotate(ShipPlantModel plant, in QuadMainLayout layout, Float3 torqueBody)
        {
            var throttle = new float[plant.ThrusterCount];
            var pitchCmd = NormalizeTorqueCommand(
                torqueBody.X,
                plant.TorqueAtFullThrottleBody[layout.Top].X,
                plant.TorqueAtFullThrottleBody[layout.Bottom].X);
            var yawCmd = NormalizeTorqueCommand(
                torqueBody.Y,
                plant.TorqueAtFullThrottleBody[layout.Right].Y,
                plant.TorqueAtFullThrottleBody[layout.Left].Y);

            throttle[layout.Top] = VsMath.Max(0f, pitchCmd);
            throttle[layout.Bottom] = VsMath.Max(0f, -pitchCmd);
            throttle[layout.Left] = VsMath.Max(0f, yawCmd);
            throttle[layout.Right] = VsMath.Max(0f, -yawCmd);
            return throttle;
        }

        public static float[] SolveForward(ShipPlantModel plant, float forwardThrottle)
        {
            var throttle = new float[plant.ThrusterCount];
            var clamped = VsMath.Clamp01(forwardThrottle);
            for (int i = 0; i < throttle.Length; i++)
                throttle[i] = clamped;
            return throttle;
        }

        public static float[] SolveForwardWithAttitude(
            ShipPlantModel plant,
            in QuadMainLayout layout,
            float forwardThrottle,
            Float3 torqueBody,
            float attitudeMix = 0.45f)
        {
            var throttle = SolveForward(plant, forwardThrottle);
            var pitchCmd = NormalizeTorqueCommand(
                torqueBody.X,
                plant.TorqueAtFullThrottleBody[layout.Top].X,
                plant.TorqueAtFullThrottleBody[layout.Bottom].X);
            var yawCmd = NormalizeTorqueCommand(
                torqueBody.Y,
                plant.TorqueAtFullThrottleBody[layout.Right].Y,
                plant.TorqueAtFullThrottleBody[layout.Left].Y);

            var mix = VsMath.Clamp01(attitudeMix);
            throttle[layout.Top] = VsMath.Clamp01(throttle[layout.Top] + pitchCmd * mix);
            throttle[layout.Bottom] = VsMath.Clamp01(throttle[layout.Bottom] - pitchCmd * mix);
            throttle[layout.Left] = VsMath.Clamp01(throttle[layout.Left] + yawCmd * mix);
            throttle[layout.Right] = VsMath.Clamp01(throttle[layout.Right] - yawCmd * mix);
            return throttle;
        }

        private static float NormalizeTorqueCommand(float desired, float positiveTorque, float negativeTorque)
        {
            if (desired >= 0f)
            {
                var capacity = VsMath.Max(positiveTorque, 1e-3f);
                return VsMath.Clamp(desired / capacity, 0f, 1f);
            }

            var retroCapacity = VsMath.Max(-negativeTorque, 1e-3f);
            return -VsMath.Clamp(-desired / retroCapacity, 0f, 1f);
        }
    }
}
