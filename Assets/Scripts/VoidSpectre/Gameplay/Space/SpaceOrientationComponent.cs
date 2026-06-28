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

        public Float3 AngularVelocityLocal
        {
            get => _angularVelocityLocal;
            set => SetField(ref _angularVelocityLocal, value);
        }
    }
}
