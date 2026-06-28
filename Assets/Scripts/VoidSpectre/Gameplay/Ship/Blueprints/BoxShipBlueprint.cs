using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Gameplay.Ship.Config;

namespace VoidSpectre.Gameplay.Ship.Blueprints
{
    public static class BoxShipBlueprint
    {
        public static void Build(SimulationContext sector, ComponentStore.EntityId ship) =>
            sector.Components.Set(ship, BoxShipPartsConfigPreset.Create());
    }
}
