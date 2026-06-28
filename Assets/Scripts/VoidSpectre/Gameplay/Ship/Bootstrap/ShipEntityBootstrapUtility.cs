using System.Collections.Generic;
using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Diagnostics;
using VoidSpectre.Core.Math;
using VoidSpectre.Gameplay.Physics;
using VoidSpectre.Gameplay.Ship.Config;
using VoidSpectre.Gameplay.Ship.Navigation;
using VoidSpectre.Gameplay.Ship.Parts;
using VoidSpectre.Gameplay.Ship.Thrusters;
using VoidSpectre.Gameplay.Space;

namespace VoidSpectre.Gameplay.Ship.Bootstrap
{
    public static class ShipEntityBootstrapUtility
    {
        public static IEnumerable<(ComponentStore.EntityId ship, string label)> EnumerateShipRoots(
            SimulationContext context)
        {
            var seen = new HashSet<int>();

            foreach (var (ship, _) in context.Components.GetAll<ShipAggregateComponent>())
            {
                if (seen.Add(ship.Id))
                    yield return (ship, "ShipAggregateComponent");
            }

            foreach (var (ship, _) in context.Components.GetAll<ShipPartsConfigComponent>())
            {
                if (seen.Add(ship.Id))
                    yield return (ship, "ShipPartsConfigComponent");
            }
        }

        public static void EnsureRootComponents(SimulationContext context, ComponentStore.EntityId ship)
        {
            if (!context.Components.Has<ShipAggregateComponent>(ship))
            {
                context.Components.Set(ship, new ShipAggregateComponent());
                VsLog.Info($"[ShipBootstrap] Added ShipAggregateComponent to ship {ship}.");
            }

            if (!context.Components.Has<ShipNavigationGoalComponent>(ship))
            {
                context.Components.Set(ship, new ShipNavigationGoalComponent());
                VsLog.Info($"[ShipBootstrap] Added ShipNavigationGoalComponent (Idle) to ship {ship}.");
            }

            if (!context.Components.Has<ShipWrenchCommandComponent>(ship))
            {
                context.Components.Set(ship, new ShipWrenchCommandComponent());
                VsLog.Info($"[ShipBootstrap] Added ShipWrenchCommandComponent to ship {ship}.");
            }

            if (!context.Components.Has<ShipManeuverPlanComponent>(ship))
            {
                context.Components.Set(ship, new ShipManeuverPlanComponent());
                VsLog.Info($"[ShipBootstrap] Added ShipManeuverPlanComponent to ship {ship}.");
            }

            if (!context.Components.Has<ShipPlanExecutionComponent>(ship))
            {
                context.Components.Set(ship, new ShipPlanExecutionComponent());
                VsLog.Info($"[ShipBootstrap] Added ShipPlanExecutionComponent to ship {ship}.");
            }

            if (!context.Components.Has<ShipPlanningLodComponent>(ship))
            {
                context.Components.Set(ship, new ShipPlanningLodComponent());
                VsLog.Info($"[ShipBootstrap] Added ShipPlanningLodComponent to ship {ship}.");
            }

            if (!context.Components.Has<ShipPlanningRequestComponent>(ship))
                context.Components.Set(ship, new ShipPlanningRequestComponent());

            if (!context.Components.Has<ShipAngularStateComponent>(ship))
            {
                context.Components.Set(ship, new ShipAngularStateComponent());
                VsLog.Info($"[ShipBootstrap] Added ShipAngularStateComponent to ship {ship}.");
            }

            if (!context.Components.TryGet(ship, out SpacePositionComponent _))
            {
                context.Components.Set(ship, new SpacePositionComponent(Float3.Zero));
                VsLog.Warning(
                    $"[ShipBootstrap] Ship {ship} had no SpacePositionComponent; added origin. " +
                    "Configure spawn position via ShipSpaceSetup or archetype.");
            }

            if (!context.Components.TryGet(ship, out SpaceOrientationComponent _))
            {
                context.Components.Set(ship, new SpaceOrientationComponent(FloatQuaternion.Identity));
                VsLog.Warning(
                    $"[ShipBootstrap] Ship {ship} had no SpaceOrientationComponent; added identity rotation.");
            }

            if (!context.Components.TryGet(ship, out SpaceMoveComponent _))
            {
                context.Components.Set(ship, new SpaceMoveComponent
                {
                    Direction = Float3.Forward,
                    Speed = 0f
                });
                VsLog.Warning(
                    $"[ShipBootstrap] Ship {ship} had no SpaceMoveComponent; added default. " +
                    "Movement forces will apply once mass is non-zero.");
            }
        }

        public static void EnsurePartComponents(SimulationContext context, ComponentStore.EntityId partEntity)
        {
            if (!context.Components.TryGet(partEntity, out ShipPartComponent part))
                return;

            if (!context.Components.TryGet(partEntity, out LocalTransformComponent _))
            {
                VsLog.Warning(
                    $"[ShipBootstrap] Ship part {partEntity} on ship {part.ParentShip} has no LocalTransformComponent.");
            }

            EnsurePartMassSource(context, partEntity, part.ParentShip);
        }

        private static void EnsurePartMassSource(
            SimulationContext context,
            ComponentStore.EntityId partEntity,
            ComponentStore.EntityId ship)
        {
            if (context.Components.Has<MassSourceComponent>(partEntity))
                return;

            if (context.Components.TryGet(partEntity, out MassComponent mass) && mass.Value > 0f)
            {
                context.Components.Set(partEntity, new MassSourceComponent(mass.Value));
                VsLog.Info(
                    $"[ShipBootstrap] Copied MassComponent ({mass.Value} kg) to MassSourceComponent " +
                    $"on part {partEntity} (ship {ship}). Prefer MassSourceComponent on part archetypes.");
                return;
            }

            if (context.Components.Has<GeometryVolumesComponent>(partEntity))
            {
                VsLog.Warning(
                    $"[ShipBootstrap] Part {partEntity} on ship {ship} has geometry but no MassSourceComponent. " +
                    "Ship mass stays zero until each part archetype includes MassSourceComponent.");
            }
        }

