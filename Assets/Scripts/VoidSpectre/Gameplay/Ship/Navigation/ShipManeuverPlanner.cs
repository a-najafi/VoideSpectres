using System.Collections.Generic;
using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Diagnostics;
using VoidSpectre.Core.Math;
using VoidSpectre.Gameplay.Ship.Parts;

namespace VoidSpectre.Gameplay.Ship.Navigation
{
    public static class ShipManeuverPlanner
    {
        private const float MaxSimDurationCap = 180f;
        private const int MaxRefinementPasses = 4;

        private const float AlignAngleThreshold = 0.12f;
        private const float AlignRateThreshold = 0.2f;
        private const float RotateTorqueGain = 14f;
        private const float AttitudeHoldGain = 5f;

        private enum PlannerPhase
        {
            RotateToBearing,
            Accelerate,
            Coast,
            RotateToRetro,
            Decelerate,
            Hold,
            Done,
        }

        public static bool TryBuildPlan(
            SimulationContext context,
            ComponentStore.EntityId ship,
            ShipNavigationGoalComponent goal,
            ShipManeuverPlanComponent plan,
            ShipPlanningLodTier lodTier = ShipPlanningLodTier.Hero)
        {
            if (goal.Mode != ShipNavigationMode.MoveToPoint)
                return false;

            if (lodTier == ShipPlanningLodTier.Dormant)
                return false;

            var plant = ShipPlantModel.Build(context, ship);
            var general = ShipValidator.Validate(context, ship, plant);
            if (general.Readiness == ShipFlightReadiness.Grounded)
            {
                VsLog.Warning($"[ShipPlanner] Ship {ship}: cannot plan — {string.Join("; ", general.BlockingIssues)}");
                return false;
            }

            var initialState = ShipSimState.FromLive(context, ship, plant);
            var reachability = GoalReachabilityValidator.Validate(
                initialState,
                goal.TargetPoint,
                general.Capabilities,
                goal.ArrivalRadius);

            if (!reachability.IsReachable)
            {
                VsLog.Warning($"[ShipPlanner] Ship {ship}: {reachability.FailureReason}");
                return false;
            }

            var fixedDt = goal.PlanSimDeltaTime;
            var contextSnapshot = ShipContextSnapshot.Capture(context, ship, fixedDt);
            var toTarget = goal.TargetPoint - initialState.Position;
            var distance = toTarget.Magnitude;

            if (distance <= goal.ArrivalRadius &&
                initialState.Velocity.Magnitude <= goal.ArrivalSpeedEpsilon)
            {
                WriteHoldPlan(plan, goal, initialState, contextSnapshot);
                return true;
            }

            if (!goal.UseLegacyPhasePlanner && lodTier >= ShipPlanningLodTier.Visible)
            {
                var beamPlanner = CreateBeamPlannerForLod(lodTier);
                if (beamPlanner.TryBuildPlan(
                        initialState,
                        contextSnapshot,
                        goal.TargetPoint,
                        goal.ArrivalRadius,
                        goal.ArrivalSpeedEpsilon,
                        out var beamTicks) &&
                    beamTicks.Count > 0)
                {
                    var finalState = beamTicks[beamTicks.Count - 1].ExpectedState;
                    if (IsPlanGoalReached(finalState, goal))
                    {
                        CommitPlan(plan, goal, contextSnapshot, beamTicks);
                        return true;
                    }
                }
            }

            return TryBuildLegacyPhasePlan(
                context,
                ship,
                goal,
                plan,
                initialState,
                contextSnapshot,
                plant);
        }

        private static ShipBeamSearchPlanner CreateBeamPlannerForLod(ShipPlanningLodTier lodTier)
        {
            var planner = new ShipBeamSearchPlanner();
            switch (lodTier)
            {
                case ShipPlanningLodTier.Background:
                    planner.BeamWidth = 8;
                    planner.MaxDepth = 10;
                    break;
                case ShipPlanningLodTier.Visible:
                    planner.BeamWidth = 16;
                    planner.MaxDepth = 14;
                    break;
                default:
                    planner.BeamWidth = 24;
                    planner.MaxDepth = 18;
                    break;
            }

            return planner;
        }

