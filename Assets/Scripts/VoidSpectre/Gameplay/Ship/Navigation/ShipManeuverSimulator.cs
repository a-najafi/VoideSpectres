using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Ship.Navigation
{
    /// <summary>
    /// Thin wrapper delegating to <see cref="ShipStepSimulation"/>.
    /// </summary>
    public static class ShipManeuverSimulator
    {
        public static ShipManeuverState SimulateStep(
            ShipManeuverState state,
            float[] targetThrottle,
            float delta,
            ShipPlantModel plant,
            ShipGravityModel gravity = null)
        {
            var simState = (ShipSimState)state;
            var result = ShipStepSimulation.StepShipSim(simState, targetThrottle, plant, gravity, delta);
            return (ShipManeuverState)result;
        }

        public static ShipManeuverState SimulateStep(
            ShipManeuverState state,
            Float3 desiredForceBody,
            Float3 desiredTorqueBody,
            float forceWeight,
            float torqueWeight,
            float delta,
            ShipPlantModel plant,
            ShipGravityModel gravity = null)
        {
            var context = new ShipContextSnapshot
            {
                FixedDt = delta,
                Plant = plant,
                Gravity = gravity,
            };
            var simState = (ShipSimState)state;
            var result = ShipStepSimulation.StepShipSim(
                simState,
                desiredForceBody,
                desiredTorqueBody,
                forceWeight,
                torqueWeight,
                context,
                delta);
            return (ShipManeuverState)result;
        }
    }
}
