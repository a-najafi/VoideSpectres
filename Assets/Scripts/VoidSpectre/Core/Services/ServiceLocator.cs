using System;
using System.Collections.Generic;

namespace VoidSpectre.Core.Services
{
    [Serializable]
    public sealed class ServiceLocator
    {
        private readonly Dictionary<(Type, object), object> _services = new();

        public void Register(Type contract, object instance, object key = null)
        {
            if (contract == null || instance == null) return;
            _services[(contract, key ?? ServiceKey.None)] = instance;
        }

        public bool TryGet<T>(out T inst, object key = null)
        {
            if (_services.TryGetValue((typeof(T), key ?? ServiceKey.None), out var obj) && obj is T typed)
            {
                inst = typed;
                return true;
            }

            inst = default;
            return false;
        }

        public T Get<T>(object key = null)
        {
            if (TryGet<T>(out var t, key)) return t;
            throw new InvalidOperationException($"Service not found: {typeof(T).Name} (key: {key ?? "None"})");
        }

        public IReadOnlyDictionary<(Type, object), object> Dump() => _services;
    }

    public static class ServiceKey
    {
        public static readonly object None = null;
    }

    public interface ISceneServiceProvider
    {
        Type ContractType { get; }
        object Key { get; }
        void Register(ServiceLocator locator);
    }

    public interface IRequireSceneServices
    {
        void InjectServices(Context.SimulationUniverse universe, ServiceLocator services);
    }
}
