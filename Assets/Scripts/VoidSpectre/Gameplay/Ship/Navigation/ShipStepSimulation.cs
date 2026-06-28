using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Ship.Navigation
{
    /// <summary>
    /// Single source of ship motion truth for planning and execution.
    /// </summary>
    public static class ShipStepSimulation
    {
        public const float AngularDampingPerSecond = 0.15f;

        public static ShipSimState StepShipSim(
            ShipSimState state,
            ShipControlInput controls,
            ShipContextSnapshot context,
            float fixedDt)
        {
            if (fixedDt <= 0f || context?.Plant == null)
                return state;

            return StepInternal(state, controls, context.Plant, context.Gravity, context.FuelLitersPerSecondAtFullPower, fixedDt);
        }

        public static ShipSimState StepShipSim(
            ShipSimState state,
            float[] targetThrottle,
            ShipPlantModel plant,
            ShipGravityModel gravity,
            float fixedDt)
        {
            if (fixedDt <= 0f)
                return state;

            var controls = new ShipControlInput
            {
                TargetThrusterPower = targetThrottle,
                TargetGimbalDegrees = state.GimbalTargetDegrees,
            };
            return StepInternal(state, controls, plant, gravity, null, fixedDt);
        }

        public static ShipSimState StepShipSim(
            ShipSimState state,
            Float3 desiredForceBody,
            Float3 desiredTorqueBody,
            float forceWeight,
            float torqueWeight,
            ShipContextSnapshot context,
            float fixedDt)
        {
            var throttle = ShipControlAllocator.Solve(
                context.Plant,
                desiredForceBody,
                desiredTorqueBody,
                forceWeight,
                torqueWeight,
                ShipControlAllocator.DefaultMaxIterations);

            var controls = new ShipControlInput
            {
                TargetThrusterPower = throttle,
                TargetGimbalDegrees = state.GimbalTargetDegrees,
            };
            return StepShipSim(state, controls, context, fixedDt);
        }

        private static ShipSimState StepInternal(
            ShipSimState state,
            ShipControlInput controls,
            ShipPlantModel plant,
            ShipGravityModel gravity,
            float[] fuelLitersPerSecondAtFullPower,
            float delta)
        {
            var next = state.Clone();
            var n = plant.ThrusterCount;

            EnsureArray(ref next.ThrusterPower, n);
            EnsureArray(ref next.TargetThrusterPower, n);
            EnsureArray(ref next.GimbalDegrees, n);
            EnsureArray(ref next.GimbalTargetDegrees, n);

            if (controls?.TargetGimbalDegrees != null)
            {
                for (int i = 0; i < n && i < controls.TargetGimbalDegrees.Length; i++)
                    next.GimbalTargetDegrees[i] = controls.TargetGimbalDegrees[i];
            }

            for (int i = 0; i < n; i++)
            {
                if (!plant.HasGimbal[i])
                    continue;

                var maxStep = plant.GimbalMaxSpeedDegreesPerSecond[i] * delta;
                next.GimbalDegrees[i] = VsMath.MoveTowards(
                    next.GimbalDegrees[i],
                    next.GimbalTargetDegrees[i],
                    maxStep);
            }

            plant.ApplyGimbalAngles(next.GimbalDegrees);

            var fuelRemaining = next.FuelLiters;
            var fuelCutout = false;

            for (int i = 0; i < n; i++)
            {
                var target = controls?.TargetThrusterPower != null && i < controls.TargetThrusterPower.Length
                    ? VsMath.Clamp01(controls.TargetThrusterPower[i])
                    : 0f;

                next.TargetThrusterPower[i] = target;

                if (fuelCutout || (fuelLitersPerSecondAtFullPower != null && fuelRemaining <= 0f))
                {
                    next.TargetThrusterPower[i] = 0f;
                    next.ThrusterPower[i] = 0f;
                    fuelCutout = true;
                    continue;
                }

                var rampRate = 1f / VsMath.Max(plant.RampUpSeconds[i], 0.01f);
                if (target <= 0f)
                    next.ThrusterPower[i] = 0f;
                else
                    next.ThrusterPower[i] = VsMath.MoveTowards(next.ThrusterPower[i], target, rampRate * delta);

                if (fuelLitersPerSecondAtFullPower != null &&
                    i < fuelLitersPerSecondAtFullPower.Length &&
                    next.ThrusterPower[i] > 0f &&
                    fuelLitersPerSecondAtFullPower[i] > 0f)
                {
                    var requested = fuelLitersPerSecondAtFullPower[i] * next.ThrusterPower[i] * delta;
                    if (requested > fuelRemaining)
                    {
                        fuelRemaining = 0f;
                        next.ThrusterPower[i] = 0f;
                        next.TargetThrusterPower[i] = 0f;
                        fuelCutout = true;
                    }
                    else
                    {
                        fuelRemaining -= requested;
                    }
                }
            }

            next.FuelLiters = fuelRemaining;

            var forceWorld = Float3.Zero;
            var torqueLocal = Float3.Zero;
            var mass = VsMath.Max(next.MassKg, plant.MassKg);
            var inertia = VsMath.Max(next.MomentOfInertia, plant.Inertia);

            for (int i = 0; i < n; i++)
            {
                var power = next.ThrusterPower[i];
                if (power <= 1e-6f) continue;

                var thrustNewtons = plant.MaxThrustNewtons[i] * power;
                var forceBody = plant.ThrustDirectionBody[i] * thrustNewtons;
                var forceWorldThruster = next.Orientation * forceBody;
                forceWorld += forceWorldThruster;

                var leverWorld = next.Orientation * plant.LeverArmBody[i];
                var torqueWorld = Float3.Cross(leverWorld, forceWorldThruster);
                torqueLocal += next.Orientation.Inverse() * torqueWorld;
            }

            if (gravity != null && mass > 1e-6f)
                forceWorld += gravity.ComputeForce(next.Position);

            if (inertia > 1e-6f)
            {
                next.AngularVelocityLocal += torqueLocal / inertia * delta;

                var angularSpeed = next.AngularVelocityLocal.Magnitude;
                if (angularSpeed > 1e-6f)
                {
                    var deltaRot = FloatQuaternion.AngleAxis(
                        angularSpeed * VsMath.Rad2Deg * delta,
                        next.AngularVelocityLocal.Normalized);
                    next.Orientation = next.Orientation * deltaRot;
                }

                next.AngularVelocityLocal *= VsMath.Clamp01(1f - AngularDampingPerSecond * delta);
            }

            if (mass > 1e-6f)
            {
                next.Velocity += forceWorld / mass * delta;
                next.Position += next.Velocity * delta;
            }

            return next;
        }

        private static void EnsureArray(ref float[] array, int length)
        {
            if (array == null || array.Length != length)
                array = new float[length];
        }
    }
}
