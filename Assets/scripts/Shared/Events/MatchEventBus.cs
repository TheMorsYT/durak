using System;
using System.Collections.Generic;

namespace Durak.Architecture.Shared.Events
{
    public sealed class MatchEventBus : IMatchEventBus
    {
        private readonly Dictionary<Type, Delegate> handlersByType = new Dictionary<Type, Delegate>();

        public IDisposable Subscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null)
            {
                throw new ArgumentNullException(nameof(handler));
            }

            Type eventType = typeof(TEvent);
            handlersByType.TryGetValue(eventType, out Delegate currentHandlers);
            handlersByType[eventType] = Delegate.Combine(currentHandlers, handler);

            return new SubscriptionToken<TEvent>(this, handler);
        }

        public void Unsubscribe<TEvent>(Action<TEvent> handler)
        {
            if (handler == null)
            {
                return;
            }

            Type eventType = typeof(TEvent);
            if (!handlersByType.TryGetValue(eventType, out Delegate currentHandlers))
            {
                return;
            }

            Delegate nextHandlers = Delegate.Remove(currentHandlers, handler);
            if (nextHandlers == null)
            {
                handlersByType.Remove(eventType);
                return;
            }

            handlersByType[eventType] = nextHandlers;
        }

        public void Publish<TEvent>(TEvent eventPayload)
        {
            if (!handlersByType.TryGetValue(typeof(TEvent), out Delegate rawHandlers))
            {
                return;
            }

            if (rawHandlers is Action<TEvent> typedHandlers)
            {
                typedHandlers.Invoke(eventPayload);
            }
        }

        private sealed class SubscriptionToken<TEvent> : IDisposable
        {
            private readonly MatchEventBus bus;
            private Action<TEvent> handler;

            public SubscriptionToken(MatchEventBus bus, Action<TEvent> handler)
            {
                this.bus = bus;
                this.handler = handler;
            }

            public void Dispose()
            {
                if (handler == null)
                {
                    return;
                }

                bus.Unsubscribe(handler);
                handler = null;
            }
        }
    }
}
