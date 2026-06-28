using System;
using Sirenix.Serialization;
using VoidSpectre.Core.Component;
using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Ship.Thrusters
{
    [Serializable]
    public sealed class ThrusterComponent : TrackableComponentBase
    {
        public static readonly Float3 PartLocalThrustDirection = Float3.Forward;

        [OdinSerialize] private float _maxThrustNewtons = 10_000f;
        [OdinSerialize] private float _rampUpSeconds = 0.5f;
        [OdinSerialize] private float _fuelLitersPerSecondAtFullPower = 2f;
        [OdinSerialize] private float _targetPower;
        [OdinSerialize] private float _currentPower;

        public float MaxThrustNewtons
        {
            get => _maxThrustNewtons;
            set => SetField(ref _maxThrustNewtons, VsMath.Max(0f, value));
        }

        public float RampUpSeconds
        {
            get => _rampUpSeconds;
            set => SetField(ref _rampUpSeconds, VsMath.Max(0.01f, value));
        }

        public float FuelLitersPerSecondAtFullPower
        {
            get => _fuelLitersPerSecondAtFullPower;
            set => SetField(ref _fuelLitersPerSecondAtFullPower, VsMath.Max(0f, value));
        }

        public float TargetPower
        {
            get => _targetPower;
            set => SetField(ref _targetPower, VsMath.Clamp01(value));
        }

        public float CurrentPower
        {
            get => _currentPower;
            set => SetField(ref _currentPower, VsMath.Clamp01(value));
        }

        public float CurrentThrustNewtons => MaxThrustNewtons * CurrentPower;
    }
}