        private static void CommitPlan(
            ShipManeuverPlanComponent plan,
            ShipNavigationGoalComponent goal,
            ShipContextSnapshot contextSnapshot,
            List<ShipPlanTick> ticks,
            List<ShipManeuverSegmentData> segments = null)
        {
            plan.SetPlan(
                goal.TargetPoint,
                goal.Mode,
                goal.MaxApproachSpeed,
                goal.ArrivalRadius,
                contextSnapshot,
                ticks,
                segments);
        }

        private static bool IsPlanGoalReached(ShipSimState state, ShipNavigationGoalComponent goal)
        {
            return (goal.TargetPoint - state.Position).Magnitude <= goal.ArrivalRadius &&
                   state.Velocity.Magnitude <= goal.ArrivalSpeedEpsilon;
        }

        private static bool TryBuildLegacyPhasePlan(
            SimulationContext context,
            ComponentStore.EntityId ship,
            ShipNavigationGoalComponent goal,
            ShipManeuverPlanComponent plan,
            ShipSimState initialState,
            ShipContextSnapshot contextSnapshot,
            ShipPlantModel plant)
        {
            var gravity = contextSnapshot.Gravity;
            var toTarget = goal.TargetPoint - initialState.Position;
            var distance = toTarget.Magnitude;
            var bearing = distance > 1e-3f ? toTarget / distance : ForwardWorld(initialState.Orientation);
            var cruiseScale = 1f;

            for (int pass = 0; pass < MaxRefinementPasses; pass++)
            {
                var vCruise = ComputeCruiseSpeed(goal, plant, distance, cruiseScale);
                if (TrySimulateLegacyPlan(
                        initialState,
                        contextSnapshot,
                        goal,
                        vCruise,
                        out var segments,
                        out var ticks,
                        out var reachedDone) &&
                    ticks.Count > 0)
                {
                    if (ShouldCommitPlan(initialState, goal, bearing, ticks, reachedDone))
                    {
                        CommitPlan(plan, goal, contextSnapshot, ticks, segments);
                        return true;
                    }
                }

                cruiseScale *= 0.85f;
            }

            VsLog.Warning(
                $"[ShipPlanner] Ship {ship}: legacy plan refinement failed " +
                $"(distance={distance:F1}m, thrusters={plant.ThrusterCount}, mass={plant.MassKg:F0}kg).");

            return false;
        }

        private static bool ShouldCommitPlan(
            ShipSimState initialState,
            ShipNavigationGoalComponent goal,
            Float3 initialBearing,
            List<ShipPlanTick> ticks,
            bool reachedDone)
        {
            if (ticks == null || ticks.Count == 0)
                return false;

            var finalState = ticks[ticks.Count - 1].ExpectedState;
            if (reachedDone || IsPlanGoalReached(finalState, goal))
                return true;

            var finalBearing = ComputeBearing(finalState, goal);
            if (!IsAlignedToDirection(finalState, finalBearing))
                return false;

            return MakesProgressTowardGoal(initialState, finalState, goal);
        }

        private static bool MakesProgressTowardGoal(
            ShipSimState initialState,
            ShipSimState finalState,
            ShipNavigationGoalComponent goal)
        {
            var initialDist = (goal.TargetPoint - initialState.Position).Magnitude;
            var finalDist = (goal.TargetPoint - finalState.Position).Magnitude;
            if (initialDist <= goal.ArrivalRadius)
                return true;

            return finalDist < initialDist - VsMath.Max(1f, initialDist * 0.01f);
        }

        private static Float3 ComputeBearing(ShipSimState state, ShipNavigationGoalComponent goal)
        {
            var toTarget = goal.TargetPoint - state.Position;
            return toTarget.SqrMagnitude > 1e-6f
                ? toTarget / toTarget.Magnitude
                : ForwardWorld(state.Orientation);
        }

