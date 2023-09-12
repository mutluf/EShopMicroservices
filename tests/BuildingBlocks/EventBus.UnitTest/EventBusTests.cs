using EventBus.Base;
using EventBus.Base.Abstraction;
using EventBus.Base.Enums;
using EventBus.Factory;
using EventBus.UnitTest.Events.EventHandlers;
using EventBus.UnitTest.Events.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Xunit;

namespace EventBus.UnitTest
{
    public class EventBusTests
    {

        private ServiceCollection _services;

        public EventBusTests()
        {
            _services = new ServiceCollection();
            _services.AddLogging(configure => configure.AddConsole());
        }

        [Fact]
        public void Subscribe_Event_On_Rabbitmq_Test()
        {
            _services.AddSingleton<IEventBus>(sp =>
               {
                   return EventBusFactory.Create(GetRabbitMQConfig(), sp);
               }); 

            var _serviceProvider = _services.BuildServiceProvider();

            var eventBus = _serviceProvider.GetService<IEventBus>(); 

            eventBus.Subscribe<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>();

            eventBus.Unsubscribe<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>();

        }

        [Fact]
        public void Subscribe_Event_On_AzureServiceBus_Test()
        {

            _services.AddSingleton<IEventBus>(sp =>
            {
                return EventBusFactory.Create(GetAzureConfig(), sp);
            });
           
            var _serviceProvider = _services.BuildServiceProvider();

            var eventBus = _serviceProvider.GetService<IEventBus>();


            eventBus.Subscribe<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>();

            eventBus.Unsubscribe<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>();     
        }

        [Fact]
        public void Send_Message_To_RabbitMQTest()
        {
            _services.AddSingleton<IEventBus>(sp =>
            {
                return EventBusFactory.Create(GetRabbitMQConfig(), sp);
            });

            var _serviceProvider = _services.BuildServiceProvider();
            var eventBus = _serviceProvider.GetService<IEventBus>();

            eventBus.Publish(new OrderCreatedIntegrationEvent(1));
        }

        [Fact]
        public void Send_Message_To_AzureTest()
        {
            _services.AddSingleton<IEventBus>(sp =>
            {
                return EventBusFactory.Create(GetAzureConfig(), sp);
            });

            var _serviceProvider = _services.BuildServiceProvider();
            var eventBus = _serviceProvider.GetService<IEventBus>();

            eventBus.Publish(new OrderCreatedIntegrationEvent(1));
        }


        private EventBusConfiguration GetAzureConfig()
        {
            return new EventBusConfiguration()
            {
                ConnectionRetryCount = 5,
                SubscriberClientAppName = "EventBus.UnitTest",
                DefaultTopicName = "EShopTopicName",
                EventBusType = EventBusType.AzureServiceBus,
                EventNameSuffix = "IntegrationEvent",
                EventBusConnectionString = ""
            };
        }
        private EventBusConfiguration GetRabbitMQConfig()
        {
            return new EventBusConfiguration()
            {
                ConnectionRetryCount = 5,
                SubscriberClientAppName = "EventBus.UnitTest",
                DefaultTopicName = "EShopTopicName",
                EventBusType = EventBusType.RabbitMQ,
                EventNameSuffix = "IntegrationEvent",
                //Connection = new ConnectionFactory() { } //default olarak var
            };
        }
    }
}