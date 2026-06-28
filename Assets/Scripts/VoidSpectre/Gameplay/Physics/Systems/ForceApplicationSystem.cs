using VoidSpectre.Core.Context;
using VoidSpectre.Core.Interfaces;
using VoidSpectre.Gameplay.Space;

namespace VoidSpectre.Gameplay.Physics.Systems
{
    [RunsInContext(ContextKind.Volume)]
    public sealed class ForceApplicationSystem : ICoreUpdateSystem
    {
        public string Name => "Force Application";
        public int Priority => 5;

        public void Update(SimulationContext context, float delta)
        {
            foreach (var (_, move) in context.Components.GetAll<SpaceMoveComponent>())
                move.ClearForces();
        }
    }
}
