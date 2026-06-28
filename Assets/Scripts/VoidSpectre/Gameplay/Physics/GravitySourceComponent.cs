using System;
using Sirenix.Serialization;
using VoidSpectre.Core.Component;
using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Physics
{
    [Serializable]
    public sealed class GravitySourceComponent : TrackableComponentBase
    {
        [OdinSerialize] private float _gravitationalParameter = 1_000_000f;
        [OdinSerialize] private float _ignoreDistanceMeters;

        public float GravitationalParameter
        {
            get => _gravitationalParameter;
            set => SetField(ref _gravitationalParameter, VsMath.Max(0f, value));
        }

        public float IgnoreDistanceMeters
        {
            get => _ignoreDistanceMeters;
            set => SetField(ref _ignoreDistanceMeters, VsMath.Max(0f, value));
        }
    }
}
