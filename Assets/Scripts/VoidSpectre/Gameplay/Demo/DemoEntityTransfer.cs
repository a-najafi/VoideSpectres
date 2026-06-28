using VoidSpectre.Core;
using VoidSpectre.Core.Context;
using VoidSpectre.Core.Math;
using VoidSpectre.Gameplay.Space;

namespace VoidSpectre.Gameplay.Demo
{
    public sealed class DemoEntityTransfer : IContextEntityTransfer
    {
        public bool CanTransfer(ComponentStore source, ComponentStore.EntityId entity) =>
            source.Has<DemoCrewTagComponent>(entity);

        public void Transfer(ComponentStore source, ComponentStore destination, ComponentStore.EntityId entity)
        {
            if (source.TryGet(entity, out DemoCrewTagComponent crewTag))
            {
                destination.Set(entity, crewTag);
                source.Remove<DemoCrewTagComponent>(entity);
            }

            if (source.TryGet(entity, out DemoInteriorPositionComponent interiorPos))
            {
                destination.Set(entity, new SpacePositionComponent(new Float3(interiorPos.Value, 0f, 0f)));
                source.Remove<DemoInteriorPositionComponent>(entity);
            }
        }
    }
}
