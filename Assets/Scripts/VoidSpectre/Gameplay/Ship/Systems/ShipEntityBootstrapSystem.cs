using System.Collections.Generic;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Interfaces;
using VoidSpectre.Gameplay.Ship.Bootstrap;
using VoidSpectre.Gameplay.Ship.Parts;

namespace VoidSpectre.Gameplay.Ship.Systems
{
    /// <summary>
    /// Ensures ship roots and parts have the components required for pilot input to produce movement.
    /// Runs after part expansion (-100) and before mass aggregation (0).
    /// </summary>
    [RunsInContext(ContextKind.Volume)]
    public sealed class ShipEntityBootstrapSystem : ICoreUpdateSystem
    {
        private readonly HashSet<int> _readinessLogged = new();

        public string Name => "Ship Entity Bootstrap";
        public int Priority => -90;

        public void Update(SimulationContext context, float delta)
        {
            foreach (var (ship, _) in ShipEntityBootstrapUtility.EnumerateShipRoots(context))
                ShipEntityBootstrapUtility.EnsureRootComponents(context, ship);

            foreach (var (partEntity, _) in context.Components.GetAll<ShipPartComponent>())
                ShipEntityBootstrapUtility.EnsurePartComponents(context, partEntity);

            foreach (var (ship, _) in ShipEntityBootstrapUtility.EnumerateShipRoots(context))
                ShipEntityBootstrapUtility.LogMovementReadiness(context, ship, _readinessLogged);
        }
    }
}
