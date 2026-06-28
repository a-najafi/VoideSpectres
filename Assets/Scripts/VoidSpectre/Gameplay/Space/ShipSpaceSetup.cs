using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Math;

namespace VoidSpectre.Gameplay.Space
{
    public static class ShipSpaceSetup
    {
        public static void Configure(
            SimulationContext sector,
            ComponentStore.EntityId ship,
            Float3 initialPosition,
            Float3 initialDirection,
            float initialSpeed)
        {
            sector.Components.Set(ship, new SpacePositionComponent(initialPosition));
            sector.Components.Set(ship, new SpaceOrientationComponent(FloatQuaternion.LookRotation(
                initialDirection.SqrMagnitude > 0f ? initialDirection : Float3.Forward,
                Float3.Up)));

            var move = new SpaceMoveComponent
            {
                Direction = initialDirection.SqrMagnitude > 0f ? initialDirection : Float3.Forward,
                Speed = initialSpeed
            };
            sector.Components.Set(ship, move);
        }
    }
}
