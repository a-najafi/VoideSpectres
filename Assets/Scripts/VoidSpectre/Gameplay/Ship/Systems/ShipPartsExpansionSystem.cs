using System.Collections.Generic;
using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Interfaces;
using VoidSpectre.Gameplay.Ship.Config;
using VoidSpectre.Gameplay.Ship.Parts;

namespace VoidSpectre.Gameplay.Ship.Systems
{
    [RunsInContext(ContextKind.Volume)]
    public sealed class ShipPartsExpansionSystem : ICoreUpdateSystem
    {
        public string Name => "Ship Parts Expansion";
        public int Priority => -100;

        public void Update(SimulationContext context, float delta)
        {
            var pending = new List<(ComponentStore.EntityId ship, ShipPartsConfigComponent config)>();
            foreach (var pair in context.Components.GetAll<ShipPartsConfigComponent>())
                pending.Add(pair);

            foreach (var (ship, config) in pending)
            {
                ShipPartFactory.ExpandFromConfig(context, ship, config);
                context.Components.Remove<ShipPartsConfigComponent>(ship);
            }
        }
    }
}
