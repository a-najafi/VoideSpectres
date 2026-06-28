using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VoidSpectre.Core.Interfaces;
using VoidSpectre.Core.Priority;
using VoidSpectre.Core.Services;

namespace VoidSpectre.Core.Context
{
    [Serializable]
    public sealed class SimulationContext
    {
        public readonly ComponentStore Components = new();
        public readonly EventStream Events = new();

        public SimulationContextId Id { get; }
        public ContextKind Kind { get; }
        public SimulationUniverse Universe { get; }
        public SimulationContext Parent { get; private set; }
        public string DisplayName { get; }

        private readonly List<SimulationContext> _children = new();
        private readonly PriorityRegistry<ICoreUpdateSystem> _coreUpdates;
        private readonly Dictionary<Type, object> _componentChangeSystems = new();
        private readonly Dictionary<Type, object> _eventSystems = new();
        private readonly SystemOrderProvider _orderProvider;

        public ComponentStore.EntityId MembershipRegistryEntity { get; private set; }
        public TickTier CurrentTickTier { get; internal set; } = TickTier.Background;
        internal float TickAccumulator { get; set; }

        public IReadOnlyList<SimulationContext> Children => _children;

        internal SimulationContext(
            SimulationUniverse universe,
            SimulationContextId id,
            ContextKind kind,
            SimulationContext parent,
            string displayName,
            SystemOrderConfigData config,
            bool autoRegisterSystems)
        {
            Universe = universe;
            Id = id;
            Kind = kind;
            Parent = parent;
            DisplayName = displayName;
            _orderProvider = new SystemOrderProvider(config);
            _coreUpdates = new PriorityRegistry<ICoreUpdateSystem>(s => s.Priority);

            MembershipRegistryEntity = universe.CreateEntityId();
            Components.Set(MembershipRegistryEntity, new ContextMembershipComponent());

            if (autoRegisterSystems)
                AutoSystemRegistry.RegisterAll(this);
        }

        internal void AddChild(SimulationContext child)
        {
            child.Parent = this;
            _children.Add(child);
        }

        public ComponentStore.EntityId CreateEntity()
        {
            var id = Universe.CreateEntityId();
            RegisterMember(id);
            return id;
        }

        public void RegisterMember(ComponentStore.EntityId entity)
        {
            if (Components.TryGet(MembershipRegistryEntity, out ContextMembershipComponent membership))
                membership.AddMember(entity);
        }

        public void UnregisterMember(ComponentStore.EntityId entity)
        {
            if (Components.TryGet(MembershipRegistryEntity, out ContextMembershipComponent membership))
                membership.RemoveMember(entity);
        }

        public void InstallServices(ServiceLocator services) => Universe.InstallServices(services);

        public void RegisterCoreSystem(ICoreUpdateSystem sys)
        {
            ApplyConfiguredPriority(sys, null);
            _coreUpdates.Add(sys);
        }

        public void RegisterComponentChangeSystem<T>(IComponentChangeSystem<T> sys)
            where T : class, ITrackableComponent
        {
            ApplyConfiguredPriority(sys, typeof(T));

            if (!_componentChangeSystems.TryGetValue(typeof(T), out _))
            {
                var reg = new PriorityRegistry<IComponentChangeSystem<T>>(s => s.Priority);
                _componentChangeSystems[typeof(T)] = reg;

                Components.Subscribe<T>(batch =>
                {
                    foreach (var s in reg.GetAll()) s.OnComponentChanged(this, batch);
                });
            }

            ((PriorityRegistry<IComponentChangeSystem<T>>)_componentChangeSystems[typeof(T)]).Add(sys);
        }

        public void RegisterEventSystem(ISystem sys)
        {
            var eventInterfaces = sys.GetType().GetInterfaces()
                .Where(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEventSystem<>));

            foreach (var interfaceType in eventInterfaces)
            {
                var eventType = interfaceType.GetGenericArguments()[0];
                var registerHelperMethod = typeof(SimulationContext)
                    .GetMethod(nameof(RegisterEventSystemForType), BindingFlags.NonPublic | BindingFlags.Instance)
                    .MakeGenericMethod(eventType);

                registerHelperMethod.Invoke(this, new object[] { sys });
            }
        }

        private void RegisterEventSystemForType<T>(IEventSystem<T> sys) where T : struct
        {
            ApplyConfiguredPriority(sys, typeof(T));

            if (!_eventSystems.TryGetValue(typeof(T), out _))
            {
                var reg = new PriorityRegistry<IEventSystem<T>>(s => s.Priority);
                _eventSystems[typeof(T)] = reg;

                Events.Subscribe<T>(evt =>
                {
                    foreach (var s in reg.GetAll()) s.OnEvent(this, evt);
                });
            }

            ((PriorityRegistry<IEventSystem<T>>)_eventSystems[typeof(T)]).Add(sys);
        }

        private void ApplyConfiguredPriority(ISystem sys, Type triggerType)
        {
            var p = _orderProvider.GetPriority(sys.GetType(), triggerType);
            if (sys is ISettablePriority settable) settable.SetPriority(p);
        }

        public void BeginTick() => Events.BeginTick();

        public void UpdateCore(float delta)
        {
            var snapshot = new List<ICoreUpdateSystem>(_coreUpdates.GetAll());
            foreach (var s in snapshot) s.Update(this, delta);
        }

        public void EndTick()
        {
            Components.EndTick();
            Events.EndTick();
        }

        internal IEnumerable<ISystem> EnumerateAllSystems()
        {
            foreach (var s in _coreUpdates.GetAll()) yield return s;

            foreach (var kv in _eventSystems.Values)
            {
                var getAll = kv.GetType().GetMethod("GetAll");
                foreach (var s in (IEnumerable<ISystem>)getAll.Invoke(kv, null))
                    yield return s;
            }

            foreach (var kv in _componentChangeSystems.Values)
            {
                var getAll = kv.GetType().GetMethod("GetAll");
                foreach (var s in (IEnumerable<ISystem>)getAll.Invoke(kv, null))
                    yield return s;
            }
        }
    }
}
