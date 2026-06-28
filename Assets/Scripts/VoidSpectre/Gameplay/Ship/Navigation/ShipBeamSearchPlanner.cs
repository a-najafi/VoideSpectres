using System.Collections.Generic;
using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Ship.Navigation
{
    public sealed class ShipBeamSearchPlanner
    {
        public int BeamWidth = 24;
        public int MaxDepth = 18;
        public float DistanceWeight = 10f;
        public float SpeedWeight = 2f;
        public float FuelWeight = 0.5f;
        public float TimeWeight = 0.1f;
        public float ControlChangeWeight = 0.05f;

        public bool TryBuildPlan(
            ShipSimState initialState,
            ShipContextSnapshot context,
            Float3 target,
            float arrivalRadius,
            float arrivalSpeedEpsilon,
            out List<ShipPlanTick> ticks)
        {
            ticks = null;
            if (context?.Plant == null)
                return false;

            var primitives = ShipManeuverPrimitiveGenerator.Generate(context.Plant);
            if (primitives.Count == 0)
                return false;

            var candidates = new List<BeamCandidate>
            {
                new(initialState.Clone(), new List<ShipPlanTick>(), 0, initialState.FuelLiters),
            };

            BeamCandidate best = null;
            var bestScore = float.PositiveInfinity;

            for (int depth = 0; depth < MaxDepth; depth++)
            {
                var nextCandidates = new List<BeamCandidate>();

                for (int c = 0; c < candidates.Count; c++)
                {
                    var candidate = candidates[c];
                    if (IsGoalReached(candidate.State, target, arrivalRadius, arrivalSpeedEpsilon))
                    {
                        var score = ScoreCandidate(candidate, target, arrivalSpeedEpsilon);
                        if (score < bestScore)
                        {
                            bestScore = score;
                            best = candidate;
                        }

                        continue;
                    }

                    for (int p = 0; p < primitives.Count; p++)
                    {
                        var primitive = primitives[p];
                        var branchState = candidate.State.Clone();
                        var branchTicks = new List<ShipPlanTick>(candidate.Ticks);
                        var startTick = branchTicks.Count;
                        var fuelStart = branchState.FuelLiters;

                        var primitiveTicks = ShipPlanSimulationUtility.SimulatePrimitive(
                            branchState,
                            context,
                            primitive,
                            startTick);

                        if (primitiveTicks.Count == 0)
                            continue;

                        branchState = primitiveTicks[primitiveTicks.Count - 1].ExpectedState;
                        ShipPlanSimulationUtility.AppendTicks(branchTicks, primitiveTicks);

                        var fuelUsed = candidate.FuelUsed + (fuelStart - branchState.FuelLiters);
                        var branch = new BeamCandidate(branchState, branchTicks, candidate.ControlChanges + 1, fuelUsed);
                        nextCandidates.Add(branch);
                    }
                }

                if (nextCandidates.Count == 0)
                    break;

                nextCandidates.Sort((a, b) =>
                    ScoreCandidate(a, target, arrivalSpeedEpsilon)
                        .CompareTo(ScoreCandidate(b, target, arrivalSpeedEpsilon)));

                if (nextCandidates.Count > BeamWidth)
                    nextCandidates.RemoveRange(BeamWidth, nextCandidates.Count - BeamWidth);

                candidates = nextCandidates;

                for (int i = 0; i < candidates.Count; i++)
                {
                    if (!IsGoalReached(candidates[i].State, target, arrivalRadius, arrivalSpeedEpsilon))
                        continue;

                    var score = ScoreCandidate(candidates[i], target, arrivalSpeedEpsilon);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = candidates[i];
                    }
                }

                if (best != null && bestScore < DistanceWeight * arrivalRadius)
                    break;
            }

            if (best == null)
            {
                candidates.Sort((a, b) =>
                    ScoreCandidate(a, target, arrivalSpeedEpsilon)
                        .CompareTo(ScoreCandidate(b, target, arrivalSpeedEpsilon)));
                if (candidates.Count > 0)
                    best = candidates[0];
            }

            if (best == null || best.Ticks.Count == 0)
                return false;

            ticks = best.Ticks;
            return true;
        }

        private float ScoreCandidate(BeamCandidate candidate, Float3 target, float arrivalSpeedEpsilon)
        {
            var state = candidate.State;
            var distance = (target - state.Position).Magnitude;
            var speed = state.Velocity.Magnitude;
            var duration = candidate.Ticks.Count * (candidate.Ticks.Count > 0 ? 1f : 1f);

            return DistanceWeight * distance +
                   SpeedWeight * VsMath.Max(0f, speed - arrivalSpeedEpsilon) +
                   FuelWeight * candidate.FuelUsed +
                   TimeWeight * duration +
                   ControlChangeWeight * candidate.ControlChanges;
        }

        private static bool IsGoalReached(
            ShipSimState state,
            Float3 target,
            float arrivalRadius,
            float arrivalSpeedEpsilon)
        {
            return (target - state.Position).Magnitude <= arrivalRadius &&
                   state.Velocity.Magnitude <= arrivalSpeedEpsilon;
        }

        private sealed class BeamCandidate
        {
            public ShipSimState State;
            public List<ShipPlanTick> Ticks;
            public int ControlChanges;
            public float FuelUsed;

            public BeamCandidate(ShipSimState state, List<ShipPlanTick> ticks, int controlChanges, float fuelUsed)
            {
                State = state;
                Ticks = ticks;
                ControlChanges = controlChanges;
                FuelUsed = fuelUsed;
            }
        }
    }
}
