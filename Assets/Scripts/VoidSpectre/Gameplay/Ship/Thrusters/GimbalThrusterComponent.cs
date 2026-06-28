using System;
using Sirenix.Serialization;
using VoidSpectre.Core.Component;
using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Ship.Thrusters
{
    [Serializable]
    public sealed class GimbalThrusterComponent : TrackableComponentBase
    {
        [OdinSerialize] private Float3 _gimbalAxisLocal = Float3.Forward;
        [OdinSerialize] private float _arcHalfDegrees = 45f;
        [OdinSerialize] private float _currentGimbalDegrees;
        [OdinSerialize] private float _targetGimbalDegrees;
        [OdinSerialize] private float _maxGimbalSpeedDegreesPerSecond = 30f;

        public Float3 GimbalAxisLocal
        {
            get => _gimbalAxisLocal;
            set => SetField(ref _gimbalAxisLocal, value.SqrMagnitude > 0f ? value.Normalized : Float3.Forward);
        }

        public float ArcHalfDegrees
        {
            get => _arcHalfDegrees;
            set => SetField(ref _arcHalfDegrees, VsMath.Clamp(value, 1f, 180f));
        }

        public float CurrentGimbalDegrees
        {
            get => _currentGimbalDegrees;
            set => SetField(ref _currentGimbalDegrees, value);
        }

        public float TargetGimbalDegrees
        {
            get => _targetGimbalDegrees;
            set => SetField(ref _targetGimbalDegrees, VsMath.Clamp(value, -ArcHalfDegrees, ArcHalfDegrees));
        }

        public float MaxGimbalSpeedDegreesPerSecond
        {
            get => _maxGimbalSpeedDegreesPerSecond;
            set => SetField(ref _maxGimbalSpeedDegreesPerSecond, VsMath.Max(1f, value));
        }

        public Float3 ApplyGimbal(Float3 baseLocalDirection)
        {
            if (baseLocalDirection.SqrMagnitude < 1e-6f) return Float3.Forward;
            return FloatQuaternion.AngleAxis(_currentGimbalDegrees, _gimbalAxisLocal) * baseLocalDirection.Normalized;
        }
    }
}
