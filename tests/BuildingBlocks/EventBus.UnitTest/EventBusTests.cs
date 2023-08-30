using EventBus.Base;
using EventBus.Base.Abstraction;
using EventBus.Base.Enums;
using EventBus.Factory;
using EventBus.UnitTest.Events.EventHandlers;
using EventBus.UnitTest.Events.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

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

            #region IEventBus
            _services.AddSingleton<IEventBus>(sp =>
               {
                   EventBusConfiguration config = new()
                   {
                       ConnectionRetryCount = 5,
                       SubscriberClientAppName = "EventBus.UnitTest",
                       DefaultTopicName = "EShopTopicName",
                       EventBusType = EventBusType.RabbitMQ,
                       EventNameSuffix = "IntegrationEvent",
                       //Connection = new ConnectionFactory() { } //default olarak var
                   };

                   return EventBusFactory.Create(config, sp);
               }); 
            #endregion

            var _serviceProvider = _services.BuildServiceProvider();

            var eventBus = _serviceProvider.GetService<IEventBus>(); // burada IEventBus' u talep ettiðimizde yukarýdaki IEventBus region'da gerçekleþtirdiðimiz kod bloklarý çalýþacak.


            eventBus.Subscribe<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>();

            eventBus.Unsubscribe<OrderCreatedIntegrationEvent, OrderCreatedIntegrationEventHandler>();

            //Assert.IsType<Sub>(eventBus);
        }
    }
}