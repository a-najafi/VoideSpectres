using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Math;
using VoidSpectre.Core.Math.Geometry;
using VoidSpectre.Gameplay.Physics;
using VoidSpectre.Gameplay.Space;

namespace VoidSpectre.Gameplay.Demo
{
    public static class PlanetBlueprint
    {
        public static ComponentStore.EntityId Build(
            SimulationContext sector,
            Float3 position,
            float sphereRadius,
            float gravitationalParameter)
        {
            var entity = sector.CreateEntity();
            sector.Components.Set(entity, new SpacePositionComponent(position));

            var geometry = new GeometryVolumesComponent();
            geometry.AddVolume(GeometryVolume.Sphere(Float3.Zero, sphereRadius));
            sector.Components.Set(entity, geometry);

            sector.Components.Set(entity, new GravitySourceComponent
            {
                GravitationalParameter = gravitationalParameter,
                IgnoreDistanceMeters = 0f
            });

            return entity;
        }
    }
}
