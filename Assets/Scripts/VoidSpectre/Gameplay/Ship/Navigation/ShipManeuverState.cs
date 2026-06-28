using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Ship.Navigation
{
    /// <summary>
    /// Backward-compatible alias. Use <see cref="ShipSimState"/> for new code.
    /// </summary>
    public struct ShipManeuverState
    {
        public Float3 Position;
        public Float3 Velocity;
        public FloatQuaternion Orientation;
        public Float3 AngularVelocityLocal;
        public float[] ThrusterPower;
        public float[] GimbalDegrees;
        public float[] GimbalTargetDegrees;

        public static implicit operator ShipSimState(ShipManeuverState legacy) => new()
        {
            Position = legacy.Position,
            Velocity = legacy.Velocity,
            Orientation = legacy.Orientation,
            AngularVelocityLocal = legacy.AngularVelocityLocal,
            ThrusterPower = legacy.ThrusterPower,
            GimbalDegrees = legacy.GimbalDegrees,
            GimbalTargetDegrees = legacy.GimbalTargetDegrees,
        };

        public static implicit operator ShipManeuverState(ShipSimState state) => new()
        {
            Position = state.Position,
            Velocity = state.Velocity,
            Orientation = state.Orientation,
            AngularVelocityLocal = state.AngularVelocityLocal,
            ThrusterPower = state.ThrusterPower,
            GimbalDegrees = state.GimbalDegrees,
            GimbalTargetDegrees = state.GimbalTargetDegrees,
        };

        public ShipManeuverState Clone() => ((ShipSimState)this).Clone();

        public static ShipManeuverState FromLive(
            Core.Context.SimulationContext context,
            Core.ComponentStore.EntityId ship,
            ShipPlantModel plant) =>
            (ShipManeuverState)ShipSimState.FromLive(context, ship, plant);
    }
}
