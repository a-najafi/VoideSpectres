using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Diagnostics;
using VoidSpectre.Core.Math;
using VoidSpectre.Gameplay.Physics;
using VoidSpectre.Gameplay.Space;

namespace VoidSpectre.Gameplay.Demo
{
    public static class DemoHierarchySetup
    {
        private static readonly Float3 DemoPlanetCenter = Float3.Zero;

        public static ComponentStore.EntityId ShipEntity { get; private set; }
        public static ComponentStore.EntityId CrewEntity { get; private set; }
        public static ComponentStore.EntityId PlanetEntity { get; private set; }
        public static ComponentStore.EntityId SpaceRockEntity { get; private set; }

        public static void Spawn(
            SimulationContext sector,
            SimulationContext shipInterior,
            IDemoArchetypeProvider archetypes)
        {
            if (archetypes == null)
                throw new System.ArgumentNullException(nameof(archetypes));

            PlanetEntity = SpawnPlanet(sector, archetypes.PlanetArchetype);
            var planetCenter = DemoSpawnGeometry.GetSpacePosition(sector, PlanetEntity);
            if (!DemoSpawnGeometry.TryGetSphereRadius(sector, PlanetEntity, out var planetRadius))
            {
                VsLog.Warning(
                    $"[DemoHierarchySetup] Planet entity {PlanetEntity} has no sphere geometry; " +
                    "using fallback radius 200m for demo placement.");
                planetRadius = 200f;
            }

            SpaceRockEntity = SpawnSpaceRock(sector, archetypes.SpaceRockArchetype, planetCenter, planetRadius);
            ShipEntity = SpawnShip(sector, shipInterior, archetypes.ShipArchetype, planetCenter, planetRadius);
            CrewEntity = SpawnCrew(shipInterior, archetypes.CrewArchetype);
        }

        private static ComponentStore.EntityId SpawnPlanet(
            SimulationContext sector,
            Core.Config.IEntityArchetype archetype)
        {
            var entity = SpawnFromArchetype(sector, archetype);
            sector.Components.Set(entity, new SpacePositionComponent(DemoPlanetCenter));
            return entity;
        }

        private static ComponentStore.EntityId SpawnSpaceRock(
            SimulationContext sector,
            Core.Config.IEntityArchetype archetype,
            Float3 planetCenter,
            float planetRadius)
        {
            var entity = SpawnFromArchetype(sector, archetype);

            DemoSpawnGeometry.TryGetSphereRadius(sector, entity, out var rockRadius);
            var rockPosition = DemoSpawnGeometry.PositionOutsideSphere(
                planetCenter,
                planetRadius,
                rockRadius,
                DemoSpawnGeometry.DefaultSpaceRockDirection(),
                DemoSpawnGeometry.RockSurfaceClearanceMeters);

            sector.Components.Set(entity, new SpacePositionComponent(rockPosition));

            return entity;
        }

        private static ComponentStore.EntityId SpawnShip(
            SimulationContext sector,
            SimulationContext shipInterior,
            Core.Config.IEntityArchetype archetype,
            Float3 planetCenter,
            float planetRadius)
        {
            var ship = SpawnFromArchetype(sector, archetype);

            sector.Components.Set(ship, new ContextBoundaryComponent
            {
                LinkedContextId = shipInterior.Id,
                IsParentSide = true
            });

            var shipPosition = DemoSpawnGeometry.PositionOutsideSphere(
                planetCenter,
                planetRadius,
                bodyRadius: 0f,
                Float3.Back,
                DemoSpawnGeometry.ShipSurfaceClearanceMeters);

            ShipSpaceSetup.Configure(
                sector,
                ship,
                initialPosition: shipPosition,
                initialDirection: Float3.Forward,
                initialSpeed: 0f);

            shipInterior.Components.Set(ship, new DemoShipHullTagComponent());
            shipInterior.Components.Set(ship, new DemoInteriorLayoutComponent("Bridge+Corridor"));
            shipInterior.Components.Set(ship, new ContextBoundaryComponent
            {
                LinkedContextId = sector.Id,
                IsParentSide = false
            });
            shipInterior.RegisterMember(ship);

            return ship;
        }

        private static ComponentStore.EntityId SpawnCrew(
            SimulationContext shipInterior,
            Core.Config.IEntityArchetype archetype) =>
            SpawnFromArchetype(shipInterior, archetype);

        private static ComponentStore.EntityId SpawnFromArchetype(
            SimulationContext context,
            Core.Config.IEntityArchetype archetype)
        {
            if (archetype == null)
                throw new System.ArgumentNullException(nameof(archetype));

            var entity = context.CreateEntity();
            archetype.ApplyTo(context, entity);
            return entity;
        }
    }
}
