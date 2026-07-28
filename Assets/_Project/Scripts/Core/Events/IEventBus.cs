using System;

namespace CrateExpectations.Core.Events
{
    /// <summary>Типизированная шина событий для развязки систем (ContractCompleted, CargoInspected и т.д.)</summary>
    public interface IEventBus
    {
        void Subscribe<T>(Action<T> handler);
        void Unsubscribe<T>(Action<T> handler);
        void Publish<T>(T @event);
    }
}
