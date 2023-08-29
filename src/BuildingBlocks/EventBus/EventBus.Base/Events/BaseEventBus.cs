using EventBus.Base.Abstraction;
using EventBus.Base.SubManagers;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace EventBus.Base.Events
{
    public abstract class BaseEventBus : IEventBus
    {
        public readonly IServiceProvider _serviceProvider;
        public readonly IEventBusSubscriptionManager _subscriptionManager;

        private EventBusConfiguration? _eventBusConfig;

        protected BaseEventBus(IServiceProvider serviceProvider, IEventBusSubscriptionManager subscriptionManager, EventBusConfiguration eventBusConfig)
        {
            _serviceProvider = serviceProvider;
            _subscriptionManager = new InMemoryEventBusSubscriptionManager(ProcessEventName);
            _eventBusConfig = eventBusConfig;
        }

        public virtual string ProcessEventName( string eventName)
        {
            if (_eventBusConfig.DeleteEventPrefix)
                eventName = eventName.TrimStart(_eventBusConfig.EventNamePrefix.ToArray());

            if(_eventBusConfig.DeleteEventSuffix)
                eventName = eventName.TrimEnd(_eventBusConfig.EventNameSuffix.ToArray());

            return eventName;
        }

        public virtual string GetSubName(string eventName)
        {
            return $"{_eventBusConfig.SubscriberClientAppName}.{ProcessEventName(eventName)}";
        }

        public virtual void Dispose() => _eventBusConfig = null;


        public async Task<bool> ProcessEvent(string eventName, string message)
        {
            eventName = ProcessEventName(eventName);
            bool processed = false;

            if(_subscriptionManager.HasSubscriptionForEvent(eventName))
            {
                var subscriptions = _subscriptionManager.GetHandlersForEvent(eventName);

                using (var scope = _serviceProvider.CreateScope())
                {
                    foreach (var subscription in subscriptions)
                    {
                        var handler = _serviceProvider.GetService(subscription.HandlerType);
                        if(handler == null) continue;

                        var eventType = _subscriptionManager.GetEventTypeByName($"{_eventBusConfig.EventNamePrefix}{eventName}{_eventBusConfig.EventNameSuffix}");
                        var integrationEvent = JsonConvert.DeserializeObject(message, eventType);

                        var concreteType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                        await (Task)concreteType.GetMethod("Handle").Invoke(handler, new[] { integrationEvent });
                    }
                }
                processed = true;
            }

            return processed;
        }

        public abstract void Publish(IntegrationEvent @event);


        public abstract void Subscribe<TEvent, THandler>() where TEvent : IntegrationEvent where THandler : IIntegrationEventHandler<TEvent>;

        public abstract void Unsubscribe<TEvent, THandler>() where TEvent : IntegrationEvent where THandler : IIntegrationEventHandler<TEvent>;
    }
}
