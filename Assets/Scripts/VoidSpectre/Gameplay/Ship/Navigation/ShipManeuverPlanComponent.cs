using System;
using System.Collections.Generic;
using Sirenix.Serialization;
using VoidSpectre.Core.Component;
using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Ship.Navigation
{
    [Serializable]
    public sealed class ShipManeuverPlanComponent : TrackableComponentBase
    {
        private static int _nextPlanId = 1;

        [OdinSerialize] private int _planId;
        [OdinSerialize] private bool _isValid;
        [OdinSerialize] private float _totalDuration;
        [OdinSerialize] private float _fixedDt = 1f / 60f;
        [OdinSerialize] private int _contextValidityHash;
        [OdinSerialize] private ShipContextSnapshot _planContextSnapshot;
        [OdinSerialize] private Float3 _plannedTargetPoint;
        [OdinSerialize] private ShipNavigationMode _plannedMode = ShipNavigationMode.Idle;
        [OdinSerialize] private float _plannedMaxApproachSpeed;
        [OdinSerialize] private float _plannedArrivalRadius;
        [OdinSerialize] private List<ShipManeuverSegmentData> _segments = new();
        [OdinSerialize] private List<ShipManeuverSample> _previewSamples = new();
        [OdinSerialize] private List<ShipManeuverControlSample> _controlSamples = new();
        [OdinSerialize] private List<ShipPlanTick> _ticks = new();

        public int PlanId => _planId;
        public bool IsValid => _isValid;
        public float TotalDuration => _totalDuration;
        public float FixedDt => _fixedDt;
        public int TickCount => _ticks.Count;
        public int ContextValidityHash => _contextValidityHash;
        public ShipContextSnapshot PlanContextSnapshot => _planContextSnapshot;
        public Float3 PlannedTargetPoint => _plannedTargetPoint;
        public ShipNavigationMode PlannedMode => _plannedMode;
        public float PlannedMaxApproachSpeed => _plannedMaxApproachSpeed;
        public float PlannedArrivalRadius => _plannedArrivalRadius;
        public IReadOnlyList<ShipManeuverSegmentData> Segments => _segments;
        public IReadOnlyList<ShipManeuverSample> PreviewSamples => _previewSamples;
        public IReadOnlyList<ShipManeuverControlSample> ControlSamples => _controlSamples;
        public IReadOnlyList<ShipPlanTick> Ticks => _ticks;

        public void Clear()
        {
            _isValid = false;
            _totalDuration = 0f;
            _contextValidityHash = 0;
            _planContextSnapshot = null;
            _segments.Clear();
            _previewSamples.Clear();
            _controlSamples.Clear();
            _ticks.Clear();
            BumpVersion();
        }

        public void SetPlan(
            Float3 plannedTarget,
            ShipNavigationMode plannedMode,
            float plannedMaxApproachSpeed,
            float plannedArrivalRadius,
            ShipContextSnapshot contextSnapshot,
            List<ShipPlanTick> ticks,
            List<ShipManeuverSegmentData> segments = null,
            List<ShipManeuverSample> previewSamples = null,
            List<ShipManeuverControlSample> controlSamples = null)
        {
            _planId = _nextPlanId++;
            _isValid = true;
            _plannedTargetPoint = plannedTarget;
            _plannedMode = plannedMode;
            _plannedMaxApproachSpeed = plannedMaxApproachSpeed;
            _plannedArrivalRadius = plannedArrivalRadius;
            _fixedDt = contextSnapshot?.FixedDt ?? 1f / 60f;
            _contextValidityHash = contextSnapshot?.ValidityHash ?? 0;
            _planContextSnapshot = contextSnapshot;

            _ticks.Clear();
            if (ticks != null)
                _ticks.AddRange(ticks);

            _totalDuration = _ticks.Count > 0 ? (_ticks.Count - 1) * _fixedDt : 0f;

            _segments.Clear();
            if (segments != null)
                _segments.AddRange(segments);

            _previewSamples.Clear();
            if (previewSamples != null && previewSamples.Count > 0)
                _previewSamples.AddRange(previewSamples);
            else
                BuildPreviewFromTicks(_fixedDt);

            _controlSamples.Clear();
            if (controlSamples != null && controlSamples.Count > 0)
                _controlSamples.AddRange(controlSamples);
            else
                BuildLegacyControlsFromTicks(_fixedDt);

            BumpVersion();
        }

        public bool TryGetTick(int tickIndex, out ShipPlanTick tick)
        {
            if (!_isValid || tickIndex < 0 || tickIndex >= _ticks.Count)
            {
                tick = default;
                return false;
            }

            tick = _ticks[tickIndex];
            return true;
        }

        public bool MatchesGoal(
            ShipNavigationGoalComponent goal,
            float replanPositionThreshold)
        {
            if (!_isValid || goal == null)
                return false;

            if (goal.Mode != _plannedMode)
                return false;

            if ((goal.TargetPoint - _plannedTargetPoint).Magnitude > replanPositionThreshold)
                return false;

            if (VsMath.Abs(goal.MaxApproachSpeed - _plannedMaxApproachSpeed) > 0.01f)
                return false;

            if (VsMath.Abs(goal.ArrivalRadius - _plannedArrivalRadius) > 0.01f)
                return false;

            return true;
        }

        public bool TrySampleControlAt(float time, out ShipManeuverControlSample sample)
        {
            if (!_isValid || _controlSamples.Count == 0)
            {
                sample = default;
                return false;
            }

            if (time <= _controlSamples[0].Time)
            {
                sample = _controlSamples[0];
                return true;
            }

            for (int i = 1; i < _controlSamples.Count; i++)
            {
                if (time <= _controlSamples[i].Time)
                {
                    var a = _controlSamples[i - 1];
                    var b = _controlSamples[i];
                    var span = b.Time - a.Time;
                    var t = span > 1e-6f ? (time - a.Time) / span : 0f;
                    sample = new ShipManeuverControlSample(
                        time,
                        Lerp(a.DesiredForceWorld, b.DesiredForceWorld, t),
                        Lerp(a.DesiredTorqueWorld, b.DesiredTorqueWorld, t),
                        InterpolateThrottle(a.Throttle, b.Throttle, t));
                    return true;
                }
            }

            sample = _controlSamples[_controlSamples.Count - 1];
            return true;
        }

        public bool TrySamplePreviewAt(float time, out ShipManeuverSample sample)
        {
            if (!_isValid || _previewSamples.Count == 0)
            {
                sample = default;
                return false;
            }

            if (time <= _previewSamples[0].Time)
            {
                sample = _previewSamples[0];
                return true;
            }

            for (int i = 1; i < _previewSamples.Count; i++)
            {
                if (time <= _previewSamples[i].Time)
                {
                    var a = _previewSamples[i - 1];
                    var b = _previewSamples[i];
                    var span = b.Time - a.Time;
                    var t = span > 1e-6f ? (time - a.Time) / span : 0f;
                    sample = new ShipManeuverSample(
                        time,
                        Lerp(a.Position, b.Position, t),
                        b.Orientation);
                    return true;
                }
            }

            sample = _previewSamples[_previewSamples.Count - 1];
            return true;
        }

        private void BuildPreviewFromTicks(float fixedDt)
        {
            const int decimation = 6;
            for (int i = 0; i < _ticks.Count; i++)
            {
                if (i % decimation != 0 && i != _ticks.Count - 1)
                    continue;

                var tick = _ticks[i];
                _previewSamples.Add(new ShipManeuverSample(
                    i * fixedDt,
                    tick.ExpectedState.Position,
                    tick.ExpectedState.Orientation));
            }
        }

        private void BuildLegacyControlsFromTicks(float fixedDt)
        {
            for (int i = 0; i < _ticks.Count; i++)
            {
                var tick = _ticks[i];
                var throttle = tick.Controls?.TargetThrusterPower;
                _controlSamples.Add(new ShipManeuverControlSample(
                    i * fixedDt,
                    Float3.Zero,
                    Float3.Zero,
                    throttle != null ? (float[])throttle.Clone() : null));
            }
        }

        private static Float3 Lerp(Float3 a, Float3 b, float t) =>
            a + (b - a) * t;

        private static float[] InterpolateThrottle(float[] a, float[] b, float t)
        {
            if (a == null || b == null || a.Length == 0 || b.Length != a.Length)
                return a ?? b;

            var result = new float[a.Length];
            for (int i = 0; i < a.Length; i++)
                result[i] = a[i] + (b[i] - a[i]) * t;
            return result;
        }
    }
}
