using VoidSpectre.Core.Context;
using VoidSpectre.Core.Diagnostics;
using VoidSpectre.Core.Interfaces;
using VoidSpectre.Gameplay.Physics;
using VoidSpectre.Gameplay.Ship.Parts;

namespace VoidSpectre.Gameplay.Space
{
    [RunsInContext(ContextKind.Volume)]
    public sealed class SpaceMovementDebugSystem : ICoreUpdateSystem
    {
        public string Name => "Space Movement Debug";
        public int Priority => 200;

        private float _timer;

        public void Update(SimulationContext context, float delta)
        {
            _timer += delta;
            if (_timer < 3f) return;
            _timer = 0f;

            foreach (var (entity, move) in context.Components.GetAll<SpaceMoveComponent>())
            {
                if (!context.Components.TryGet(entity, out SpacePositionComponent position)) continue;
                if (!context.Components.TryGet(entity, out MassComponent mass)) continue;

                var velocity = move.Velocity;
                var fuelText = "n/a";
                if (ShipPartQueries.TryGetEngineFuel(context, entity, out _, out var fuel))
                    fuelText = $"{fuel.CurrentFuelLiters:F0}/{fuel.MaxFuelLiters:F0} L";

                var volumeText = "n/a";
                if (context.Components.TryGet(entity, out ShipAggregateComponent aggregate))
                    volumeText = $"{aggregate.TotalVolumeCubicMeters:F0} m³";

                VsLog.Info(
                    $"[Space] {entity} pos={position.Value} vel={velocity.Magnitude:F1} m/s " +
                    $"mass={mass.Value:F0} kg vol={volumeText} fuel={fuelText}");
            }
        }
    }
}
