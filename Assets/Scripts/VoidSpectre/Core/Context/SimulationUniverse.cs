using System;
using System.Collections.Generic;
using VoidSpectre.Core.Priority;
using VoidSpectre.Core.Services;

namespace VoidSpectre.Core.Context
{
    public sealed class SimulationUniverse
    {
        public const float MaxAccumulatedDelta = 0.25f;

        private readonly List<SimulationContext> _contexts = new();
        private readonly Dictionary<SimulationContextId, SimulationContext> _contextById = new();
        private readonly List<IContextEntityTransfer> _entityTransfers = new();
        private int _nextEntityId = 1;
        private int _nextContextId = 1;

        public SimulationScheduler Scheduler { get; }
        public ServiceLocator Services { get; } = new();
        public SystemOrderConfigData SystemOrderConfig { get; }

        public IReadOnlyList<SimulationContext> Contexts => _contexts;
        public IReadOnlyList<IContextEntityTransfer> EntityTransfers => _entityTransfers;

        public SimulationUniverse(SystemOrderConfigData systemOrderConfig = null)
        {
            SystemOrderConfig = systemOrderConfig;
            Scheduler = new SimulationScheduler(this);
        }

        public ComponentStore.EntityId CreateEntityId()
        {
            var id = _nextEntityId++;
            return new ComponentStore.EntityId(id);
        }

        public SimulationContext CreateContext(
            ContextKind kind,
            SimulationContext parent,
            string displayName,
            bool autoRegisterSystems = true)
        {
            var id = new SimulationContextId(_nextContextId++);
            var context = new SimulationContext(
                this,
                id,
                kind,
                parent,
                displayName,
                SystemOrderConfig,
                autoRegisterSystems);

            _contexts.Add(context);
            _contextById[id] = context;

            if (parent != null)
                parent.AddChild(context);

            return context;
        }

        public bool TryGetContext(SimulationContextId id, out SimulationContext context) =>
            _contextById.TryGetValue(id, out context);

        public SimulationContext GetContext(SimulationContextId id)
        {
            if (_contextById.TryGetValue(id, out var context)) return context;
            throw new KeyNotFoundException($"Context not found: {id}");
        }

        public void RegisterEntityTransfer(IContextEntityTransfer transfer)
        {
            if (transfer != null) _entityTransfers.Add(transfer);
        }

        public void InstallServices(ServiceLocator services)
        {
            foreach (var context in _contexts)
            {
                foreach (var sys in context.EnumerateAllSystems())
                {
                    if (sys is IRequireSceneServices needs)
                        needs.InjectServices(this, services);
                }
            }
        }

        public void InjectServicesIntoContext(SimulationContext context, ServiceLocator services)
        {
            foreach (var sys in context.EnumerateAllSystems())
            {
                if (sys is IRequireSceneServices needs)
                    needs.InjectServices(this, services);
            }
        }

        /// <summary>
        /// Moves an entity from source to destination context. Only call from migration bridge code.
        /// </summary>
        public void MigrateEntity(
            SimulationContext source,
            SimulationContext destination,
            ComponentStore.EntityId entity)
        {
            if (source == null || destination == null) return;

            IContextEntityTransfer matched = null;
            foreach (var transfer in _entityTransfers)
            {
                if (transfer.CanTransfer(source.Components, entity))
                {
                    matched = transfer;
                    break;
                }
            }

            if (matched == null) return;

            matched.Transfer(source.Components, destination.Components, entity);

            source.UnregisterMember(entity);
            destination.RegisterMember(entity);

            source.Events.Enqueue(new EntityExitedContext
            {
                Entity = entity,
                DestinationContextId = destination.Id
            });

            destination.Events.Enqueue(new EntityEnteredContext
            {
                Entity = entity,
                SourceContextId = source.Id
            });
        }
    }
}
