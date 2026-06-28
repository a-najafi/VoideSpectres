using System.Collections.Generic;
using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Math;
using VoidSpectre.Gameplay.Physics;
using VoidSpectre.Gameplay.Space;

namespace VoidSpectre.Gameplay.Ship.Navigation
{
    /// <summary>
    /// Lightweight gravity snapshot for forward simulation during maneuver planning.
    /// </summary>
    public sealed class ShipGravityModel
    {
        private struct GravitySourceSnapshot
        {
            public Float3 Position;
            public float GravitationalParameter;
            public float IgnoreDistanceMeters;
        }

        private readonly float _massKg;
        private readonly List<GravitySourceSnapshot> _sources = new();

        public bool HasGravity => _sources.Count > 0 && _massKg > 0f;

        private ShipGravityModel(float massKg)
        {
            _massKg = massKg;
        }

        public static ShipGravityModel TryBuild(
            SimulationContext context,
            ComponentStore.EntityId ship,
            float massKg)
        {
            if (massKg <= 0f ||
                !context.Components.TryGet(ship, out GravityAffectedComponent _))
            {
                return null;
            }

            var model = new ShipGravityModel(massKg);

            foreach (var (sourceEntity, gravity) in context.Components.GetAll<GravitySourceComponent>())
            {
                if (sourceEntity == ship) continue;
                if (!context.Components.TryGet(sourceEntity, out SpacePositionComponent sourcePosition)) continue;

                model._sources.Add(new GravitySourceSnapshot
                {
                    Position = sourcePosition.Value,
                    GravitationalParameter = gravity.GravitationalParameter,
                    IgnoreDistanceMeters = gravity.IgnoreDistanceMeters,
                });
            }

            return model.HasGravity ? model : null;
        }

        public Float3 ComputeForce(Float3 shipPosition)
        {
            if (!HasGravity)
                return Float3.Zero;

            var force = Float3.Zero;
            for (int i = 0; i < _sources.Count; i++)
            {
                var source = _sources[i];
                var delta = source.Position - shipPosition;
                var distance = delta.Magnitude;
                if (distance <= source.IgnoreDistanceMeters)
                    continue;

                if (delta.SqrMagnitude < 1e-8f)
                    continue;

                var effectiveDistance = VsMath.Max(distance, 1f);
                var magnitude = source.GravitationalParameter * _massKg /
                                (effectiveDistance * effectiveDistance);
                force += delta.Normalized * magnitude;
            }

            return force;
        }
    }
}
