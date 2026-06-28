using System;
using Sirenix.Serialization;
using VoidSpectre.Core.Component;
using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Ship.Navigation
{
    [Serializable]
    public sealed class ShipPilotCommandComponent : TrackableComponentBase
    {
        [OdinSerialize] private float _mainThrust;
        [OdinSerialize] private float _pitch;
        [OdinSerialize] private float _yaw;
        [OdinSerialize] private float _roll;

        public float MainThrust
        {
            get => _mainThrust;
            set => SetField(ref _mainThrust, VsMath.Clamp01(value));
        }

        public float Pitch
        {
            get => _pitch;
            set => SetField(ref _pitch, VsMath.Clamp(value, -1f, 1f));
        }

        public float Yaw
        {
            get => _yaw;
            set => SetField(ref _yaw, VsMath.Clamp(value, -1f, 1f));
        }

        public float Roll
        {
            get => _roll;
            set => SetField(ref _roll, VsMath.Clamp(value, -1f, 1f));
        }
    }
}
