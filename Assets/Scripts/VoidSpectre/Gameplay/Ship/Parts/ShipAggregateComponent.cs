using System;
using Sirenix.Serialization;
using VoidSpectre.Core.Component;
using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Ship.Parts
{
    [Serializable]
    public sealed class ShipAggregateComponent : TrackableComponentBase
    {
        [OdinSerialize] private float _totalMassKg;
        [OdinSerialize] private float _totalVolumeCubicMeters;
        [OdinSerialize] private Float3 _centerOfMassLocal;
        [OdinSerialize] private Float3 _compositeBoundsSize;
        [OdinSerialize] private float _approximateMomentOfInertia;

        public float TotalMassKg
        {
            get => _totalMassKg;
            set => SetField(ref _totalMassKg, VsMath.Max(0f, value));
        }

        public float TotalVolumeCubicMeters
        {
            get => _totalVolumeCubicMeters;
            set => SetField(ref _totalVolumeCubicMeters, VsMath.Max(0f, value));
        }

        public Float3 CenterOfMassLocal
        {
            get => _centerOfMassLocal;
            set => SetField(ref _centerOfMassLocal, value);
        }

        public Float3 CompositeBoundsSize
        {
            get => _compositeBoundsSize;
            set => SetField(ref _compositeBoundsSize, value);
        }

        public float ApproximateMomentOfInertia
        {
            get => _approximateMomentOfInertia;
            set => SetField(ref _approximateMomentOfInertia, VsMath.Max(0.01f, value));
        }
    }
}
