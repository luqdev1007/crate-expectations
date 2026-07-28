using System;
using System.Collections.Generic;

namespace CrateExpectations.Core.Events
{
    public sealed class EventBus : IEventBus
    {
        private readonly Dictionary<Type, Delegate> _handlers = new();

        public void Subscribe<T>(Action<T> handler)
        {
            var t = typeof(T);
            _handlers[t] = _handlers.TryGetValue(t, out var d) ? Delegate.Combine(d, handler) : handler;
        }

        public void Unsubscribe<T>(Action<T> handler)
        {
            var t = typeof(T);
            if (!_handlers.TryGetValue(t, out var d)) return;
            var updated = Delegate.Remove(d, handler);
            if (updated == null) _handlers.Remove(t);
            else _handlers[t] = updated;
        }

        public void Publish<T>(T @event)
        {
            if (_handlers.TryGetValue(typeof(T), out var d))
                ((Action<T>)d)?.Invoke(@event);
        }
    }
}
