using System.Collections.Generic;
using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Diagnostics;
using VoidSpectre.Core.Math;
using VoidSpectre.Gameplay.Physics;
using VoidSpectre.Gameplay.Ship.Parts;
using VoidSpectre.Gameplay.Space;
using VoidSpectreUnity.Conversion;
using UnityEngine;

namespace VoidSpectreUnity.View
{
    public static class EntityWorldTransform
    {
        private static readonly HashSet<ComponentStore.EntityId> _loggedMissing = new();

        public static bool TryGetWorldPose(
            SimulationContext context,
            ComponentStore.EntityId entity,
            out Vector3 position,
            out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            if (TryGetShipPartWorldPose(context, entity, out position, out rotation))
                return true;

            if (TryGetSpaceBodyWorldPose(context, entity, out position, out rotation))
                return true;

            LogMissingOnce(context, entity);
            return false;
        }

        private static bool TryGetSpaceBodyWorldPose(
            SimulationContext context,
            ComponentStore.EntityId entity,
            out Vector3 position,
            out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            if (!context.Components.TryGet(entity, out SpacePositionComponent spacePosition))
                return false;

            position = spacePosition.Value.ToUnity();
            if (context.Components.TryGet(entity, out SpaceOrientationComponent orientation))
                rotation = orientation.Value.ToUnity();
            return true;
        }

        private static bool TryGetShipPartWorldPose(
            SimulationContext context,
            ComponentStore.EntityId entity,
            out Vector3 position,
            out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;

            if (!context.Components.TryGet(entity, out ShipPartComponent part))
                return false;
            if (!context.Components.TryGet(entity, out LocalTransformComponent local))
                return false;
            if (!context.Components.TryGet(part.ParentShip, out SpacePositionComponent shipPosition))
                return false;

            var shipRotation = context.Components.TryGet(part.ParentShip, out SpaceOrientationComponent shipOrientation)
                ? shipOrientation.Value
                : FloatQuaternion.Identity;

            var worldPosition = shipPosition.Value + shipRotation * local.LocalPosition;
            var worldRotation = shipRotation * local.LocalRotation;

            position = worldPosition.ToUnity();
            rotation = worldRotation.ToUnity();
            return true;
        }

        private static void LogMissingOnce(SimulationContext context, ComponentStore.EntityId entity)
        {
            if (!_loggedMissing.Add(entity))
                return;

            VsLog.Warning(
                $"[EntityWorldTransform] Could not resolve world pose for {entity} in context {context.Id}.");
        }
    }
}
