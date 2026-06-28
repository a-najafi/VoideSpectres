using VoidSpectre.Core;

namespace VoidSpectre.Core.Context
{
    public interface IContextEntityTransfer
    {
        bool CanTransfer(ComponentStore source, ComponentStore.EntityId entity);
        void Transfer(ComponentStore source, ComponentStore destination, ComponentStore.EntityId entity);
    }
}
