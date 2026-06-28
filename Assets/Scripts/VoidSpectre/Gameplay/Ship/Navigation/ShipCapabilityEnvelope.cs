namespace VoidSpectre.Gameplay.Ship.Navigation
{
    public enum ShipFlightReadiness
    {
        Grounded = 0,
        LimitedFlight = 1,
        Spaceworthy = 2,
        CombatReady = 3,
    }

    public sealed class ShipCapabilityEnvelope
    {
        public float MaxForwardAcceleration;
        public float MaxReverseAcceleration;
        public float MaxLateralAcceleration;

        public float PitchTorque;
        public float YawTorque;
        public float RollTorque;

        public float MaxBrakingAcceleration;
        public float TurnRateScore;
        public float FuelEfficiencyScore;
        public float CombatManeuverScore;

        public bool CanBrake;
        public bool CanDock;
        public bool CanStrafe;
        public bool CanFlipAndBurn;
    }

    public sealed class ShipGeneralValidationResult
    {
        public ShipFlightReadiness Readiness = ShipFlightReadiness.Grounded;
        public System.Collections.Generic.List<string> BlockingIssues = new();
        public System.Collections.Generic.List<string> Warnings = new();
        public ShipCapabilityEnvelope Capabilities;
    }

    public sealed class GoalReachabilityResult
    {
        public bool IsReachable;
        public string FailureReason;
        public float EstimatedTime;
        public float EstimatedFuel;
        public float RiskScore;
    }
}
