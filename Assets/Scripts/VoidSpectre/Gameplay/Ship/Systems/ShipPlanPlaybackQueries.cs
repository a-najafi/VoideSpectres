using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Gameplay.Ship.Navigation;
using VoidSpectre.Gameplay.Ship.Parts;

namespace VoidSpectre.Gameplay.Ship.Systems
{
    public static class ShipPlanPlaybackQueries
    {
        public static bool IsStepSimPlayback(SimulationContext context, ComponentStore.EntityId ship)
        {
            if (!context.Components.TryGet(ship, out ShipPlanExecutionComponent execution) ||
                !context.Components.TryGet(ship, out ShipManeuverPlanComponent plan) ||
                !context.Components.TryGet(ship, out ShipNavigationGoalComponent goal))
            {
                return false;
            }

            return goal.Mode == ShipNavigationMode.MoveToPoint &&
                   plan.IsValid &&
                   execution.ActivePlanId == plan.PlanId &&
                   (execution.Status == ShipPlanExecutionStatus.Executing ||
                    execution.Status == ShipPlanExecutionStatus.Completed);
        }

        public static bool IsThrusterOnPlaybackShip(SimulationContext context, ComponentStore.EntityId thrusterEntity)
        {
            if (!context.Components.TryGet(thrusterEntity, out ShipPartComponent part))
                return false;

            return IsStepSimPlayback(context, part.ParentShip);
        }
    }
}
