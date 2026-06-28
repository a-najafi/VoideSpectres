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
    /// Replays tick-indexed plans by applying each tick's recorded expected state — the green path contract.
    /// Thruster controls are synced for visuals; pose comes from the plan, not a second simulation pass.
    /// </summary>
    [RunsInContext(ContextKind.Volume)]
    public sealed class ShipPlanExecutorSystem : ICoreUpdateSystem
    {
        public string Name => "Ship Plan Executor";
        public int Priority => 2;

        public void Update(SimulationContext context, float delta)
        {
            foreach (var (ship, execution) in context.Components.GetAll<ShipPlanExecutionComponent>())
            {
                if (!context.Components.TryGet(ship, out ShipWrenchCommandComponent wrench))
                {
                    wrench = new ShipWrenchCommandComponent();
                    context.Components.Set(ship, wrench);
                }

                if (!context.Components.TryGet(ship, out ShipManeuverPlanComponent plan) ||
                    !context.Components.TryGet(ship, out ShipNavigationGoalComponent goal))
                {
                    wrench.Clear();
                    continue;
                }

                if (goal.Mode != ShipNavigationMode.MoveToPoint ||
                    !plan.IsValid ||
                    execution.ActivePlanId != plan.PlanId)
                {
                    wrench.Clear();
                    continue;
                }

                var snapshot = plan.PlanContextSnapshot;
                if (snapshot?.Plant == null)
                {
                    wrench.Clear();
                    continue;
                }

                if (execution.Status == ShipPlanExecutionStatus.Completed)
                {
                    HoldLastTick(context, ship, plan, snapshot.Plant, wrench);
                    continue;
                }

                if (execution.Status != ShipPlanExecutionStatus.Executing)
                {
                    wrench.Clear();
                    continue;
                }

                if (execution.TickIndex >= plan.TickCount)
                {
                    CompletePlan(context, ship, goal, execution, wrench, plan, snapshot.Plant);
                    continue;
                }

                var fixedDt = plan.FixedDt > 0f ? plan.FixedDt : goal.PlanSimDeltaTime;

                execution.FixedStepAccumulator += delta;

                while (execution.FixedStepAccumulator >= fixedDt && execution.TickIndex < plan.TickCount)
                {
                    execution.FixedStepAccumulator -= fixedDt;

                    if (!plan.TryGetTick(execution.TickIndex, out var tick))
                        break;

                    tick.ExpectedState.ApplyToLive(context, ship, snapshot.Plant);
                    ApplyControls(snapshot.Plant, tick.Controls, wrench, tick);

                    execution.TickIndex++;
                    execution.ElapsedTime = execution.TickIndex * fixedDt;
                }
            }
        }

        private static void ApplyControls(
            ShipPlantModel plant,
            ShipControlInput controls,
            ShipWrenchCommandComponent wrench,
            ShipPlanTick tick)
        {
            if (controls?.TargetThrusterPower == null)
                return;

            plant.ApplyGimbalAngles(tick.ExpectedState.GimbalDegrees);
            plant.GetWrenchAtThrottle(controls.TargetThrusterPower, out var forceBody, out var torqueBody);
            wrench.Set(forceBody, torqueBody);
        }

        private static void HoldLastTick(
            SimulationContext context,
            ComponentStore.EntityId ship,
            ShipManeuverPlanComponent plan,
            ShipPlantModel plant,
            ShipWrenchCommandComponent wrench)
        {
            if (plan.TickCount <= 0 || !plan.TryGetTick(plan.TickCount - 1, out var lastTick))
            {
                wrench.Clear();
                return;
            }

            lastTick.ExpectedState.ApplyToLive(context, ship, plant);
            ApplyControls(plant, lastTick.Controls, wrench, lastTick);
        }

        private static void CompletePlan(
            SimulationContext context,
            ComponentStore.EntityId ship,
            ShipNavigationGoalComponent goal,
            ShipPlanExecutionComponent execution,
            ShipWrenchCommandComponent wrench,
            ShipManeuverPlanComponent plan,
            ShipPlantModel plant)
        {
            HoldLastTick(context, ship, plan, plant, wrench);

            if (IsAtTarget(context, ship, goal))
                goal.Mode = ShipNavigationMode.Idle;

            execution.Status = ShipPlanExecutionStatus.Completed;
            execution.LastInvalidationReason = ShipPlanInvalidationReason.PlanCompleted;
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
