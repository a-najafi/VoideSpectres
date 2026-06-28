using System.Collections.Generic;
using VoidSpectre.Core.Math;
using VoidSpectre.Gameplay.Ship.Thrusters;

namespace VoidSpectre.Gameplay.Ship.Navigation
{
    public static class ShipCapabilityAnalyzer
    {
        public static ShipCapabilityEnvelope Analyze(ShipPlantModel plant)
        {
            var envelope = new ShipCapabilityEnvelope();
            if (plant == null || plant.ThrusterCount == 0 || plant.MassKg <= 0f)
                return envelope;

            var rows = new List<ThrusterWrenchRow>();
            for (int i = 0; i < plant.ThrusterCount; i++)
            {
                rows.Add(new ThrusterWrenchRow(
                    plant.ThrusterEntities[i],
                    plant.ForceAtFullThrottleBody[i],
                    plant.TorqueAtFullThrottleBody[i]));
            }

            var forwardForce = ShipThrustModel.ForceCapacityAlong(rows, Float3.Forward);
            var retroForce = ShipThrustModel.ForceCapacityAlong(rows, -Float3.Forward);
            var lateralForce = ShipThrustModel.ForceCapacityAlong(rows, Float3.Right);

            envelope.MaxForwardAcceleration = forwardForce / plant.MassKg;
            envelope.MaxReverseAcceleration = retroForce / plant.MassKg;
            envelope.MaxLateralAcceleration = lateralForce / plant.MassKg;
            envelope.MaxBrakingAcceleration = VsMath.Max(envelope.MaxReverseAcceleration, envelope.MaxForwardAcceleration);

            envelope.PitchTorque = ShipThrustModel.TorqueCapacityAbout(rows, Float3.Right);
            envelope.YawTorque = ShipThrustModel.TorqueCapacityAbout(rows, Float3.Up);
            envelope.RollTorque = ShipThrustModel.TorqueCapacityAbout(rows, Float3.Forward);

            envelope.CanFlipAndBurn = envelope.MaxForwardAcceleration > 0.05f &&
                                      (envelope.PitchTorque > 0.05f || envelope.YawTorque > 0.05f);
            envelope.CanBrake = envelope.MaxBrakingAcceleration > 0.05f || envelope.CanFlipAndBurn;
            envelope.CanStrafe = envelope.MaxLateralAcceleration > 0.05f;
            envelope.CanDock = envelope.CanStrafe && envelope.YawTorque > 0.05f;

            var turnAuthority = envelope.PitchTorque + envelope.YawTorque;
            envelope.TurnRateScore = VsMath.Clamp01(turnAuthority / VsMath.Max(plant.Inertia * 2f, 1f));
            envelope.FuelEfficiencyScore = VsMath.Clamp01(plant.MaxForwardAccel / 20f);
            envelope.CombatManeuverScore = VsMath.Clamp01(
                (envelope.TurnRateScore + (envelope.CanStrafe ? 0.5f : 0f)) * 0.5f);

            return envelope;
        }
    }
}
