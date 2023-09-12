using EventBus.AzureServiceBus;
using EventBus.Base;
using EventBus.Base.Abstraction;
using EventBus.Base.Enums;
using EventBus.RabbitMQ;

namespace EventBus.Factory
{
    public class EventBusFactory
    {
        public static IEventBus Create(EventBusConfiguration configuration, IServiceProvider serviceProvider)
        {
            return configuration.EventBusType switch
            {
                EventBusType.AzureServiceBus => new EventBusServiceBus( serviceProvider, configuration),
                _ => new EventBusRabbitMQ(eventBusConfig:configuration, serviceProvider: serviceProvider),
            };
        }
    }
}
