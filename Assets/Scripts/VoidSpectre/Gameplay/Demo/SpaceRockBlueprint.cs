using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Math;
using VoidSpectre.Core.Math.Geometry;
using VoidSpectre.Gameplay.Physics;
using VoidSpectre.Gameplay.Space;

namespace VoidSpectre.Gameplay.Demo
{
    public static class SpaceRockBlueprint
    {
        public static ComponentStore.EntityId Build(
            SimulationContext sector,
            Float3 position,
            float massKg,
            float sphereRadius)
        {
            var entity = sector.CreateEntity();
            sector.Components.Set(entity, new SpacePositionComponent(position));
            sector.Components.Set(entity, new SpaceOrientationComponent(FloatQuaternion.Identity));
            sector.Components.Set(entity, new SpaceMoveComponent());
            sector.Components.Set(entity, new MassComponent(massKg));

            var geometry = new GeometryVolumesComponent();
            geometry.AddVolume(GeometryVolume.Sphere(Float3.Zero, sphereRadius));
            sector.Components.Set(entity, geometry);

            return entity;
        }
    }
}
