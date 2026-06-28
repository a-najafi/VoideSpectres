using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using VoidSpectre.Core.Interfaces;
using VoidSpectre.Gameplay.Demo;
using VoidSpectre.Gameplay.Physics;
using VoidSpectre.Gameplay.Ship.Config;
using VoidSpectreUnity.Config;
using VoidSpectreUnity.View;

namespace VoidSpectreUnity.Demo
{
    public static class DemoArchetypeValidator
    {
        public static bool TryValidate(DemoArchetypeCatalogSO catalog, out string errorMessage)
        {
            var errors = new List<string>();
            if (catalog == null)
            {
                errorMessage = "Demo archetype catalog is null.";
                return false;
            }

            ValidateArchetype(catalog.PlanetArchetypeAsset, DemoArchetypeRequirements.Planet, "Planet", errors);
            ValidateArchetype(catalog.SpaceRockArchetypeAsset, DemoArchetypeRequirements.SpaceRock, "Space Rock", errors);
            ValidateShipArchetype(catalog.ShipArchetypeAsset, errors);
            ValidateArchetype(catalog.CrewArchetypeAsset, DemoArchetypeRequirements.Crew, "Crew", errors);

            if (errors.Count == 0)
            {
                errorMessage = null;
                return true;
            }

            var builder = new StringBuilder();
            builder.AppendLine("[VoidSpectre] Demo archetype catalog validation failed:");
            for (int i = 0; i < errors.Count; i++)
                builder.AppendLine($"  - {errors[i]}");

            errorMessage = builder.ToString();
            return false;
        }

        public static void ValidateOrThrow(DemoArchetypeCatalogSO catalog)
        {
            if (!TryValidate(catalog, out var message))
                throw new InvalidOperationException(message);
        }

        private static void ValidateArchetype(
            EntityArchetypeSO archetype,
            IReadOnlyList<Type> requiredComponents,
            string label,
            List<string> errors)
        {
            if (archetype == null)
            {
                errors.Add($"{label}: no EntityArchetypeSO assigned.");
                return;
            }

            for (int i = 0; i < requiredComponents.Count; i++)
            {
                var required = requiredComponents[i];
                if (!DemoArchetypeRequirements.HasComponentPrototypes(archetype.Components, required))
                {
                    errors.Add(
                        $"{label} ('{archetype.name}'): missing required component {required.Name}.");
                }
            }
        }

        private static void ValidateShipArchetype(EntityArchetypeSO shipArchetype, List<string> errors)
        {
            ValidateArchetype(shipArchetype, DemoArchetypeRequirements.Ship, "Ship", errors);
            if (shipArchetype?.Components == null)
                return;

            ShipPartsConfigComponent partsConfig = null;
            for (int i = 0; i < shipArchetype.Components.Count; i++)
            {
                if (shipArchetype.Components[i] is ShipPartsConfigComponent config)
                {
                    partsConfig = config;
                    break;
                }
            }

            if (partsConfig == null)
                return;

            if (partsConfig.Parts == null || partsConfig.Parts.Count == 0)
            {
                errors.Add($"Ship ('{shipArchetype.name}'): ShipPartsConfigComponent.Parts is empty.");
                return;
            }

            for (int i = 0; i < partsConfig.Parts.Count; i++)
            {
                var placement = partsConfig.Parts[i];
                if (placement?.Archetype == null)
                {
                    errors.Add($"Ship ('{shipArchetype.name}'): part [{i}] has no archetype.");
                    continue;
                }

                if (placement.Archetype is not EntityArchetypeSO partArchetype)
                {
                    errors.Add(
                        $"Ship ('{shipArchetype.name}'): part [{i}] must reference an EntityArchetypeSO asset.");
                    continue;
                }

                if (!DemoArchetypeRequirements.HasComponentPrototypes(
                        partArchetype.Components,
                        typeof(EntityVisualComponent)))
                {
                    errors.Add(
                        $"Ship ('{shipArchetype.name}'): part [{i}] archetype '{partArchetype.name}' " +
                        "is missing EntityVisualComponent with a prefab.");
                    continue;
                }

                if (EntityVisualUtility.TryGetPrefab(partArchetype) == null)
                {
                    errors.Add(
                        $"Ship ('{shipArchetype.name}'): part [{i}] archetype '{partArchetype.name}' " +
                        "has EntityVisualComponent but no prefab assigned.");
                }

                ValidateShipPartArchetype(shipArchetype.name, i, partArchetype, errors);
            }
        }

        private static void ValidateShipPartArchetype(
            string shipName,
            int partIndex,
            EntityArchetypeSO partArchetype,
            List<string> errors)
        {
            var label = $"Ship ('{shipName}'): part [{partIndex}] archetype '{partArchetype.name}'";

            if (!DemoArchetypeRequirements.HasComponentPrototypes(
                    partArchetype.Components,
                    typeof(MassSourceComponent)))
            {
                errors.Add($"{label} is missing MassSourceComponent — ship mass will stay zero.");
            }

            if (!DemoArchetypeRequirements.HasComponentPrototypes(
                    partArchetype.Components,
                    typeof(GeometryVolumesComponent)))
            {
                errors.Add($"{label} is missing GeometryVolumesComponent.");
            }

            // Thrusters no longer need a mount role: the control allocator derives each thruster's
            // contribution from its position and orientation. A thruster part just needs a
            // ThrusterComponent plus the geometry/mass checked above.
        }
    }
}
