using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Ship.Navigation
{
    public static class GoalReachabilityValidator
    {
        public static GoalReachabilityResult Validate(
            in ShipSimState state,
            Float3 target,
            ShipCapabilityEnvelope caps,
            float arrivalRadius = 5f)
        {
            var toTarget = target - state.Position;
            var distance = toTarget.Magnitude;

            if (distance <= arrivalRadius && state.Velocity.Magnitude <= 0.5f)
                return Reachable(0f, 0f);

            if (caps == null)
                return Unreachable("Ship capabilities are unknown.");

            if (distance <= 0.1f)
                return Reachable(0f, 0f);

            if (caps.PitchTorque <= 0.01f && caps.YawTorque <= 0.01f)
                return Unreachable("Ship cannot turn toward this destination.");

            var estimatedFuel = EstimateFuelCost(distance, caps);

            return Reachable(
                EstimateTravelTime(distance, caps),
                estimatedFuel);
        }

        public static float EstimateStoppingDistance(float speedTowardTarget, float brakingAcceleration)
        {
            if (brakingAcceleration <= 0f)
                return float.PositiveInfinity;

            var speed = VsMath.Max(0f, speedTowardTarget);
            return (speed * speed) / (2f * brakingAcceleration);
        }

        private static float EstimateTravelTime(float distance, ShipCapabilityEnvelope caps)
        {
            var accel = VsMath.Max(caps.MaxForwardAcceleration, 0.1f);
            return VsMath.Sqrt(2f * distance / accel) + distance / VsMath.Max(accel * 5f, 1f);
        }

        private static float EstimateFuelCost(float distance, ShipCapabilityEnvelope caps)
        {
            var efficiency = VsMath.Max(caps.FuelEfficiencyScore, 0.1f);
            return distance * 0.001f / efficiency;
        }

        private static GoalReachabilityResult Reachable(float time, float fuel) => new()
        {
            IsReachable = true,
            EstimatedTime = time,
            EstimatedFuel = fuel,
        };

        private static GoalReachabilityResult Unreachable(string reason) => new()
        {
            IsReachable = false,
            FailureReason = reason,
        };
    }
}
