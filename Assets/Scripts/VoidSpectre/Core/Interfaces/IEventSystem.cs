using VoidSpectre.Core.Context;

namespace VoidSpectre.Core.Interfaces
{
    public interface IEventSystem<T> : ISystem where T : struct
    {
        void OnEvent(SimulationContext context, T evt);
    }
}
