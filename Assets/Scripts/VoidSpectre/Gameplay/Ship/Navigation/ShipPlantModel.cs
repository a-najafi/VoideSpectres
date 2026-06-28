using System.Collections.Generic;
using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Math;
using VoidSpectre.Gameplay.Physics;
using VoidSpectre.Gameplay.Ship.Parts;
using VoidSpectre.Gameplay.Ship.Thrusters;

namespace VoidSpectre.Gameplay.Ship.Navigation
{
    /// <summary>
    /// Snapshot of the ship as a control plant: thruster effectiveness at full throttle,
    /// mass properties, and derived acceleration limits.
    /// </summary>
    public sealed class ShipPlantModel
    {
        public ComponentStore.EntityId Ship;
        public Float3 CenterOfMassLocal;
        public float MassKg;
        public float Inertia;

        public int ThrusterCount => ThrusterEntities?.Length ?? 0;
        public ComponentStore.EntityId[] ThrusterEntities = System.Array.Empty<ComponentStore.EntityId>();
        public Float3[] ForceAtFullThrottleBody = System.Array.Empty<Float3>();
        public Float3[] TorqueAtFullThrottleBody = System.Array.Empty<Float3>();
        public Float3[] ThrustDirectionBody = System.Array.Empty<Float3>();
        public Float3[] LeverArmBody = System.Array.Empty<Float3>();
        public float[] MaxThrustNewtons = System.Array.Empty<float>();
        public float[] RampUpSeconds = System.Array.Empty<float>();

        public bool[] HasGimbal = System.Array.Empty<bool>();
        public Float3[] GimbalAxisLocal = System.Array.Empty<Float3>();
        public float[] GimbalMaxSpeedDegreesPerSecond = System.Array.Empty<float>();
        public Float3[] ThrusterLocalPosition = System.Array.Empty<Float3>();
        public FloatQuaternion[] ThrusterLocalRotation = System.Array.Empty<FloatQuaternion>();

        public float MaxForwardAccel;
        public float MaxRetroAccel;
        public float MaxForwardForceCapacity;

        public static ShipPlantModel Build(SimulationContext context, ComponentStore.EntityId ship)
        {
            var plant = new ShipPlantModel { Ship = ship };

            if (!context.Components.TryGet(ship, out ShipAggregateComponent aggregate))
                return plant;

            plant.CenterOfMassLocal = aggregate.CenterOfMassLocal;
            plant.MassKg = aggregate.TotalMassKg;
            plant.Inertia = VsMath.Max(aggregate.ApproximateMomentOfInertia, 1e-3f);

            var thrusters = new List<(
                ComponentStore.EntityId entity,
                Float3 forceBody,
                Float3 torqueBody,
                Float3 dirBody,
                Float3 lever,
                float maxThrust,
                float ramp)>();

            var rows = new List<ThrusterWrenchRow>();
            ShipThrustModel.BuildRows(context, ship, plant.CenterOfMassLocal, rows);

            for (int i = 0; i < rows.Count; i++)
            {
                var row = rows[i];
                if (!context.Components.TryGet(row.Entity, out ThrusterComponent thruster)) continue;
                if (!context.Components.TryGet(row.Entity, out LocalTransformComponent local)) continue;

                var lever = local.LocalPosition - plant.CenterOfMassLocal;
                var dir = row.ForceLocal.SqrMagnitude > 1e-6f
                    ? row.ForceLocal.Normalized
                    : Float3.Forward;

                thrusters.Add((
                    row.Entity,
                    row.ForceLocal,
                    row.TorqueLocal,
                    dir,
                    lever,
                    thruster.MaxThrustNewtons,
                    thruster.RampUpSeconds));
            }

            var n = thrusters.Count;
            plant.ThrusterEntities = new ComponentStore.EntityId[n];
            plant.ForceAtFullThrottleBody = new Float3[n];
            plant.TorqueAtFullThrottleBody = new Float3[n];
            plant.ThrustDirectionBody = new Float3[n];
            plant.LeverArmBody = new Float3[n];
            plant.MaxThrustNewtons = new float[n];
            plant.RampUpSeconds = new float[n];
            plant.HasGimbal = new bool[n];
            plant.GimbalAxisLocal = new Float3[n];
            plant.GimbalMaxSpeedDegreesPerSecond = new float[n];
            plant.ThrusterLocalPosition = new Float3[n];
            plant.ThrusterLocalRotation = new FloatQuaternion[n];

            for (int i = 0; i < n; i++)
            {
                var t = thrusters[i];
                plant.ThrusterEntities[i] = t.entity;
                plant.ForceAtFullThrottleBody[i] = t.forceBody;
                plant.TorqueAtFullThrottleBody[i] = t.torqueBody;
                plant.ThrustDirectionBody[i] = t.dirBody;
                plant.LeverArmBody[i] = t.lever;
                plant.MaxThrustNewtons[i] = t.maxThrust;
                plant.RampUpSeconds[i] = t.ramp;

                if (context.Components.TryGet(t.entity, out LocalTransformComponent local))
                {
                    plant.ThrusterLocalPosition[i] = local.LocalPosition;
                    plant.ThrusterLocalRotation[i] = local.LocalRotation;
                }

                if (context.Components.TryGet(t.entity, out GimbalThrusterComponent gimbal))
                {
                    plant.HasGimbal[i] = true;
                    plant.GimbalAxisLocal[i] = gimbal.GimbalAxisLocal;
                    plant.GimbalMaxSpeedDegreesPerSecond[i] = gimbal.MaxGimbalSpeedDegreesPerSecond;
                }
            }

            plant.MaxForwardForceCapacity = ShipThrustModel.ForceCapacityAlong(rows, Float3.Forward);
            if (plant.MassKg > 1e-3f)
            {
                plant.MaxForwardAccel = plant.MaxForwardForceCapacity / plant.MassKg;
                plant.MaxRetroAccel = plant.MaxForwardAccel;
            }

            return plant;
        }

        public void ApplyGimbalAngles(float[] currentGimbalDegrees)
        {
            var n = ThrusterCount;
            for (int i = 0; i < n; i++)
            {
                var direction = ThrusterLocalRotation[i] * ThrusterComponent.PartLocalThrustDirection;
                if (HasGimbal[i] &&
                    currentGimbalDegrees != null &&
                    i < currentGimbalDegrees.Length)
                {
                    direction = FloatQuaternion.AngleAxis(
                        currentGimbalDegrees[i],
                        GimbalAxisLocal[i]) * direction;
                }

                direction = direction.SqrMagnitude > 1e-6f ? direction.Normalized : Float3.Forward;
                ThrustDirectionBody[i] = direction;

                var forceBody = direction * MaxThrustNewtons[i];
                ForceAtFullThrottleBody[i] = forceBody;
                TorqueAtFullThrottleBody[i] = Float3.Cross(LeverArmBody[i], forceBody);
            }
        }

        public void GetWrenchAtThrottle(float[] throttle, out Float3 forceBody, out Float3 torqueBody)
        {
            forceBody = Float3.Zero;
            torqueBody = Float3.Zero;
            if (throttle == null) return;

            var n = VsMath.Min(ThrusterCount, throttle.Length);
            for (int i = 0; i < n; i++)
            {
                var t = VsMath.Clamp01(throttle[i]);
                forceBody += ForceAtFullThrottleBody[i] * t;
                torqueBody += TorqueAtFullThrottleBody[i] * t;
            }
        }
    }
}
