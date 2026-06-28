using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Gameplay.Ship.Modules;

namespace VoidSpectre.Gameplay.Ship.Parts
{
    public static class ShipPartQueries
    {
        public static bool TryGetEngineFuel(
            SimulationContext context,
            ComponentStore.EntityId ship,
            out ComponentStore.EntityId engineEntity,
            out EngineFuelComponent fuel)
        {
            foreach (var (entity, part) in context.Components.GetAll<ShipPartComponent>())
            {
                if (part.ParentShip != ship) continue;
                if (!context.Components.TryGet<EngineFuelComponent>(entity, out fuel)) continue;
                engineEntity = entity;
                return true;
            }

            engineEntity = default;
            fuel = null;
            return false;
        }
    }
}
