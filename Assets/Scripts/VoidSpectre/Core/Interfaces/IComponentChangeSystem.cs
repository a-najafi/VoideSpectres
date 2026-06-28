using VoidSpectre.Core.Context;

namespace VoidSpectre.Core.Interfaces
{
    public interface IComponentChangeSystem<T> : ISystem where T : class, ITrackableComponent
    {
        void OnComponentChanged(SimulationContext context, ComponentStore.ChangeBatch<T> changes);
    }
}
