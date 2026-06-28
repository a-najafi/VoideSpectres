using System;
using System.Collections.Generic;
using Sirenix.Serialization;
using VoidSpectre.Core.Interfaces;

namespace VoidSpectre.Core
{
    [Serializable]
    public sealed class ComponentStore
    {
        [Serializable]
        public readonly struct EntityId : IEquatable<EntityId>
        {
            [OdinSerialize] public readonly int Id;
            public EntityId(int id) => Id = id;
            public bool Equals(EntityId other) => Id == other.Id;
            public override bool Equals(object obj) => obj is EntityId other && Equals(other);
            public override int GetHashCode() => Id;
            public override string ToString() => $"E{Id}";
            public static bool operator ==(EntityId a, EntityId b) => a.Id == b.Id;
            public static bool operator !=(EntityId a, EntityId b) => a.Id != b.Id;
        }

        public sealed class ChangeBatch<T> where T : class, ITrackableComponent
        {
            public readonly List<(EntityId entity, T component)> Added = new();
            public readonly List<(EntityId entity, T component)> Updated = new();
            public readonly List<(EntityId entity, T component)> Removed = new();
        }

        private readonly Dictionary<Type, object> _tables = new();

        private Table<T> GetTable<T>() where T : class, ITrackableComponent
        {
            var type = typeof(T);
            if (!_tables.TryGetValue(type, out var boxed))
            {
                boxed = new Table<T>();
                _tables[type] = boxed;
            }

            return (Table<T>)boxed;
        }

        public bool Has<T>(EntityId e) where T : class, ITrackableComponent => GetTable<T>().TryGet(e, out _);

        public void Set(EntityId e, ITrackableComponent component)
        {
            if (component == null) return;
            var t = component.GetType();
            if (!_tables.TryGetValue(t, out var boxed))
            {
                boxed = Activator.CreateInstance(typeof(Table<>).MakeGenericType(t));
                _tables[t] = boxed;
            }

            ((ITable)boxed).SetObject(e, component);
        }

        public void Set<T>(EntityId e, T component) where T : class, ITrackableComponent =>
            GetTable<T>().Set(e, component);

        public bool TryGet<T>(EntityId e, out T component) where T : class, ITrackableComponent =>
            GetTable<T>().TryGet(e, out component);

        public bool Remove<T>(EntityId e) where T : class, ITrackableComponent => GetTable<T>().Remove(e);

        public void RemoveAll(EntityId e)
        {
            foreach (var boxed in _tables.Values)
                ((ITable)boxed).RemoveEntity(e);
        }

        public IEnumerable<(EntityId entity, T component)> GetAll<T>() where T : class, ITrackableComponent =>
            GetTable<T>().GetAll();

        public IDisposable Subscribe<T>(Action<ChangeBatch<T>> onBatch)
            where T : class, ITrackableComponent =>
            GetTable<T>().Subscribe(onBatch);

        public void EndTick()
        {
            const int safetyMaxPasses = 8;
            int passes = 0;
            int processedCount = 0;

            while (passes++ < safetyMaxPasses)
            {
                var snapshot = new List<object>(_tables.Values);
                if (snapshot.Count == processedCount) break;

                for (int i = processedCount; i < snapshot.Count; i++)
                    ((ITable)snapshot[i]).EndFrame();

                processedCount = snapshot.Count;
            }
        }

        private interface ITable
        {
            void EndFrame();
            void SetObject(EntityId e, ITrackableComponent c);
            bool RemoveEntity(EntityId e);
        }

        private sealed class Table<T> : ITable where T : class, ITrackableComponent
        {
            private readonly Dictionary<EntityId, T> _items = new();
            private readonly Dictionary<EntityId, uint> _lastSeenVersion = new();
            private readonly List<Action<ChangeBatch<T>>> _subs = new();
            private readonly HashSet<EntityId> _addedThisFrame = new();
            private readonly HashSet<EntityId> _removedThisFrame = new();
            private readonly HashSet<EntityId> _replacedThisFrame = new();

