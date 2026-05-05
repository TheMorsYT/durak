using System;

namespace Durak.Architecture.Shared.Events
{
    public interface IMatchEventBus
    {
        IDisposable Subscribe<TEvent>(Action<TEvent> handler);
        void Unsubscribe<TEvent>(Action<TEvent> handler);
        void Publish<TEvent>(TEvent eventPayload);
    }
}
