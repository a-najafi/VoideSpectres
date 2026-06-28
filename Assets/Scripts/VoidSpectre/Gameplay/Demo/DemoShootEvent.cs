using VoidSpectre.Core;

namespace VoidSpectre.Gameplay.Demo
{
    public struct DemoShootEvent
    {
        public ComponentStore.EntityId Shooter;
        public ComponentStore.EntityId Target;
    }
}
