using System.Collections.Generic;
using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Ship.Navigation
{
    public struct ShipManeuverPrimitive
    {
        public string Id;
        public ShipControlInput Controls;
        public int DurationTicks;

        public ShipManeuverPrimitive(string id, ShipControlInput controls, int durationTicks)
        {
            Id = id;
            Controls = controls;
            DurationTicks = durationTicks;
        }
    }

    public static class ShipManeuverPrimitiveGenerator
    {
        public const int DefaultPrimitiveDurationTicks = 10;

        public static List<ShipManeuverPrimitive> Generate(ShipPlantModel plant, int durationTicks = DefaultPrimitiveDurationTicks)
        {
            var primitives = new List<ShipManeuverPrimitive>();
            var n = plant.ThrusterCount;
            if (n == 0)
                return primitives;

            primitives.Add(new ShipManeuverPrimitive("Coast", ShipControlInput.Zero(n), durationTicks));

            AddWrenchPrimitive(primitives, plant, "MainBurnForward",
                Float3.Forward * plant.MaxForwardForceCapacity,
                Float3.Zero, 1f, 0.1f, durationTicks);

            AddWrenchPrimitive(primitives, plant, "MainBurnRetro",
                -Float3.Forward * plant.MaxForwardForceCapacity,
                Float3.Zero, 1f, 0.1f, durationTicks);

            AddWrenchPrimitive(primitives, plant, "YawLeft",
                Float3.Zero,
                Float3.Up * EstimateMaxYawTorque(plant), 0.05f, 1f, durationTicks);

            AddWrenchPrimitive(primitives, plant, "YawRight",
                Float3.Zero,
                -Float3.Up * EstimateMaxYawTorque(plant), 0.05f, 1f, durationTicks);

            AddWrenchPrimitive(primitives, plant, "PitchUp",
                Float3.Zero,
                Float3.Right * EstimateMaxPitchTorque(plant), 0.05f, 1f, durationTicks);

            AddWrenchPrimitive(primitives, plant, "PitchDown",
                Float3.Zero,
                -Float3.Right * EstimateMaxPitchTorque(plant), 0.05f, 1f, durationTicks);

            AddWrenchPrimitive(primitives, plant, "StrafeLeft",
                -Float3.Right * EstimateMaxLateralForce(plant),
                Float3.Zero, 1f, 0.1f, durationTicks);

            AddWrenchPrimitive(primitives, plant, "StrafeRight",
                Float3.Right * EstimateMaxLateralForce(plant),
                Float3.Zero, 1f, 0.1f, durationTicks);

            return primitives;
        }

        private static void AddWrenchPrimitive(
            List<ShipManeuverPrimitive> primitives,
            ShipPlantModel plant,
            string id,
            Float3 forceBody,
            Float3 torqueBody,
            float forceWeight,
            float torqueWeight,
            int durationTicks)
        {
            var throttle = ShipControlAllocator.Solve(
                plant,
                forceBody,
                torqueBody,
                forceWeight,
                torqueWeight,
                ShipControlAllocator.DefaultMaxIterations);

            var controls = new ShipControlInput
            {
                TargetThrusterPower = (float[])throttle.Clone(),
                TargetGimbalDegrees = new float[plant.ThrusterCount],
            };
            primitives.Add(new ShipManeuverPrimitive(id, controls, durationTicks));
        }

        private static float EstimateMaxYawTorque(ShipPlantModel plant)
        {
            var max = 0f;
            for (int i = 0; i < plant.ThrusterCount; i++)
                max = VsMath.Max(max, VsMath.Abs(plant.TorqueAtFullThrottleBody[i].Y));
            return max;
        }

        private static float EstimateMaxPitchTorque(ShipPlantModel plant)
        {
            var max = 0f;
            for (int i = 0; i < plant.ThrusterCount; i++)
                max = VsMath.Max(max, VsMath.Abs(plant.TorqueAtFullThrottleBody[i].X));
            return max;
        }

        private static float EstimateMaxLateralForce(ShipPlantModel plant)
        {
            var max = 0f;
            for (int i = 0; i < plant.ThrusterCount; i++)
                max = VsMath.Max(max, VsMath.Abs(plant.ForceAtFullThrottleBody[i].X));
            return max;
        }
    }
}
