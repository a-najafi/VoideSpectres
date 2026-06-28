using System;
using Sirenix.Serialization;
using VoidSpectre.Core.Component;
using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Space
{
    /// <summary>
    /// Kinematic velocity state. Motion is written by plan playback / spawn setup, not force integration.
    /// </summary>
    [Serializable]
    public sealed class SpaceMoveComponent : TrackableComponentBase
    {
        private const float DirectionEpsilon = 1e-6f;

        [OdinSerialize] private Float3 _direction = Float3.Forward;
        [OdinSerialize] private float _speed;

        public Float3 Direction
        {
            get => _direction;
            set => SetField(ref _direction, value);
        }

        public float Speed
        {
            get => _speed;
            set => SetField(ref _speed, VsMath.Max(0f, value));
        }

        public Float3 Velocity
        {
            get
            {
                if (_direction.SqrMagnitude < DirectionEpsilon)
                    return Float3.Zero;
                return _direction.Normalized * _speed;
            }
        }

        public void SetVelocity(Float3 velocity)
        {
            var magnitude = velocity.Magnitude;
            if (magnitude < DirectionEpsilon)
            {
                Speed = 0f;
                return;
            }

            Direction = velocity / magnitude;
            Speed = magnitude;
        }
    }
}
