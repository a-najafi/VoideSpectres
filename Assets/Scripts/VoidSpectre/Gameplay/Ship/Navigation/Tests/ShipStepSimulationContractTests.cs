using System;
using System.Collections.Generic;
using VoidSpectre.Core.Diagnostics;
using VoidSpectre.Core.Math;
using VoidSpectre.Gameplay.Ship.Navigation;

namespace VoidSpectre.Gameplay.Ship.Navigation.Tests
{
    /// <summary>
    /// Pure C# contract tests: predicted state must equal executed state when replaying controls through StepShipSim.
    /// </summary>
    public static class ShipStepSimulationContractTests
    {
        private const float FixedDt = 1f / 60f;
        private const float PositionEpsilon = 1e-3f;
        private const float OrientationEpsilonDegrees = 0.1f;
        private const int TickCount = 120;

        public static bool RunAll()
        {
            var layouts = new[]
            {
                TestShipPlantLayouts.CreateStrongRearWeakTorque(),
                TestShipPlantLayouts.CreateBalancedRcs(),
                TestShipPlantLayouts.CreateAsymmetricSideThrusters(),
            };

            var allPassed = true;
            for (int i = 0; i < layouts.Length; i++)
            {
                if (!RunReplayContract(layouts[i], $"layout_{i}"))
                    allPassed = false;
            }

            if (allPassed)
                VsLog.Info("[ShipStepSimulationContractTests] All contract tests passed.");
            else
                VsLog.Warning(
                    $"[ShipStepSimulationContractTests] One or more contract tests failed.");

            return allPassed;
        }

        public static bool RunReplayContract(ShipPlantModel plant, string label)
        {
            var context = CreateContext(plant);
            var initial = CreateInitialState(plant);
            var controls = BuildTestControlSequence(plant);

            var predicted = new List<ShipSimState> { initial.Clone() };
            var state = initial.Clone();

            for (int t = 0; t < controls.Count; t++)
            {
                state = ShipStepSimulation.StepShipSim(state, controls[t], context, FixedDt);
                predicted.Add(state.Clone());
            }

            var executed = new List<ShipSimState> { initial.Clone() };
            state = initial.Clone();
            for (int t = 0; t < controls.Count; t++)
            {
                state = ShipStepSimulation.StepShipSim(state, controls[t], context, FixedDt);
                executed.Add(state.Clone());
            }

            for (int i = 0; i < predicted.Count; i++)
            {
                if (!predicted[i].MatchesWithinTolerance(executed[i], PositionEpsilon, OrientationEpsilonDegrees))
                {
                    VsLog.Warning(
                        $"[ShipStepSimulationContractTests] {label} mismatch at tick {i}: " +
                        $"posErr={predicted[i].PositionDistanceTo(executed[i]):E3}");
                    return false;
                }
            }

            VsLog.Info($"[ShipStepSimulationContractTests] {label} passed ({predicted.Count} ticks).");
            return true;
        }

        private static ShipContextSnapshot CreateContext(ShipPlantModel plant)
        {
            plant.ApplyGimbalAngles(new float[plant.ThrusterCount]);
            return new ShipContextSnapshot
            {
                FixedDt = FixedDt,
                Plant = plant,
                Gravity = null,
                FuelLitersPerSecondAtFullPower = new float[plant.ThrusterCount],
                ValidityHash = 1,
            };
        }

        private static ShipSimState CreateInitialState(ShipPlantModel plant) => new()
        {
            Position = Float3.Zero,
            Velocity = Float3.Zero,
            Orientation = FloatQuaternion.Identity,
            AngularVelocityLocal = Float3.Zero,
            ThrusterPower = new float[plant.ThrusterCount],
            TargetThrusterPower = new float[plant.ThrusterCount],
            GimbalDegrees = new float[plant.ThrusterCount],
            GimbalTargetDegrees = new float[plant.ThrusterCount],
            FuelLiters = 10000f,
            MassKg = plant.MassKg,
            CenterOfMassBody = plant.CenterOfMassLocal,
            MomentOfInertia = plant.Inertia,
        };

        private static List<ShipControlInput> BuildTestControlSequence(ShipPlantModel plant)
        {
            var sequence = new List<ShipControlInput>();
            for (int t = 0; t < TickCount; t++)
            {
                var controls = ShipControlInput.Zero(plant.ThrusterCount);
                if (t >= 20 && t < 60)
                    controls.TargetThrusterPower[0] = 1f;
                if (t >= 60 && t < 90 && plant.ThrusterCount > 1)
                    controls.TargetThrusterPower[1] = 0.8f;
                sequence.Add(controls);
            }

            return sequence;
        }
    }