        public static bool TryBuildPlan(
            SimulationContext context,
            ComponentStore.EntityId ship,
            ShipNavigationGoalComponent goal,
            ShipManeuverPlanComponent plan)
        {
            var lodTier = ShipPlanningLodTier.Hero;
            if (context.Components.TryGet(ship, out ShipPlanningLodComponent lod))
                lodTier = lod.Tier;

            return TryBuildPlan(context, ship, goal, plan, lodTier);
        }

        private static float[] SolvePlannerThrottle(
            ShipPlantModel plant,
            PlannerPhase phase,
            Float3 forceBody,
            Float3 torqueBody,
            float forceWeight,
            float torqueWeight)
        {
            if (ShipQuadMainEngineAllocator.TryClassify(plant, out var quad))
            {
                if (forceWeight <= 1e-3f)
                    return ShipQuadMainEngineAllocator.SolveRotate(plant, quad, torqueBody);

                var forwardThrottle = plant.MaxForwardForceCapacity > 1e-3f
                    ? VsMath.Clamp01(forceBody.Magnitude / plant.MaxForwardForceCapacity)
                    : 0f;

                if (phase is PlannerPhase.RotateToBearing or PlannerPhase.RotateToRetro)
                    return ShipQuadMainEngineAllocator.SolveRotate(plant, quad, torqueBody);

                if (torqueWeight > 0.1f)
                {
                    return ShipQuadMainEngineAllocator.SolveForwardWithAttitude(
                        plant,
                        quad,
                        forwardThrottle,
                        torqueBody);
                }

                return ShipQuadMainEngineAllocator.SolveForward(plant, forwardThrottle);
            }

            return ShipControlAllocator.Solve(
                plant,
                forceBody,
                torqueBody,
                forceWeight,
                torqueWeight,
                ShipControlAllocator.DefaultMaxIterations);
        }

        private static bool TrySimulateLegacyPlan(
            ShipSimState initialState,
            ShipContextSnapshot contextSnapshot,
            ShipNavigationGoalComponent goal,
            float vCruise,
            out List<ShipManeuverSegmentData> segments,
            out List<ShipPlanTick> ticks,
            out bool reachedDone)
        {
            segments = new List<ShipManeuverSegmentData>();
            ticks = new List<ShipPlanTick>();

            var plant = contextSnapshot.Plant;
            var gravity = contextSnapshot.Gravity;
            var state = initialState.Clone();
            plant.ApplyGimbalAngles(initialState.GimbalDegrees);
            var phase = PlannerPhase.RotateToBearing;
            var time = 0f;
            var simDt = contextSnapshot.FixedDt;
            var segmentStart = 0f;
            var segmentKind = ShipManeuverSegmentKind.RotateToDirection;
            var segmentDirection = ComputeBearing(state, goal);
            var segmentTargetSpeed = vCruise;
            var distance = (goal.TargetPoint - initialState.Position).Magnitude;
            var maxSimDuration = EstimateSimDuration(initialState, goal, plant, distance, vCruise);
            var tickIndex = 0;
            var bearing = ComputeBearing(state, goal);

            if (IsAlignedToDirection(state, bearing) && state.Velocity.Magnitude < 0.5f)
                phase = PlannerPhase.Accelerate;

            while (time < maxSimDuration && phase != PlannerPhase.Done)
            {
                bearing = ComputeBearing(state, goal);
                ComputeDesiredWrench(
                    phase,
                    state,
                    plant,
                    goal,
                    bearing,
                    vCruise,
                    out var forceBody,
                    out var torqueBody,
                    out var forceWeight,
                    out var torqueWeight);

                var throttle = SolvePlannerThrottle(
                    plant,
                    phase,
                    forceBody,
                    torqueBody,
                    forceWeight,
                    torqueWeight);

                var controls = new ShipControlInput
                {
                    TargetThrusterPower = CloneThrottle(throttle),
                    TargetGimbalDegrees = state.GimbalTargetDegrees != null
                        ? (float[])state.GimbalTargetDegrees.Clone()
                        : new float[plant.ThrusterCount],
                };

                state = ShipStepSimulation.StepShipSim(state, controls, contextSnapshot, simDt);
                ticks.Add(new ShipPlanTick(tickIndex++, controls.Clone(), state.Clone()));
                time += simDt;

                var nextPhase = AdvancePhase(phase, state, plant, goal, bearing, vCruise);
                if (nextPhase != phase)
                {
                    segments.Add(new ShipManeuverSegmentData
                    {
                        Kind = segmentKind,
                        StartTime = segmentStart,
                        EndTime = time,
                        ReferenceDirectionWorld = segmentDirection,
                        TargetSpeed = segmentTargetSpeed,
                    });

                    phase = nextPhase;
                    segmentStart = time;
                    (segmentKind, segmentDirection, segmentTargetSpeed) = DescribePhase(phase, state, bearing, vCruise);
                }
            }

            if (phase != PlannerPhase.Done)
            {
                reachedDone = IsPlanGoalReached(state, goal);
                return ticks.Count > 0;
            }

            segments.Add(new ShipManeuverSegmentData
            {
                Kind = ShipManeuverSegmentKind.Hold,
                StartTime = segmentStart,
                EndTime = time,
                ReferenceDirectionWorld = bearing,
                TargetSpeed = 0f,
            });

            reachedDone = true;
            return true;
        }