            bool ITable.RemoveEntity(EntityId e) => Remove(e);
            void ITable.SetObject(EntityId e, ITrackableComponent c) => Set(e, (T)c);

            public void Set(EntityId e, T c)
            {
                if (!_items.ContainsKey(e) && !_lastSeenVersion.ContainsKey(e))
                {
                    _items[e] = c;
                    _removedThisFrame.Remove(e);
                    _addedThisFrame.Add(e);
                    return;
                }

                _items[e] = c;
                _removedThisFrame.Remove(e);
                _replacedThisFrame.Add(e);
            }

            public bool TryGet(EntityId e, out T c) => _items.TryGetValue(e, out c);

            public bool Remove(EntityId e)
            {
                var existed = _items.Remove(e);
                if (!_addedThisFrame.Remove(e) && existed)
                    _removedThisFrame.Add(e);
                return existed;
            }

            public IEnumerable<(EntityId entity, T component)> GetAll()
            {
                foreach (var kv in _items) yield return (kv.Key, kv.Value);
            }

            public IDisposable Subscribe(Action<ChangeBatch<T>> onBatch)
            {
                _subs.Add(onBatch);
                return new Unsub(_subs, onBatch);
            }

            public void EndFrame()
            {
                if (_subs.Count == 0)
                {
                    foreach (var kv in _items)
                        _lastSeenVersion[kv.Key] = kv.Value.LocalVersion;
                    _addedThisFrame.Clear();
                    _removedThisFrame.Clear();
                    _replacedThisFrame.Clear();
                    return;
                }

                var batch = new ChangeBatch<T>();

                foreach (var id in _addedThisFrame)
                    if (_items.TryGetValue(id, out var comp))
                        batch.Added.Add((id, comp));

                foreach (var kv in _items)
                {
                    var id = kv.Key;
                    if (_addedThisFrame.Contains(id) || _removedThisFrame.Contains(id)) continue;
                    var comp = kv.Value;

                    if (_replacedThisFrame.Contains(id))
                        batch.Updated.Add((id, comp));
                    else if (_lastSeenVersion.TryGetValue(id, out var prev))
                    {
                        if (comp.LocalVersion != prev)
                            batch.Updated.Add((id, comp));
                    }
                    else
                        batch.Added.Add((id, comp));
                }

                var toRemove = new List<EntityId>();
                foreach (var id in _removedThisFrame) toRemove.Add(id);
                foreach (var kv in _lastSeenVersion)
                    if (!_items.ContainsKey(kv.Key))
                        toRemove.Add(kv.Key);

                if (toRemove.Count > 1)
                {
                    var uniq = new HashSet<EntityId>(toRemove);
                    toRemove.Clear();
                    foreach (var id in uniq) toRemove.Add(id);
                }

                foreach (var id in toRemove)
                    batch.Removed.Add((id, default!));

                foreach (var id in toRemove)
                    _lastSeenVersion.Remove(id);

                foreach (var kv in _items)
                    _lastSeenVersion[kv.Key] = kv.Value.LocalVersion;

                _addedThisFrame.Clear();
                _removedThisFrame.Clear();
                _replacedThisFrame.Clear();

                if (batch.Added.Count == 0 && batch.Updated.Count == 0 && batch.Removed.Count == 0)
                    return;

                foreach (var sub in _subs) sub(batch);
            }

            private sealed class Unsub : IDisposable
            {
                private readonly List<Action<ChangeBatch<T>>> _subs;
                private readonly Action<ChangeBatch<T>> _cb;
                public Unsub(List<Action<ChangeBatch<T>>> subs, Action<ChangeBatch<T>> cb) { _subs = subs; _cb = cb; }
                public void Dispose() => _subs.Remove(_cb);
            }
        }
    }
}
