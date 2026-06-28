using System.Collections.Generic;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Interfaces;

namespace VoidSpectre.Core.Config
{
    public sealed class EntityArchetype : IEntityArchetype
    {
        private readonly ITrackableComponent[] _components;

        public EntityArchetype(IReadOnlyList<ITrackableComponent> components)
        {
            _components = new ITrackableComponent[components.Count];
            for (int i = 0; i < components.Count; i++)
                _components[i] = components[i];
        }

        public EntityArchetype(params ITrackableComponent[] components) =>
            _components = components;

        public void ApplyTo(SimulationContext context, ComponentStore.EntityId entity)
        {
            foreach (var prototype in _components)
            {
                if (prototype == null) continue;
                context.Components.Set(entity, ComponentPrototypeCloner.Clone(prototype));
            }
        }
    }
}