        private static void WriteHoldPlan(
            ShipManeuverPlanComponent plan,
            ShipNavigationGoalComponent goal,
            ShipSimState state,
            ShipContextSnapshot contextSnapshot)
        {
            var controls = ShipControlInput.Zero(contextSnapshot.Plant.ThrusterCount);
            var ticks = new List<ShipPlanTick>
            {
                new(0, controls.Clone(), state.Clone()),
            };

            plan.SetPlan(
                goal.TargetPoint,
                goal.Mode,
                goal.MaxApproachSpeed,
                goal.ArrivalRadius,
                contextSnapshot,
                ticks);
        }

        private static float ComputeCruiseSpeed(
            ShipNavigationGoalComponent goal,
            ShipPlantModel plant,
            float distance,
            float scale)
        {
            var maxByDistance = VsMath.Sqrt(2f * plant.MaxForwardAccel * VsMath.Max(0f, distance * 0.45f));
            return VsMath.Min(goal.MaxApproachSpeed, maxByDistance) * scale;
        }

        /// <summary>
        /// Upper bound on forward-sim time so planning stays bounded (was 900s → 18k steps at 0.05 dt).
        /// </summary>
        private static float EstimateSimDuration(
            ShipSimState initialState,
            ShipNavigationGoalComponent goal,
            ShipPlantModel plant,
            float distance,
            float vCruise)
        {
            var speed = VsMath.Max(initialState.Velocity.Magnitude, vCruise);
            var accelTime = plant.MaxForwardAccel > 1e-3f
                ? speed / plant.MaxForwardAccel
                : 10f;
            var travelTime = speed > 1e-3f ? distance / speed : distance;
            var rotateBudget = 60f;
            var estimate = rotateBudget + accelTime * 2f + travelTime * 2.5f + 30f;
            return VsMath.Min(MaxSimDurationCap, VsMath.Max(30f, estimate));
        }

