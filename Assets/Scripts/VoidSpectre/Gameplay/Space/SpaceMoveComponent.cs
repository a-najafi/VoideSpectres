using System;
using System.Collections.Generic;
using Sirenix.Serialization;
using VoidSpectre.Core.Component;
using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Space
{
    [Serializable]
    public sealed class SpaceMoveComponent : TrackableComponentBase
    {
        private const float DirectionEpsilon = 1e-6f;

        [OdinSerialize] private Float3 _direction = Float3.Forward;
        [OdinSerialize] private float _speed;
        [OdinSerialize] private List<SpaceForce> _activeForces = new();

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

        public IReadOnlyList<SpaceForce> ActiveForces => _activeForces;

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

        public void AddForce(SpaceForce force)
        {
            _activeForces.Add(force);
            BumpVersion();
        }

        public void AddForce(Float3 force, int sourceId = 0) =>
            AddForce(new SpaceForce(force, sourceId));

        public bool RemoveForceAt(int index)
        {
            if (index < 0 || index >= _activeForces.Count) return false;
            _activeForces.RemoveAt(index);
            BumpVersion();
            return true;
        }

        public int RemoveForcesFromSource(int sourceId)
        {
            int removed = _activeForces.RemoveAll(f => f.SourceId == sourceId);
            if (removed > 0) BumpVersion();
            return removed;
        }

        public void ClearForces()
        {
            if (_activeForces.Count == 0) return;
            _activeForces.Clear();
            BumpVersion();
        }
    }
}
