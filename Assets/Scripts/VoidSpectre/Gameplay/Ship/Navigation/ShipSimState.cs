using System;
using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Math;
using VoidSpectre.Gameplay.Ship.Modules;
using VoidSpectre.Gameplay.Ship.Parts;
using VoidSpectre.Gameplay.Ship.Thrusters;
using VoidSpectre.Gameplay.Space;

namespace VoidSpectre.Gameplay.Ship.Navigation
{
    /// <summary>
    /// Complete ship state used by StepShipSim for planning and execution.
    /// </summary>
    public struct ShipSimState
    {
        public Float3 Position;
        public Float3 Velocity;
        public FloatQuaternion Orientation;
        public Float3 AngularVelocityLocal;

        public float[] ThrusterPower;
        public float[] TargetThrusterPower;
        public float[] GimbalDegrees;
        public float[] GimbalTargetDegrees;

        public float FuelLiters;
        public float MassKg;
        public Float3 CenterOfMassBody;
        public float MomentOfInertia;

        public static ShipSimState FromLive(
            SimulationContext context,
            ComponentStore.EntityId ship,
            ShipPlantModel plant)
        {
            var n = plant.ThrusterCount;
            var state = new ShipSimState
            {
                Position = Float3.Zero,
                Velocity = Float3.Zero,
                Orientation = FloatQuaternion.Identity,
                AngularVelocityLocal = Float3.Zero,
                ThrusterPower = new float[n],
                TargetThrusterPower = new float[n],
                GimbalDegrees = new float[n],
                GimbalTargetDegrees = new float[n],
                MassKg = plant.MassKg,
                CenterOfMassBody = plant.CenterOfMassLocal,
                MomentOfInertia = plant.Inertia,
            };

            if (context.Components.TryGet(ship, out SpacePositionComponent pos))
                state.Position = pos.Value;

            if (context.Components.TryGet(ship, out SpaceMoveComponent move))
                state.Velocity = move.Velocity;

            if (context.Components.TryGet(ship, out SpaceOrientationComponent orient))
                state.Orientation = orient.Value;

            if (context.Components.TryGet(ship, out ShipAngularStateComponent angular))
                state.AngularVelocityLocal = angular.AngularVelocityLocal;

            if (ShipPartQueries.TryGetEngineFuel(context, ship, out _, out var fuel))
                state.FuelLiters = fuel.CurrentFuelLiters;

            for (int i = 0; i < n; i++)
            {
                if (context.Components.TryGet(plant.ThrusterEntities[i], out ThrusterComponent thruster))
                {
                    state.ThrusterPower[i] = thruster.CurrentPower;
                    state.TargetThrusterPower[i] = thruster.TargetPower;
                }

                if (plant.HasGimbal[i] &&
                    context.Components.TryGet(plant.ThrusterEntities[i], out GimbalThrusterComponent gimbal))
                {
                    state.GimbalDegrees[i] = gimbal.CurrentGimbalDegrees;
                    state.GimbalTargetDegrees[i] = gimbal.TargetGimbalDegrees;
                }
            }

            return state;
        }

        public void ApplyToLive(
            SimulationContext context,
            ComponentStore.EntityId ship,
            ShipPlantModel plant)
        {
            if (context.Components.TryGet(ship, out SpacePositionComponent pos))
                pos.Value = Position;

            if (context.Components.TryGet(ship, out SpaceMoveComponent move))
                move.SetVelocity(Velocity);

            if (context.Components.TryGet(ship, out SpaceOrientationComponent orient))
                orient.Value = Orientation;

            if (context.Components.TryGet(ship, out ShipAngularStateComponent angular))
                angular.AngularVelocityLocal = AngularVelocityLocal;

            if (ShipPartQueries.TryGetEngineFuel(context, ship, out _, out var fuel))
                fuel.CurrentFuelLiters = FuelLiters;

            var n = plant.ThrusterCount;
            for (int i = 0; i < n; i++)
            {
                if (context.Components.TryGet(plant.ThrusterEntities[i], out ThrusterComponent thruster))
                {
                    thruster.CurrentPower = i < ThrusterPower.Length ? ThrusterPower[i] : 0f;
                    thruster.TargetPower = i < TargetThrusterPower.Length ? TargetThrusterPower[i] : 0f;
                }

                if (plant.HasGimbal[i] &&
                    context.Components.TryGet(plant.ThrusterEntities[i], out GimbalThrusterComponent gimbal))
                {
                    gimbal.CurrentGimbalDegrees = i < GimbalDegrees.Length ? GimbalDegrees[i] : 0f;
                    gimbal.TargetGimbalDegrees = i < GimbalTargetDegrees.Length ? GimbalTargetDegrees[i] : 0f;
                }
            }
        }

        public float PositionDistanceTo(in ShipSimState other) =>
            (Position - other.Position).Magnitude;

        public float OrientationErrorDegrees(in ShipSimState other)
        {
            var forwardA = Orientation * Float3.Forward;
            var forwardB = other.Orientation * Float3.Forward;
            var cos = VsMath.Clamp(Float3.Dot(forwardA, forwardB), -1f, 1f);
            return VsMath.Acos(cos) * VsMath.Rad2Deg;
        }

        public bool MatchesWithinTolerance(in ShipSimState other, float positionEpsilon, float orientationDegrees)
        {
            return PositionDistanceTo(other) <= positionEpsilon &&
                   OrientationErrorDegrees(other) <= orientationDegrees;
        }

        public ShipSimState Clone()
        {
            return new ShipSimState
            {
                Position = Position,
                Velocity = Velocity,
                Orientation = Orientation,
                AngularVelocityLocal = AngularVelocityLocal,
                ThrusterPower = CloneArray(ThrusterPower),
                TargetThrusterPower = CloneArray(TargetThrusterPower),
                GimbalDegrees = CloneArray(GimbalDegrees),
                GimbalTargetDegrees = CloneArray(GimbalTargetDegrees),
                FuelLiters = FuelLiters,
                MassKg = MassKg,
                CenterOfMassBody = CenterOfMassBody,
                MomentOfInertia = MomentOfInertia,
            };
        }

        private static float[] CloneArray(float[] source)
        {
            if (source == null)
                return null;

            var copy = new float[source.Length];
            Array.Copy(source, copy, source.Length);
            return copy;
        }
    }
}