        private static PlannerPhase AdvancePhase(
            PlannerPhase phase,
            ShipSimState state,
            ShipPlantModel plant,
            ShipNavigationGoalComponent goal,
            Float3 bearing,
            float vCruise)
        {
            var speed = state.Velocity.Magnitude;
            var distToTarget = (goal.TargetPoint - state.Position).Magnitude;
            var closingSpeed = Float3.Dot(state.Velocity, bearing);
            var brakeDistance = speed * speed / (2f * plant.MaxForwardAccel);

            switch (phase)
            {
                case PlannerPhase.RotateToBearing:
                    if (IsAlignedToDirection(state, bearing))
                        return PlannerPhase.Accelerate;
                    return phase;

                case PlannerPhase.Accelerate:
                    if (closingSpeed >= vCruise * 0.95f)
                        return PlannerPhase.Coast;
                    if (speed > goal.ArrivalSpeedEpsilon &&
                        distToTarget <= brakeDistance + goal.ArrivalRadius * 2f)
                        return PlannerPhase.RotateToRetro;
                    return phase;

                case PlannerPhase.Coast:
                    if (distToTarget <= brakeDistance + goal.ArrivalRadius)
                        return PlannerPhase.RotateToRetro;
                    if (speed <= goal.ArrivalSpeedEpsilon &&
                        distToTarget > goal.ArrivalRadius * 2f)
                        return PlannerPhase.Accelerate;
                    return phase;

                case PlannerPhase.RotateToRetro:
                    if (IsPlanGoalReached(state, goal))
                        return PlannerPhase.Hold;
                    if (speed <= goal.ArrivalSpeedEpsilon &&
                        distToTarget <= goal.ArrivalRadius * 1.5f)
                        return PlannerPhase.Hold;
                    if (IsAlignedToDirection(state, RetroDirection(state)))
                        return PlannerPhase.Decelerate;
                    return phase;

                case PlannerPhase.Decelerate:
                    if (IsPlanGoalReached(state, goal))
                        return PlannerPhase.Hold;
                    if (distToTarget <= goal.ArrivalRadius * 2.5f &&
                        speed <= VsMath.Max(goal.ArrivalSpeedEpsilon * 4f, vCruise * 0.4f))
                        return PlannerPhase.Hold;
                    return phase;

                case PlannerPhase.Hold:
                    return PlannerPhase.Done;

                default:
                    return PlannerPhase.Done;
            }
        }

        private static (ShipManeuverSegmentKind kind, Float3 dir, float speed) DescribePhase(
            PlannerPhase phase,
            ShipSimState state,
            Float3 bearing,
            float vCruise)
        {
            return phase switch
            {
                PlannerPhase.RotateToBearing => (
                    ShipManeuverSegmentKind.RotateToDirection, bearing, 0f),
                PlannerPhase.Accelerate => (
                    ShipManeuverSegmentKind.Accelerate, bearing, vCruise),
                PlannerPhase.Coast => (
                    ShipManeuverSegmentKind.Coast, bearing, vCruise),
                PlannerPhase.RotateToRetro => (
                    ShipManeuverSegmentKind.RotateToDirection, RetroDirection(state), 0f),
                PlannerPhase.Decelerate => (
                    ShipManeuverSegmentKind.Decelerate, RetroDirection(state), 0f),
                _ => (ShipManeuverSegmentKind.Hold, bearing, 0f),
            };
        }

        private static void ComputeDesiredWrench(
            PlannerPhase phase,
            ShipSimState state,
            ShipPlantModel plant,
            ShipNavigationGoalComponent goal,
            Float3 bearing,
            float vCruise,
            out Float3 forceBody,
            out Float3 torqueBody,
            out float forceWeight,
            out float torqueWeight)
        {
            switch (phase)
            {
                case PlannerPhase.RotateToBearing:
                    ComputeRotateWrench(state, plant, bearing, out forceBody, out torqueBody);
                    forceWeight = 0f;
                    torqueWeight = 1f;
                    return;

                case PlannerPhase.Accelerate:
                    ComputeAccelWrench(state, plant, bearing, vCruise, out forceBody, out torqueBody);
                    forceWeight = 1f;
                    torqueWeight = 0.25f;
                    return;

                case PlannerPhase.Coast:
                    ComputeAttitudeHoldWrench(state, plant, bearing, out forceBody, out torqueBody);
                    forceWeight = 0f;
                    torqueWeight = 1f;
                    return;

                case PlannerPhase.RotateToRetro:
                    ComputeRotateWrench(state, plant, RetroDirection(state), out forceBody, out torqueBody);
                    forceWeight = 0f;
                    torqueWeight = 1f;
                    return;

                case PlannerPhase.Decelerate:
                    ComputeDecelWrench(state, plant, out forceBody, out torqueBody);
                    forceWeight = 1f;
                    torqueWeight = 0.15f;
                    return;

                default:
                    forceBody = Float3.Zero;
                    torqueBody = Float3.Zero;
                    forceWeight = 1f;
                    torqueWeight = 1f;
                    return;
            }
        }

