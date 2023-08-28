using EventBus.Base.Events;

namespace EventBus.Base.Abstraction
{
    public interface IEventBus
    {
        void Publish(IntegrationEvent @event);
        void Subscribe<TEvent, THandler>() where TEvent : IntegrationEvent where THandler : IIntegrationEventHandler<TEvent>;

        void Unsubscribe<TEvent, THandler>() where TEvent : IntegrationEvent where THandler: IIntegrationEventHandler<TEvent>;
    }
}
