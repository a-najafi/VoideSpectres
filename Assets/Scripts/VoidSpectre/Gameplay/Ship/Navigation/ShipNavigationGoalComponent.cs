using System;
using Sirenix.Serialization;
using VoidSpectre.Core.Component;
using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Ship.Navigation
{
    public enum ShipNavigationMode
    {
        Idle = 0,
        MoveToPoint = 1,
    }

    [Serializable]
    public sealed class ShipNavigationGoalComponent : TrackableComponentBase
    {
        [OdinSerialize] private ShipNavigationMode _mode = ShipNavigationMode.Idle;
        [OdinSerialize] private Float3 _targetPoint;
        [OdinSerialize] private float _arrivalRadius = 5f;
        [OdinSerialize] private float _arrivalSpeedEpsilon = 0.5f;
        [OdinSerialize] private float _maxApproachSpeed = 50f;
        [OdinSerialize] private float _planSampleInterval = 0.1f;
        [OdinSerialize] private float _planSimDeltaTime = 1f / 60f;
        [OdinSerialize] private float _replanPositionThreshold = 0.5f;
        [OdinSerialize] private float _replanIntervalSeconds;
        [OdinSerialize] private float _replanTrackingErrorThreshold;
        [OdinSerialize] private float _replanAttitudeErrorDegrees;
        [OdinSerialize] private bool _useLegacyPhasePlanner = true;

        public ShipNavigationMode Mode
        {
            get => _mode;
            set => SetField(ref _mode, value);
        }

        public Float3 TargetPoint
        {
            get => _targetPoint;
            set => SetField(ref _targetPoint, value);
        }

        public float ArrivalRadius
        {
            get => _arrivalRadius;
            set => SetField(ref _arrivalRadius, VsMath.Max(0.01f, value));
        }

        public float ArrivalSpeedEpsilon
        {
            get => _arrivalSpeedEpsilon;
            set => SetField(ref _arrivalSpeedEpsilon, VsMath.Max(0.001f, value));
        }

        public float MaxApproachSpeed
        {
            get => _maxApproachSpeed;
            set => SetField(ref _maxApproachSpeed, VsMath.Max(0f, value));
        }

        public float PlanSampleInterval
        {
            get => _planSampleInterval;
            set => SetField(ref _planSampleInterval, VsMath.Clamp(value, 0.02f, 1f));
        }

        public float PlanSimDeltaTime
        {
            get => _planSimDeltaTime;
            set => SetField(ref _planSimDeltaTime, VsMath.Clamp(value, 1f / 120f, 0.25f));
        }

        public float ReplanPositionThreshold
        {
            get => _replanPositionThreshold;
            set => SetField(ref _replanPositionThreshold, VsMath.Max(0.01f, value));
        }

        public float ReplanIntervalSeconds
        {
            get => _replanIntervalSeconds;
            set => SetField(ref _replanIntervalSeconds, VsMath.Max(0f, value));
        }

        public float ReplanTrackingErrorThreshold
        {
            get => _replanTrackingErrorThreshold;
            set => SetField(ref _replanTrackingErrorThreshold, VsMath.Max(0f, value));
        }

        public float ReplanAttitudeErrorDegrees
        {
            get => _replanAttitudeErrorDegrees;
            set => SetField(ref _replanAttitudeErrorDegrees, VsMath.Max(0f, value));
        }

        public bool UseLegacyPhasePlanner
        {
            get => _useLegacyPhasePlanner;
            set => SetField(ref _useLegacyPhasePlanner, value);
        }
    }
}
