using System;
using Sirenix.Serialization;
using VoidSpectre.Core.Component;
using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Ship.Navigation
{
    [Serializable]
    public sealed class ShipPlanExecutionComponent : TrackableComponentBase
    {
        [OdinSerialize] private int _activePlanId;
        [OdinSerialize] private float _elapsedTime;
        [OdinSerialize] private int _tickIndex;
        [OdinSerialize] private float _fixedStepAccumulator;
        [OdinSerialize] private ShipPlanExecutionStatus _status = ShipPlanExecutionStatus.Idle;
        [OdinSerialize] private ShipPlanInvalidationReason _lastInvalidationReason = ShipPlanInvalidationReason.None;
        [OdinSerialize] private Float3 _failedGoalPoint;
        [OdinSerialize] private bool _hasFailedForGoal;

        public int ActivePlanId
        {
            get => _activePlanId;
            set => SetField(ref _activePlanId, value);
        }

        public float ElapsedTime
        {
            get => _elapsedTime;
            set => SetField(ref _elapsedTime, VsMath.Max(0f, value));
        }

        public int TickIndex
        {
            get => _tickIndex;
            set => SetField<int>(ref _tickIndex, (int)VsMath.Max(0, value));
        }

        public float FixedStepAccumulator
        {
            get => _fixedStepAccumulator;
            set => SetField(ref _fixedStepAccumulator, VsMath.Max(0f, value));
        }

        public ShipPlanExecutionStatus Status
        {
            get => _status;
            set => SetField(ref _status, value);
        }

        public ShipPlanInvalidationReason LastInvalidationReason
        {
            get => _lastInvalidationReason;
            set => SetField(ref _lastInvalidationReason, value);
        }

        public void BeginPlan(int planId, float fixedDt = 1f / 60f)
        {
            _activePlanId = planId;
            _elapsedTime = 0f;
            _tickIndex = 0;
            _fixedStepAccumulator = 0f;
            _status = ShipPlanExecutionStatus.Executing;
            _lastInvalidationReason = ShipPlanInvalidationReason.None;
            _hasFailedForGoal = false;
            BumpVersion();
        }

        public void MarkFailed(Float3 goalPoint, ShipPlanInvalidationReason reason = ShipPlanInvalidationReason.PlanningFailed)
        {
            _activePlanId = 0;
            _elapsedTime = 0f;
            _tickIndex = 0;
            _fixedStepAccumulator = 0f;
            _status = ShipPlanExecutionStatus.Failed;
            _lastInvalidationReason = reason;
            _failedGoalPoint = goalPoint;
            _hasFailedForGoal = true;
            BumpVersion();
        }

        public bool ShouldSkipReplanAfterFailure(
            ShipNavigationGoalComponent goal,
            float replanPositionThreshold)
        {
            if (!_hasFailedForGoal || goal == null)
                return false;

            if (_status != ShipPlanExecutionStatus.Failed)
                return false;

            if (_lastInvalidationReason != ShipPlanInvalidationReason.PlanningFailed)
                return false;

            return (goal.TargetPoint - _failedGoalPoint).Magnitude <= replanPositionThreshold;
        }

        public void Reset()
        {
            _activePlanId = 0;
            _elapsedTime = 0f;
            _tickIndex = 0;
            _fixedStepAccumulator = 0f;
            _status = ShipPlanExecutionStatus.Idle;
            _lastInvalidationReason = ShipPlanInvalidationReason.None;
            _hasFailedForGoal = false;
            BumpVersion();
        }
    }
}
