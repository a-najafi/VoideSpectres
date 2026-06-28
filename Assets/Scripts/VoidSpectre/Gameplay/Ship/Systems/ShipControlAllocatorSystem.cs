using VoidSpectre.Core.Context;
using VoidSpectre.Core.Interfaces;
using VoidSpectre.Gameplay.Ship.Navigation;
using VoidSpectre.Gameplay.Ship.Thrusters;

namespace VoidSpectre.Gameplay.Ship.Systems
{
    /// <summary>
    /// Solves for per-thruster throttles from the desired body-frame wrench using the ship plant model.
    /// Skips ships whose active plan is replaying throttles directly.
    /// </summary>
    [RunsInContext(ContextKind.Volume)]
    public sealed class ShipControlAllocatorSystem : ICoreUpdateSystem
    {
        public string Name => "Ship Control Allocator";
        public int Priority => 3;

        public void Update(SimulationContext context, float delta)
        {
            foreach (var (ship, wrench) in context.Components.GetAll<ShipWrenchCommandComponent>())
            {
                if (IsExecutingPlanPlayback(context, ship))
                    continue;

                var plant = ShipPlantModel.Build(context, ship);
                if (plant.ThrusterCount == 0)
                    continue;

                var throttle = ShipControlAllocator.Solve(
                    plant,
                    wrench.DesiredForceBody,
                    wrench.DesiredTorqueBody);

                for (int i = 0; i < plant.ThrusterCount; i++)
                {
                    if (context.Components.TryGet(plant.ThrusterEntities[i], out ThrusterComponent thruster))
                        thruster.TargetPower = throttle[i];
                }
            }
        }

        private static bool IsExecutingPlanPlayback(
            SimulationContext context,
            VoidSpectre.Core.ComponentStore.EntityId ship)
        {
            if (!context.Components.TryGet(ship, out ShipPlanExecutionComponent execution) ||
                !context.Components.TryGet(ship, out ShipManeuverPlanComponent plan))
            {
                return false;
            }

            return plan.IsValid &&
                   execution.ActivePlanId == plan.PlanId &&
                   (execution.Status == ShipPlanExecutionStatus.Executing ||
                    execution.Status == ShipPlanExecutionStatus.Completed);
        }
    }
}
