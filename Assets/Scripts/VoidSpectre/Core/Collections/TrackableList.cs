using System;
using System.Collections;
using System.Collections.Generic;
using Sirenix.Serialization;
using VoidSpectre.Core.Interfaces;

namespace VoidSpectre.Core.Collections
{
    [Serializable]
    public sealed class TrackableList<T> : ITrackableNode, IList<T>
    {
        [OdinSerialize] private List<T> _list = new();
        public event Action Changed;

        private void Ping() => Changed?.Invoke();

        private static void TryAttach(T item, Action handler)
        {
            if (item is ITrackableNode t) t.Changed += handler;
        }

        private static void TryDetach(T item, Action handler)
        {
            if (item is ITrackableNode t) t.Changed -= handler;
        }

        private void OnChildChanged() => Ping();

        private void AttachAll()
        {
            for (int i = 0; i < _list.Count; i++)
                TryAttach(_list[i], OnChildChanged);
        }

        private void DetachAll()
        {
            for (int i = 0; i < _list.Count; i++)
                TryDetach(_list[i], OnChildChanged);
        }

        public T this[int index]
        {
            get => _list[index];
            set
            {
                if (index >= 0 && index < _list.Count)
                    TryDetach(_list[index], OnChildChanged);
                _list[index] = value;
                TryAttach(value, OnChildChanged);
                Ping();
            }
        }

        public int Count => _list.Count;
        public bool IsReadOnly => false;

        public void Add(T item)
        {
            _list.Add(item);
            TryAttach(item, OnChildChanged);
            Ping();
        }

        public void Clear()
        {
            if (_list.Count == 0) return;
            DetachAll();
            _list.Clear();
            Ping();
        }

        public bool Contains(T item) => _list.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => _list.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => _list.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => _list.GetEnumerator();
        public int IndexOf(T item) => _list.IndexOf(item);

        public void Insert(int index, T item)
        {
            _list.Insert(index, item);
            TryAttach(item, OnChildChanged);
            Ping();
        }

        public bool Remove(T item)
        {
            var idx = _list.IndexOf(item);
            if (idx < 0) return false;
            TryDetach(_list[idx], OnChildChanged);
            _list.RemoveAt(idx);
            Ping();
            return true;
        }

        public void RemoveAt(int index)
        {
            TryDetach(_list[index], OnChildChanged);
            _list.RemoveAt(index);
            Ping();
        }
    }
}
