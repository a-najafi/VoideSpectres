using VoidSpectre.Core.Context;

namespace VoidSpectre.Core.Interfaces
{
    public interface ICoreUpdateSystem : ISystem
    {
        void Update(SimulationContext context, float delta);
    }
}
