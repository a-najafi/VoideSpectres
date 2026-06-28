using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Interfaces;
using VoidSpectre.Core.Math;
using VoidSpectre.Gameplay.Ship.Navigation;
using VoidSpectre.Gameplay.Ship.Parts;
using VoidSpectre.Gameplay.Space;

namespace VoidSpectre.Gameplay.Ship.Systems
{
    /// <summary>
    /// Submits replan requests. While a plan is executing, the path is locked until the goal moves
    /// or the plan finishes and still has not reached the target.
    /// </summary>
    [RunsInContext(ContextKind.Volume)]
    public sealed class ShipPlanningSystem : ICoreUpdateSystem
    {
        public string Name => "Ship Planning";
        public int Priority => 0;

        public void Update(SimulationContext context, float delta)
        {
            foreach (var (ship, goal) in context.Components.GetAll<ShipNavigationGoalComponent>())
            {
                if (!context.Components.TryGet(ship, out ShipManeuverPlanComponent plan))
                {
                    plan = new ShipManeuverPlanComponent();
                    context.Components.Set(ship, plan);
                }

                if (!context.Components.TryGet(ship, out ShipPlanExecutionComponent execution))
                {
                    execution = new ShipPlanExecutionComponent();
                    context.Components.Set(ship, execution);
                }

                EnsurePlanningComponents(context, ship);

                if (goal.Mode != ShipNavigationMode.MoveToPoint)
                {
                    plan.Clear();
                    execution.Reset();
                    continue;
                }

                if (!IsShipReadyToPlan(context, ship))
                    continue;

                if (IsAtTarget(context, ship, goal))
                {
                    plan.Clear();
                    execution.Reset();
                    continue;
                }

                var goalMatchesPlan = plan.IsValid && plan.MatchesGoal(goal, goal.ReplanPositionThreshold);
                var executionMatchesPlan = goalMatchesPlan && execution.ActivePlanId == plan.PlanId;

                if (execution.Status == ShipPlanExecutionStatus.Executing && executionMatchesPlan)
                    continue;

                if (execution.Status == ShipPlanExecutionStatus.Executing && !goalMatchesPlan)
                {
                    plan.Clear();
                    execution.Reset();
                    SubmitReplan(context, ship, ShipPlanInvalidationReason.GoalChanged);
                    continue;
                }

                if (goalMatchesPlan && executionMatchesPlan)
                {
                    if (execution.Status == ShipPlanExecutionStatus.Idle)
                    {
                        execution.BeginPlan(plan.PlanId, plan.FixedDt);
                        continue;
                    }

                    if (execution.Status == ShipPlanExecutionStatus.Completed)
                    {
                        SubmitReplan(context, ship, ShipPlanInvalidationReason.PlanCompleted);
                        continue;
                    }

                    if (execution.Status == ShipPlanExecutionStatus.Failed)
                    {
                        execution.Reset();
                        SubmitReplan(context, ship, ShipPlanInvalidationReason.PlanningFailed);
                        continue;
                    }

                    continue;
                }

                SubmitReplan(context, ship, ShipPlanInvalidationReason.GoalChanged);
            }
        }

        private static bool IsShipReadyToPlan(SimulationContext context, ComponentStore.EntityId ship)
        {
            if (!context.Components.TryGet(ship, out ShipAggregateComponent aggregate) ||
                aggregate.TotalMassKg <= 0f)
            {
                return false;
            }

            var plant = ShipPlantModel.Build(context, ship);
            return plant.ThrusterCount > 0 && plant.MaxForwardAccel > 0.01f;
        }

        private static void EnsurePlanningComponents(SimulationContext context, ComponentStore.EntityId ship)
        {
            if (!context.Components.Has<ShipPlanningLodComponent>(ship))
                context.Components.Set(ship, new ShipPlanningLodComponent());

            if (!context.Components.Has<ShipPlanningRequestComponent>(ship))
                context.Components.Set(ship, new ShipPlanningRequestComponent());
        }

        private static void SubmitReplan(
            SimulationContext context,
            ComponentStore.EntityId ship,
            ShipPlanInvalidationReason reason)
        {
            if (!context.Components.TryGet(ship, out ShipPlanningRequestComponent request))
            {
                request = new ShipPlanningRequestComponent();
                context.Components.Set(ship, request);
            }

            if (request.HasPendingRequest)
                return;

            var priority = reason switch
            {
                ShipPlanInvalidationReason.FuelExhausted => ShipPlanningRequestPriority.Critical,
                ShipPlanInvalidationReason.GoalChanged => ShipPlanningRequestPriority.Critical,
                ShipPlanInvalidationReason.PlanCompleted => ShipPlanningRequestPriority.High,
                _ => ShipPlanningRequestPriority.Normal,
            };

            ShipPlanningBudgetSystem.SubmitRequest(context, ship, priority, reason);
        }

        private static bool IsAtTarget(
            SimulationContext context,
            ComponentStore.EntityId ship,
            ShipNavigationGoalComponent goal)
        {
            if (!context.Components.TryGet(ship, out SpacePositionComponent position) ||
                !context.Components.TryGet(ship, out SpaceMoveComponent move))
            {
                return false;
            }

            var dist = (goal.TargetPoint - position.Value).Magnitude;
            return dist <= goal.ArrivalRadius &&
                   move.Velocity.Magnitude <= goal.ArrivalSpeedEpsilon;
        }
    }
}
