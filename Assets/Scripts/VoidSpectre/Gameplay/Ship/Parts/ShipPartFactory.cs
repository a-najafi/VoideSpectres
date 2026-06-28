using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Diagnostics;
using VoidSpectre.Gameplay.Physics;
using VoidSpectre.Gameplay.Ship.Bootstrap;
using VoidSpectre.Gameplay.Ship.Config;

namespace VoidSpectre.Gameplay.Ship.Parts
{
    public static class ShipPartFactory
    {
        public static void ExpandFromConfig(
            SimulationContext context,
            ComponentStore.EntityId ship,
            ShipPartsConfigComponent config)
        {
            EnsureShipRootComponents(context, ship);

            foreach (var placement in config.Parts)
                SpawnPart(context, ship, placement);
        }

        private static void EnsureShipRootComponents(SimulationContext context, ComponentStore.EntityId ship) =>
            ShipEntityBootstrapUtility.EnsureRootComponents(context, ship);

        private static void SpawnPart(
            SimulationContext context,
            ComponentStore.EntityId ship,
            ShipPartPlacement placement)
        {
            if (placement.Archetype == null)
            {
                VsLog.Warning($"[ShipPartFactory] Skipping part with null archetype on ship {ship}.");
                return;
            }

            var entity = context.CreateEntity();
            context.Components.Set(entity, new ShipPartComponent { ParentShip = ship });
            context.Components.Set(entity, new LocalTransformComponent(
                placement.LocalPosition,
                placement.LocalOrientation));
            placement.Archetype.ApplyTo(context, entity);
        }
    }
}
