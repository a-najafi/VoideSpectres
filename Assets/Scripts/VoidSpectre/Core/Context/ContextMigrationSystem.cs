using VoidSpectre.Core;
using VoidSpectre.Core.Interfaces;

namespace VoidSpectre.Core.Context
{
    public sealed class ContextMigrationSystem : IEventSystem<EntityExitContextRequested>
    {
        public string Name => "Context Migration";
        public int Priority => 0;

        public void OnEvent(SimulationContext context, EntityExitContextRequested evt)
        {
            if (!context.Universe.TryGetContext(evt.DestinationContextId, out var destination))
                return;

            context.Universe.MigrateEntity(context, destination, evt.Entity);
        }
    }
}
