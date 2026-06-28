using System;
using Sirenix.Serialization;
using VoidSpectre.Core.Component;
using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Physics
{
    [Serializable]
    public sealed class LocalTransformComponent : TrackableComponentBase
    {
        [OdinSerialize] private Float3 _localPosition;
        [OdinSerialize] private FloatQuaternion _localRotation = FloatQuaternion.Identity;

        public Float3 LocalPosition
        {
            get => _localPosition;
            set => SetField(ref _localPosition, value);
        }

        public FloatQuaternion LocalRotation
        {
            get => _localRotation;
            set => SetField(ref _localRotation, value);
        }

        public LocalTransformComponent() { }

        public LocalTransformComponent(Float3 localPosition, FloatQuaternion localRotation)
        {
            _localPosition = localPosition;
            _localRotation = localRotation;
        }
    }
}