        public static void ComputeDesiredWrenchForSegment(
            ShipManeuverSegmentData segment,
            ShipSimState state,
            ShipPlantModel plant,
            ShipNavigationGoalComponent goal,
            out Float3 forceBody,
            out Float3 torqueBody,
            out float forceWeight,
            out float torqueWeight)
        {
            switch (segment.Kind)
            {
                case ShipManeuverSegmentKind.RotateToDirection:
                    ComputeRotateWrench(state, plant, segment.ReferenceDirectionWorld, out forceBody, out torqueBody);
                    forceWeight = 0f;
                    torqueWeight = 1f;
                    return;

                case ShipManeuverSegmentKind.Accelerate:
                    ComputeAccelWrench(state, plant, segment.ReferenceDirectionWorld, segment.TargetSpeed, out forceBody, out torqueBody);
                    forceWeight = 1f;
                    torqueWeight = 0.25f;
                    return;

                case ShipManeuverSegmentKind.Coast:
                    ComputeAttitudeHoldWrench(state, plant, segment.ReferenceDirectionWorld, out forceBody, out torqueBody);
                    forceWeight = 0f;
                    torqueWeight = 1f;
                    return;

                case ShipManeuverSegmentKind.Decelerate:
                    ComputeDecelWrench(state, plant, out forceBody, out torqueBody);
                    forceWeight = 1f;
                    torqueWeight = 0.15f;
                    return;

                default:
                    forceBody = Float3.Zero;
                    torqueBody = Float3.Zero;
                    forceWeight = 1f;
                    torqueWeight = 1f;
                    return;
            }
        }

        private static void ComputeRotateWrench(
            ShipSimState state,
            ShipPlantModel plant,
            Float3 targetDirectionWorld,
            out Float3 forceBody,
            out Float3 torqueBody)
        {
            var targetLocal = (state.Orientation.Inverse() * targetDirectionWorld).Normalized;
            var pitchError = VsMath.Atan2(targetLocal.Y, targetLocal.Z);
            var yawError = VsMath.Atan2(targetLocal.X, targetLocal.Z);

            torqueBody = new Float3(
                pitchError * RotateTorqueGain - state.AngularVelocityLocal.X * 2f,
                yawError * RotateTorqueGain - state.AngularVelocityLocal.Y * 2f,
                0f);

            ClampTorque(ref torqueBody, plant);
            forceBody = Float3.Zero;
        }

        private static void ComputeAttitudeHoldWrench(
            ShipSimState state,
            ShipPlantModel plant,
            Float3 targetDirectionWorld,
            out Float3 forceBody,
            out Float3 torqueBody)
        {
            var targetLocal = (state.Orientation.Inverse() * targetDirectionWorld).Normalized;
            var pitchError = VsMath.Atan2(targetLocal.Y, targetLocal.Z);
            var yawError = VsMath.Atan2(targetLocal.X, targetLocal.Z);

            torqueBody = new Float3(
                pitchError * AttitudeHoldGain - state.AngularVelocityLocal.X,
                yawError * AttitudeHoldGain - state.AngularVelocityLocal.Y,
                0f);

            ClampTorque(ref torqueBody, plant);
            forceBody = Float3.Zero;
        }

