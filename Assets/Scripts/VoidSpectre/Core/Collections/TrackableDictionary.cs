using System;
using System.Collections.Generic;
using VoidSpectre.Core.Interfaces;

namespace VoidSpectre.Core.Collections
{
    public sealed class TrackableDictionary<TKey, TValue> : ITrackableNode
    {
        private readonly Dictionary<TKey, TValue> _dict = new();
        public event Action Changed;

        private void Ping() => Changed?.Invoke();

        public bool TryGetValue(TKey key, out TValue value) => _dict.TryGetValue(key, out value);

        public void Set(TKey key, TValue value)
        {
            _dict[key] = value;
            Ping();
        }

        public bool Remove(TKey key)
        {
            if (_dict.Remove(key))
            {
                Ping();
                return true;
            }

            return false;
        }

        public void Clear()
        {
            if (_dict.Count > 0)
            {
                _dict.Clear();
                Ping();
            }
        }

        public bool ContainsKey(TKey key) => _dict.ContainsKey(key);
        public IReadOnlyDictionary<TKey, TValue> Readonly => _dict;
    }
}
