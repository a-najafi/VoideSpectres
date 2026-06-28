using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using VoidSpectre.Core;
using VoidSpectre.Core.Config;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Interfaces;

namespace VoidSpectreUnity.Config
{
    [CreateAssetMenu(menuName = "VoidSpectre/Entity Archetype")]
    public sealed class EntityArchetypeSO : SerializedScriptableObject, IEntityArchetype
    {
        [OdinSerialize]
        private string _archetypeId;

        [OdinSerialize]
        public List<ITrackableComponent> Components = new();

        public string ArchetypeId => _archetypeId;

        private void OnEnable()
        {
            if (!string.IsNullOrEmpty(_archetypeId))
                EntityArchetypeRegistry.Register(_archetypeId, this);
        }

        public ComponentStore.EntityId Spawn(SimulationContext context)
        {
            var entity = context.CreateEntity();
            ApplyTo(context, entity);
            return entity;
        }

        public void ApplyTo(SimulationContext context, ComponentStore.EntityId entity)
        {
            foreach (var component in Components)
            {
                if (component == null) continue;
                context.Components.Set(entity, ComponentPrototypeCloner.Clone(component));
            }
        }
    }
}
