using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using VoidSpectre.Core.Diagnostics;

namespace VoidSpectre.Core
{
    public static class EventAwaiter
    {
        public static Task<T> Next<T>(EventStream events, Func<T, bool> predicate = null, CancellationToken ct = default)
            where T : struct
        {
            var tcs = new TaskCompletionSource<T>();
            IDisposable sub = null;

            sub = events.Subscribe<T>(e =>
            {
                if (ct.IsCancellationRequested) return;
                if (predicate == null || predicate(e))
                {
                    sub.Dispose();
                    tcs.TrySetResult(e);
                }
            });

            if (ct.CanBeCanceled)
                ct.Register(() =>
                {
                    sub.Dispose();
                    tcs.TrySetCanceled();
                });

            return tcs.Task;
        }
    }

    public sealed class EventStream
    {
        private readonly Dictionary<Type, IEventQueue> _queues = new();

        public void Enqueue<T>(T e) where T : struct => GetQueue<T>().Enqueue(e);

        public IDisposable Subscribe<T>(Action<T> callback) where T : struct =>
            GetQueue<T>().Subscribe(callback);

        public void BeginTick() { }

        public void EndTick()
        {
            const int safetyMaxPasses = 64;
            int passes = 0;

            while (true)
            {
                passes++;
                var snapshot = _queues.Values.ToArray();
                foreach (var q in snapshot) q.Drain();

                if (!_queues.Values.Any(q => q.HasPending)) break;

                if (passes >= safetyMaxPasses)
                {
                    VsLog.Warning(
                        $"EventStream.EndTick: exceeded {safetyMaxPasses} drain passes. Possible infinite cascade.");
                    break;
                }
            }
        }

        private EventQueue<T> GetQueue<T>() where T : struct
        {
            var type = typeof(T);
            if (!_queues.TryGetValue(type, out var queue))
            {
                queue = new EventQueue<T>();
                _queues[type] = queue;
            }

            return (EventQueue<T>)queue;
        }

        private interface IEventQueue
        {
            void Drain();
            bool HasPending { get; }
        }

        private sealed class EventQueue<T> : IEventQueue where T : struct
        {
            private readonly Queue<T> _eventQueue = new(8);
            private readonly List<Action<T>> _subscribers = new();

            public void Enqueue(T evt) => _eventQueue.Enqueue(evt);

            public IDisposable Subscribe(Action<T> callback)
            {
                _subscribers.Add(callback);
                return new Unsub<T>(_subscribers, callback);
            }

            public bool HasPending => _eventQueue.Count > 0;

            public void Drain()
            {
                if (_subscribers.Count == 0)
                {
                    _eventQueue.Clear();
                    return;
                }

                var subs = _subscribers.ToArray();
                while (_eventQueue.Count > 0)
                {
                    var e = _eventQueue.Dequeue();
                    for (int i = 0; i < subs.Length; i++)
                    {
                        try { subs[i](e); }
                        catch (Exception ex) { VsLog.Exception(ex); }
                    }
                }
            }
        }

        private sealed class Unsub<T> : IDisposable
        {
            private readonly List<Action<T>> _list;
            private readonly Action<T> _cb;
            public Unsub(List<Action<T>> list, Action<T> cb) { _list = list; _cb = cb; }
            public void Dispose() => _list.Remove(_cb);
        }
    }
}
