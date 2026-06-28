using System;
using Sirenix.Serialization;
using VoidSpectre.Core.Component;
using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Space
{
    [Serializable]
    public sealed class SpaceOrientationComponent : BasicGenericTrackableComponent<FloatQuaternion>
    {
        public SpaceOrientationComponent() : base(FloatQuaternion.Identity) { }
        public SpaceOrientationComponent(FloatQuaternion rotation) : base(rotation) { }
    }

    [Serializable]
    public sealed class ShipAngularStateComponent : TrackableComponentBase
    {
        [OdinSerialize] private Float3 _angularVelocityLocal;
        [OdinSerialize] private Float3 _accumulatedTorqueLocal;

        public Float3 AngularVelocityLocal
        {
            get => _angularVelocityLocal;
            set => SetField(ref _angularVelocityLocal, value);
        }

        public Float3 AccumulatedTorqueLocal
        {
            get => _accumulatedTorqueLocal;
            set => SetField(ref _accumulatedTorqueLocal, value);
        }

        public void AddTorque(Float3 torqueLocal)
        {
            _accumulatedTorqueLocal += torqueLocal;
            BumpVersion();
        }

        public void ClearTorque() => AccumulatedTorqueLocal = Float3.Zero;
    }
}
