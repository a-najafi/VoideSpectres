using System;
using System.Collections.Generic;

namespace VoidSpectre.Core.Priority
{
    public sealed class PriorityRegistry<T>
    {
        private readonly List<T> _items = new();
        private readonly Func<T, int> _prio;

        public PriorityRegistry(Func<T, int> prioritySelector) => _prio = prioritySelector;

        public void Add(T item)
        {
            _items.Add(item);
            _items.Sort((a, b) => _prio(a).CompareTo(_prio(b)));
        }

        public IEnumerable<T> GetAll() => _items;
    }
}