    public static class TestShipPlantLayouts
    {
        public static ShipPlantModel CreateStrongRearWeakTorque()
        {
            var plant = CreateBasePlant();
            AddThruster(plant, 0, Float3.Back * 5f, Float3.Forward, 50000f, Float3.Zero);
            plant.ApplyGimbalAngles(new float[plant.ThrusterCount]);
            RecomputeLimits(plant);
            return plant;
        }

        public static ShipPlantModel CreateBalancedRcs()
        {
            var plant = CreateBasePlant();
            AddThruster(plant, 0, Float3.Back * 5f, Float3.Forward, 30000f, Float3.Zero);
            AddThruster(plant, 1, Float3.Left * 2f, Float3.Right, 5000f, Float3.Up * 2f);
            AddThruster(plant, 2, Float3.Right * 2f, Float3.Left, 5000f, Float3.Up * 2f);
            AddThruster(plant, 3, Float3.Up * 2f, Float3.Down, 5000f, Float3.Right * 2f);
            AddThruster(plant, 4, Float3.Down * 2f, Float3.Up, 5000f, Float3.Right * 2f);
            plant.ApplyGimbalAngles(new float[plant.ThrusterCount]);
            RecomputeLimits(plant);
            return plant;
        }

        public static ShipPlantModel CreateAsymmetricSideThrusters()
        {
            var plant = CreateBasePlant();
            AddThruster(plant, 0, Float3.Back * 5f, Float3.Forward, 40000f, Float3.Zero);
            AddThruster(plant, 1, Float3.Left * 3f, Float3.Right, 8000f, Float3.Up * 3f);
            AddThruster(plant, 2, Float3.Right * 1f, Float3.Left, 3000f, Float3.Up * 1f);
            plant.ApplyGimbalAngles(new float[plant.ThrusterCount]);
            RecomputeLimits(plant);
            return plant;
        }

        private static ShipPlantModel CreateBasePlant() => new()
        {
            MassKg = 1000f,
            Inertia = 800f,
            CenterOfMassLocal = Float3.Zero,
        };

        private static void AddThruster(
            ShipPlantModel plant,
            int index,
            Float3 localPosition,
            Float3 direction,
            float maxThrust,
            Float3 leverArm)
        {
            EnsureThrusterArrays(plant, index + 1);
            plant.ThrusterLocalPosition[index] = localPosition;
            plant.LeverArmBody[index] = leverArm.SqrMagnitude > 1e-6f ? leverArm : localPosition;
            plant.MaxThrustNewtons[index] = maxThrust;
            plant.RampUpSeconds[index] = 0.5f;
            plant.ThrustDirectionBody[index] = direction.Normalized;
            plant.ForceAtFullThrottleBody[index] = direction.Normalized * maxThrust;
            plant.TorqueAtFullThrottleBody[index] = Float3.Cross(plant.LeverArmBody[index], plant.ForceAtFullThrottleBody[index]);
        }

        private static void EnsureThrusterArrays(ShipPlantModel plant, int count)
        {
            if (plant.ThrusterCount >= count)
                return;

            plant.ThrusterEntities = new Core.ComponentStore.EntityId[count];
            plant.ForceAtFullThrottleBody = new Float3[count];
            plant.TorqueAtFullThrottleBody = new Float3[count];
            plant.ThrustDirectionBody = new Float3[count];
            plant.LeverArmBody = new Float3[count];
            plant.MaxThrustNewtons = new float[count];
            plant.RampUpSeconds = new float[count];
            plant.HasGimbal = new bool[count];
            plant.GimbalAxisLocal = new Float3[count];
            plant.GimbalMaxSpeedDegreesPerSecond = new float[count];
            plant.ThrusterLocalPosition = new Float3[count];
            plant.ThrusterLocalRotation = new FloatQuaternion[count];
        }

        private static void RecomputeLimits(ShipPlantModel plant)
        {
            plant.MaxForwardForceCapacity = 0f;
            for (int i = 0; i < plant.ThrusterCount; i++)
            {
                var projected = Float3.Dot(plant.ForceAtFullThrottleBody[i], Float3.Forward);
                if (projected > 0f)
                    plant.MaxForwardForceCapacity += projected;
            }

            plant.MaxForwardAccel = plant.MaxForwardForceCapacity / plant.MassKg;
            plant.MaxRetroAccel = plant.MaxForwardAccel;
        }
    }
}