        private static void ComputeAccelWrench(
            ShipSimState state,
            ShipPlantModel plant,
            Float3 bearingWorld,
            float targetSpeed,
            out Float3 forceBody,
            out Float3 torqueBody)
        {
            if (!IsAlignedToDirection(state, bearingWorld))
            {
                ComputeRotateWrench(state, plant, bearingWorld, out forceBody, out torqueBody);
                return;
            }

            var closingSpeed = Float3.Dot(state.Velocity, bearingWorld);
            var speedError = targetSpeed - closingSpeed;
            var throttle = speedError > 0f
                ? VsMath.Clamp01(speedError / VsMath.Max(targetSpeed, 1f))
                : 0f;

            var thrustDirLocal = bearingWorld.SqrMagnitude > 1e-6f
                ? (state.Orientation.Inverse() * bearingWorld.Normalized).Normalized
                : Float3.Forward;
            forceBody = thrustDirLocal * (plant.MaxForwardForceCapacity * throttle);
            ComputeAttitudeHoldWrench(state, plant, bearingWorld, out _, out torqueBody);
        }

        private static void ComputeDecelWrench(
            ShipSimState state,
            ShipPlantModel plant,
            out Float3 forceBody,
            out Float3 torqueBody)
        {
            var speed = state.Velocity.Magnitude;
            var throttle = speed > 1e-3f
                ? VsMath.Clamp01(speed / VsMath.Max(plant.MaxForwardAccel, 1f))
                : 0f;

            Float3 thrustDirLocal;
            if (speed > 1e-3f)
            {
                var retroWorld = -state.Velocity / speed;
                thrustDirLocal = (state.Orientation.Inverse() * retroWorld).Normalized;
            }
            else
            {
                thrustDirLocal = Float3.Forward;
            }

            forceBody = thrustDirLocal * (plant.MaxForwardForceCapacity * VsMath.Max(throttle, 0.25f));
            torqueBody = -state.AngularVelocityLocal * AttitudeHoldGain;
            ClampTorque(ref torqueBody, plant);
        }

        private static void ClampTorque(ref Float3 torqueBody, ShipPlantModel plant)
        {
            var maxPitch = 0f;
            var maxYaw = 0f;
            for (int i = 0; i < plant.ThrusterCount; i++)
            {
                maxPitch = VsMath.Max(maxPitch, VsMath.Abs(plant.TorqueAtFullThrottleBody[i].X));
                maxYaw = VsMath.Max(maxYaw, VsMath.Abs(plant.TorqueAtFullThrottleBody[i].Y));
            }

            torqueBody = new Float3(
                VsMath.Clamp(torqueBody.X, -maxPitch, maxPitch),
                VsMath.Clamp(torqueBody.Y, -maxYaw, maxYaw),
                0f);
        }

        private static bool IsAlignedToDirection(ShipSimState state, Float3 directionWorld)
        {
            if (directionWorld.SqrMagnitude < 1e-6f)
                return true;

            var forward = ForwardWorld(state.Orientation);
            var cos = VsMath.Clamp(Float3.Dot(forward, directionWorld.Normalized), -1f, 1f);
            var angle = VsMath.Acos(cos);
            return angle <= AlignAngleThreshold &&
                   VsMath.Abs(state.AngularVelocityLocal.X) <= AlignRateThreshold &&
                   VsMath.Abs(state.AngularVelocityLocal.Y) <= AlignRateThreshold;
        }

        private static Float3 ForwardWorld(FloatQuaternion orientation) =>
            orientation * Float3.Forward;

        private static Float3 RetroDirection(ShipSimState state)
        {
            if (state.Velocity.SqrMagnitude < 1e-3f)
                return -ForwardWorld(state.Orientation);
            return -state.Velocity.Normalized;
        }

        private static float[] CloneThrottle(float[] throttle)
        {
            if (throttle == null)
                return null;

            var copy = new float[throttle.Length];
            System.Array.Copy(throttle, copy, throttle.Length);
            return copy;
        }
    }
}
