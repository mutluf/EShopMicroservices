using EventBus.Base;
using EventBus.Base.Abstraction;
using EventBus.Base.Events;

namespace EventBus.RabbitMQ
{
    public class EventBusRabbitMQ : BaseEventBus
    {
        public EventBusRabbitMQ(IServiceProvider serviceProvider, IEventBusSubscriptionManager subscriptionManager, EventBusConfiguration eventBusConfig) : base(serviceProvider, subscriptionManager, eventBusConfig)
        {
        }

        public override void Publish(IntegrationEvent @event)
        {
            throw new NotImplementedException();
        }

        public override void Subscribe<TEvent, THandler>()
        {
            var eventName = typeof(TEvent).Name;    
            eventName = ProcessEventName(eventName);
            
            if(!_subscriptionManager.HasSubscriptionForEvent(eventName))
            {

            }
        }

        public override void Unsubscribe<TEvent, THandler>()
        {
            throw new NotImplementedException();
        }
    }
}
