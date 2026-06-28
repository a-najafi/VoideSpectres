using System;
using System.Collections.Generic;
using VoidSpectre.Core.Interfaces;
using VoidSpectre.Gameplay.Physics;
using VoidSpectre.Gameplay.Ship.Config;
using VoidSpectre.Gameplay.Space;

namespace VoidSpectre.Gameplay.Demo
{
    public static class DemoArchetypeRequirements
    {
        public static IReadOnlyList<Type> Planet { get; } = new[]
        {
            typeof(SpacePositionComponent),
            typeof(GeometryVolumesComponent),
            typeof(GravitySourceComponent),
        };

        public static IReadOnlyList<Type> SpaceRock { get; } = new[]
        {
            typeof(SpacePositionComponent),
            typeof(SpaceOrientationComponent),
            typeof(SpaceMoveComponent),
            typeof(MassComponent),
            typeof(GeometryVolumesComponent),
            typeof(GravityAffectedComponent),
        };

        public static IReadOnlyList<Type> Ship { get; } = new[]
        {
            typeof(DemoShipHullTagComponent),
            typeof(GravityAffectedComponent),
            typeof(ShipPartsConfigComponent),
        };

        public static IReadOnlyList<Type> Crew { get; } = new[]
        {
            typeof(DemoCrewTagComponent),
            typeof(DemoInteriorPositionComponent),
        };

        public static bool HasComponentPrototypes(
            IReadOnlyList<ITrackableComponent> components,
            Type componentType)
        {
            if (components == null)
                return false;

            for (int i = 0; i < components.Count; i++)
            {
                if (components[i] != null && componentType.IsInstanceOfType(components[i]))
                    return true;
            }

            return false;
        }
    }
}
