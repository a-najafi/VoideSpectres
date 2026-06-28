using VoidSpectre.Core;
using VoidSpectre.Core.Context;

namespace VoidSpectre.Core.Config
{
    public interface IEntityArchetype
    {
        void ApplyTo(SimulationContext context, ComponentStore.EntityId entity);
    }
}
