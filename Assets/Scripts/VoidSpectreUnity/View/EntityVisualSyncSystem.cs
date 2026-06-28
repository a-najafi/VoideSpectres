using System.Collections.Generic;
using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Gameplay.Ship.Parts;
using UnityEngine;
using VoidSpectre.Core.Interfaces;

namespace VoidSpectreUnity.View
{
    [RunsInContext(ContextKind.Volume)]
    public sealed class EntityVisualSyncSystem : ICoreUpdateSystem
    {
        public string Name => "Entity Visual Sync";
        public int Priority => 100;

        private readonly Dictionary<ComponentStore.EntityId, GameObject> _instances = new();
        private readonly HashSet<ComponentStore.EntityId> _reportedMissingVisual = new();

        public void Update(SimulationContext context, float delta)
        {
            if (EntityVisualRootMB.Root == null)
                return;

            var activeEntities = new HashSet<ComponentStore.EntityId>();

            foreach (var (entity, visual) in context.Components.GetAll<EntityVisualComponent>())
            {
                if (visual.Prefab == null)
                {
                    ReportMissingVisual(entity, "EntityVisualComponent.Prefab is not assigned.");
                    continue;
                }

                activeEntities.Add(entity);
                EnsureInstance(context, entity, visual.Prefab);
            }

            foreach (var (entity, _) in context.Components.GetAll<ShipPartComponent>())
            {
                if (context.Components.Has<EntityVisualComponent>(entity))
                    continue;

                ReportMissingVisual(entity, "Ship part entity is missing EntityVisualComponent.");
            }

            SyncActiveInstances(context, activeEntities);
            DespawnStaleInstances(activeEntities);
        }

        private void ReportMissingVisual(ComponentStore.EntityId entity, string reason)
        {
            if (!_reportedMissingVisual.Add(entity))
                return;

            Debug.LogError($"[VoidSpectre] Entity {entity} has no visual: {reason}", EntityVisualRootMB.Root);
        }

        private void EnsureInstance(
            SimulationContext context,
            ComponentStore.EntityId entity,
            GameObject prefab)
        {
            if (_instances.ContainsKey(entity))
                return;

            var instance = Object.Instantiate(prefab, EntityVisualRootMB.Root);
            instance.name = $"{prefab.name} ({entity})";
            instance.transform.localScale = GeometryVisualScaleUtility.TryGetVisualScale(context, entity, out var scale)
                ? scale
                : Vector3.one;
            _instances[entity] = instance;
        }

        private void SyncActiveInstances(
            SimulationContext context,
            HashSet<ComponentStore.EntityId> activeEntities)
        {
            foreach (var entity in activeEntities)
            {
                if (!_instances.TryGetValue(entity, out var instance) || instance == null)
                    continue;

                if (!EntityWorldTransform.TryGetWorldPose(context, entity, out var position, out var rotation))
                    continue;

                instance.transform.SetPositionAndRotation(position, rotation);
            }
        }

        private void DespawnStaleInstances(HashSet<ComponentStore.EntityId> activeEntities)
        {
            var toRemove = new List<ComponentStore.EntityId>();

            foreach (var pair in _instances)
            {
                if (activeEntities.Contains(pair.Key))
                    continue;

                if (pair.Value != null)
                    Object.Destroy(pair.Value);

                toRemove.Add(pair.Key);
            }

            for (int i = 0; i < toRemove.Count; i++)
                _instances.Remove(toRemove[i]);
        }
    }
}