        public static bool TryGetPartMassKg(
            SimulationContext context,
            ComponentStore.EntityId partEntity,
            out float massKg)
        {
            if (context.Components.TryGet(partEntity, out MassSourceComponent massSource) &&
                massSource.Value > 0f)
            {
                massKg = massSource.Value;
                return true;
            }

            if (context.Components.TryGet(partEntity, out MassComponent mass) && mass.Value > 0f)
            {
                massKg = mass.Value;
                return true;
            }

            massKg = 0f;
            return false;
        }

        public static void LogMovementReadiness(
            SimulationContext context,
            ComponentStore.EntityId ship,
            HashSet<int> loggedShips)
        {
            if (loggedShips.Contains(ship.Id))
                return;

            // ShipAggregateSystem (priority 0) runs after this bootstrap pass (-90).
            // Defer readiness until aggregated mass is available so validation is not falsely Grounded.
            if (!context.Components.TryGet(ship, out ShipAggregateComponent aggregate) ||
                aggregate.TotalMassKg <= 0f)
            {
                return;
            }

            if (!loggedShips.Add(ship.Id))
                return;

            if (!context.Components.Has<ShipNavigationGoalComponent>(ship))
            {
                VsLog.Warning(
                    $"[ShipMovement] Ship {ship}: missing ShipNavigationGoalComponent — autopilot cannot run.");
            }

            if (!context.Components.Has<ShipWrenchCommandComponent>(ship))
            {
                VsLog.Warning(
                    $"[ShipMovement] Ship {ship}: missing ShipWrenchCommandComponent — allocator has no input.");
            }

            if (!context.Components.Has<ShipManeuverPlanComponent>(ship))
            {
                VsLog.Warning(
                    $"[ShipMovement] Ship {ship}: missing ShipManeuverPlanComponent — planner cannot store paths.");
            }

            if (!context.Components.Has<ShipPlanExecutionComponent>(ship))
            {
                VsLog.Warning(
                    $"[ShipMovement] Ship {ship}: missing ShipPlanExecutionComponent — plan cannot execute.");
            }

            if (!context.Components.TryGet(ship, out SpaceMoveComponent _))
                VsLog.Warning($"[ShipMovement] Ship {ship}: missing SpaceMoveComponent — forces cannot accumulate.");

            if (!context.Components.TryGet(ship, out MassComponent shipMass) || shipMass.Value <= 0f)
            {
                VsLog.Warning(
                    $"[ShipMovement] Ship {ship}: MassComponent is zero. " +
                    "Add MassSourceComponent to each ship part archetype (cockpit, engine, thrusters, etc.).");
            }

            var comLocal = aggregate.CenterOfMassLocal;

            var rows = new List<ThrusterWrenchRow>();
            ShipThrustModel.BuildRows(context, ship, comLocal, rows);

            int partsMissingMass = 0;
            foreach (var (partEntity, part) in context.Components.GetAll<ShipPartComponent>())
            {
                if (part.ParentShip != ship) continue;
                if (!TryGetPartMassKg(context, partEntity, out _))
                    partsMissingMass++;
            }

            if (rows.Count == 0)
            {
                VsLog.Warning(
                    $"[ShipMovement] Ship {ship}: no thrusters found — the ship cannot move. " +
                    "Add thruster parts with a ThrusterComponent.");
            }
            else
            {
                var forwardForce = ShipThrustModel.ForceCapacityAlong(rows, Float3.Forward);

                float torqueAuthority = 0f;
                for (int i = 0; i < rows.Count; i++)
                    torqueAuthority += rows[i].TorqueLocal.Magnitude;

                VsLog.Info(
                    $"[ShipMovement] Ship {ship}: {rows.Count} thruster(s), " +
                    $"forward force capacity {forwardForce:F0} N, torque authority {torqueAuthority:F0} N·m.");

                if (forwardForce <= 1e-3f)
                {
                    VsLog.Warning(
                        $"[ShipMovement] Ship {ship}: no thruster produces +Z (forward) force. " +
                        "The autopilot cannot accelerate toward a target.");
                }

                if (torqueAuthority <= 1e-3f)
                {
                    VsLog.Warning(
                        $"[ShipMovement] Ship {ship}: thrusters produce no torque about the centre of mass. " +
                        "The ship cannot rotate to aim; place thrusters off-centre or add gimbal/RCS thrusters.");
                }
            }

            if (partsMissingMass > 0)
            {
                VsLog.Warning(
                    $"[ShipMovement] Ship {ship}: {partsMissingMass} part(s) contribute no mass. " +
                    "SpaceMovementSystem skips entities with zero mass.");
            }

            if (!ShipPartQueries.TryGetEngineFuel(context, ship, out _, out _))
            {
                VsLog.Info(
                    $"[ShipMovement] Ship {ship}: no engine fuel module. Thrusters may be cut off by FuelConsumptionSystem.");
            }

            var plant = ShipPlantModel.Build(context, ship);
            var validation = ShipValidator.Validate(context, ship, plant);
            VsLog.Info($"[ShipMovement] Ship {ship}: flight readiness {validation.Readiness}.");
            for (int i = 0; i < validation.BlockingIssues.Count; i++)
            {
                VsLog.Warning($"[ShipMovement] Ship {ship}: {validation.BlockingIssues[i]}");
            }
        }
    }
}
