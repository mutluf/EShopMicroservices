using EventBus.Base;
using EventBus.Base.Events;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.ServiceBus.Management;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;

namespace EventBus.AzureServiceBus
{
    public class EventBusServiceBus : BaseEventBus
    {
        private ITopicClient _topicClient;
        private ManagementClient _managementClient;
        private ILogger _logger;
            
        public EventBusServiceBus(IServiceProvider serviceProvider, EventBusConfiguration eventBusConfig) : base(serviceProvider,  eventBusConfig)
        {
            _logger = serviceProvider.GetService(typeof(ILogger<EventBusServiceBus>)) as ILogger<EventBusServiceBus>;
            _topicClient = CreateTopicClient();
            _managementClient = new ManagementClient(eventBusConfig.EventBusConnectionString);
        }


        private ITopicClient CreateTopicClient()
        {
            if(_topicClient == null || _topicClient.IsClosedOrClosing)
            {
                _topicClient = new TopicClient(EventBusConfig.EventBusConnectionString, EventBusConfig.DefaultTopicName, retryPolicy: default);
            }

            if(!_managementClient.TopicExistsAsync(EventBusConfig.DefaultTopicName).GetAwaiter().GetResult())
            {
                _managementClient.CreateTopicAsync(EventBusConfig.DefaultTopicName).GetAwaiter().GetResult();
            }

            return _topicClient;
        }
        public override void Publish(IntegrationEvent @event)
        {
            var eventName= @event.GetType().Name;
            eventName = ProcessEventName(eventName);

            var eventString = JsonConvert.SerializeObject(@event);
            var bodyArray = Encoding.UTF8.GetBytes(eventString);

            var message = new Message()
            {
                MessageId = Guid.NewGuid().ToString(),
                Body = bodyArray,
                Label = eventName,
            };

            _topicClient.SendAsync(message).GetAwaiter().GetResult();
        }

        public override void Subscribe<TEvent, THandler>()
        {
            var eventName =  typeof(TEvent).Name;
            eventName = ProcessEventName(eventName);

            if(!_subscriptionManager.HasSubscriptionForEvent(eventName))
            {
                var subscriptionClient = CreateSubscriptionClientIfNotExist(eventName);
                RegisterSubscriptionClientMessageHandler(subscriptionClient);
            }

            _logger.LogInformation($"Subscribing to event {eventName} with {typeof(THandler).Name}");

            _subscriptionManager.AddSubscription<TEvent,THandler>();
        }

        private void RegisterSubscriptionClientMessageHandler(ISubscriptionClient subscriptionClient)
        {
            subscriptionClient.RegisterMessageHandler(
                async (message, token) =>
                {
                    var eventName = $"{message.Label}";
                    var messageData = Encoding.UTF8.GetString(message.Body);

                    if (await ProcessEvent(ProcessEventName(eventName), messageData))
                    {
                        await subscriptionClient.CompleteAsync(message.SystemProperties.LockToken);
                    }
                },
                new MessageHandlerOptions(ExceptionRecieveHandler) { MaxConcurrentCalls = 10, AutoComplete=false }
                );
        }

        private Task ExceptionRecieveHandler(ExceptionReceivedEventArgs exceptionReceivedEventArgs)
        {
            var ex = exceptionReceivedEventArgs.Exception;
            var context = exceptionReceivedEventArgs.ExceptionReceivedContext;

            _logger.LogError(ex, "ERROR handling message: {ExceptionMessage} - Context: {@ExceptionContext}",ex.Message,context);

            return Task.CompletedTask;
        }

        private ISubscriptionClient CreateSubscriptionClientIfNotExist(string eventName)
        {
            var subClient = CreateSubscriptionClient(eventName);
            var exist = _managementClient.SubscriptionExistsAsync(EventBusConfig.DefaultTopicName, GetSubName(eventName)).GetAwaiter().GetResult();

            if (!exist)
            {
                _managementClient.CreateSubscriptionAsync(EventBusConfig.DefaultTopicName,GetSubName(eventName)).GetAwaiter().GetResult();
                RemoveDefaultRule(subClient);
            }
            CreateRuleIfNotExist(ProcessEventName(eventName),subClient);

            return subClient;
        }
        private void CreateRuleIfNotExist( string eventName, ISubscriptionClient subscriptionClient)
        {
            bool ruleExist;

            try
            {
                var rule = _managementClient.GetRuleAsync(EventBusConfig.DefaultTopicName, eventName, eventName).GetAwaiter().GetResult;
                ruleExist = rule != null;
            }
            catch(MessagingEntityNotFoundException)
            {
                ruleExist = false;
            }

            if(!ruleExist)
            {
                subscriptionClient.AddRuleAsync( new RuleDescription{
                    Filter= new CorrelationFilter { Label = eventName},
                    Name= eventName,
                }).GetAwaiter().GetResult();
            }
        }

        private SubscriptionClient CreateSubscriptionClient( string eventName)
        {
            return new SubscriptionClient(EventBusConfig.EventBusConnectionString, EventBusConfig.DefaultTopicName, GetSubName(eventName));
        }

        private void RemoveDefaultRule(SubscriptionClient subscriptionClient)
        {
            try
            {
                subscriptionClient
                    .RemoveRuleAsync(RuleDescription.DefaultRuleName).GetAwaiter().GetResult();
            }
            catch(MessagingEntityNotFoundException)
            {
                _logger.LogWarning("The messaging entity {DefaultRuleName} colud not be found.", RuleDescription.DefaultRuleName);
            }
        }

        public override void Unsubscribe<TEvent, THandler>()
        {
            var eventName = typeof(TEvent).Name;
            try
            {

                var subscriptionClient = CreateSubscriptionClient(eventName);

                subscriptionClient.RemoveRuleAsync(eventName).GetAwaiter().GetResult();
            }
            catch (MessagingEntityNotFoundException)
            {

                _logger.LogWarning($"The messaging entity {eventName}");
                _subscriptionManager.RemoveSubscription<TEvent,THandler>();
            }
        }


        public override void Dispose()
        {
            base.Dispose();
            _topicClient.CloseAsync().GetAwaiter().GetResult();
            _managementClient.CloseAsync().GetAwaiter().GetResult();

            _topicClient = null;
            _managementClient = null;           
        }
    }
}
