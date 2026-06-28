using VoidSpectre.Core.Context;
using VoidSpectre.Core.Interfaces;
using VoidSpectre.Gameplay.Ship.Parts;
using VoidSpectre.Gameplay.Ship.Thrusters;

namespace VoidSpectre.Gameplay.Ship.Systems
{
    [RunsInContext(ContextKind.Volume)]
    public sealed class FuelConsumptionSystem : ICoreUpdateSystem
    {
        public string Name => "Fuel Consumption";
        public int Priority => 18;

        public void Update(SimulationContext context, float delta)
        {
            foreach (var (entity, part) in context.Components.GetAll<ShipPartComponent>())
            {
                if (ShipPlanPlaybackQueries.IsStepSimPlayback(context, part.ParentShip)) continue;
                if (!context.Components.TryGet(entity, out ThrusterComponent thruster)) continue;
                if (thruster.CurrentPower <= 0f) continue;

                if (!ShipPartQueries.TryGetEngineFuel(context, part.ParentShip, out _, out var fuel))
                {
                    thruster.TargetPower = 0f;
                    thruster.CurrentPower = 0f;
                    continue;
                }

                var requested = thruster.FuelLitersPerSecondAtFullPower * thruster.CurrentPower * delta;
                var consumed = fuel.Consume(requested);
                if (consumed < requested - 1e-4f)
                {
                    thruster.TargetPower = 0f;
                    thruster.CurrentPower = 0f;
                }
            }
        }
    }
}
