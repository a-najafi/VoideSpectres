using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Gameplay.Ship.Parts;

namespace VoidSpectre.Gameplay.Ship.Navigation
{
    public static class ShipValidator
    {
        public static ShipGeneralValidationResult Validate(
            SimulationContext context,
            ComponentStore.EntityId ship,
            ShipPlantModel plant = null)
        {
            plant ??= ShipPlantModel.Build(context, ship);
            var result = new ShipGeneralValidationResult
            {
                Capabilities = ShipCapabilityAnalyzer.Analyze(plant),
            };

            if (plant.MassKg <= 0f)
                result.BlockingIssues.Add("Ship has no mass.");

            if (plant.ThrusterCount == 0)
                result.BlockingIssues.Add("Ship has no thrusters.");

            if (!ShipPartQueries.TryGetEngineFuel(context, ship, out _, out var fuel))
                result.Warnings.Add("Ship has no fuel source.");
            else if (fuel.CurrentFuelLiters <= 0f)
                result.Warnings.Add("Ship has no fuel.");

            var caps = result.Capabilities;
            if (caps.MaxForwardAcceleration <= 0.01f)
                result.BlockingIssues.Add("Ship cannot produce useful forward thrust.");

            if (caps.PitchTorque <= 0.01f && caps.YawTorque <= 0.01f)
                result.BlockingIssues.Add("Ship cannot rotate toward a target.");

            if (result.BlockingIssues.Count > 0)
                result.Readiness = ShipFlightReadiness.Grounded;
            else if (caps.CombatManeuverScore > 0.7f)
                result.Readiness = ShipFlightReadiness.CombatReady;
            else if (caps.TurnRateScore < 0.25f || caps.MaxForwardAcceleration < 0.5f)
                result.Readiness = ShipFlightReadiness.LimitedFlight;
            else
                result.Readiness = ShipFlightReadiness.Spaceworthy;

            return result;
        }
    }
}
