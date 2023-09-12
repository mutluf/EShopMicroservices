using EventBus.Base;
using EventBus.Base.Abstraction;
using EventBus.Base.Events;
using Newtonsoft.Json;
using Polly;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System.Net.Sockets;
using System.Text;

namespace EventBus.RabbitMQ
{
    public class EventBusRabbitMQ : BaseEventBus
    {
        RabbitMQPersistenceConnection _rabbitConnection;
        private readonly IConnectionFactory _connectionFactory;
        private readonly IModel consumerChannel;
        public EventBusRabbitMQ(IServiceProvider serviceProvider, EventBusConfiguration eventBusConfig) : base(serviceProvider, eventBusConfig)
        {
            if(eventBusConfig.Connection != null)
            {
                var connectionJson = JsonConvert.SerializeObject(eventBusConfig.Connection, new JsonSerializerSettings()
                {
                    // self referencing loop detected for property
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                });               
            }

            else
            {
                _connectionFactory = new ConnectionFactory();
            }

            _rabbitConnection = new RabbitMQPersistenceConnection(_connectionFactory, eventBusConfig.ConnectionRetryCount);

            consumerChannel = CreateConsumerChannel();
        }

        public override void Publish(IntegrationEvent @event)
        {
            if (!_rabbitConnection.IsConnected)
            {
                _rabbitConnection.TryConnect();
            }

            var policy = Policy.Handle<BrokerUnreachableException>()
                .Or<SocketException>()
                .WaitAndRetry(EventBusConfig.ConnectionRetryCount, retryAttempt =>               
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                    {
                    }
                );


            var eventName = @event.GetType().Name;
            eventName= ProcessEventName(eventName);

            consumerChannel.ExchangeDeclare(exchange: EventBusConfig.DefaultTopicName, type: "direct");

            var message = JsonConvert.SerializeObject(@event);
            var body = Encoding.UTF8.GetBytes(message);

            policy.Execute(() =>
            {
                var properties = consumerChannel.CreateBasicProperties();
                properties.DeliveryMode = 2;

                consumerChannel.QueueDeclare(queue: GetSubName(eventName),
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);


                consumerChannel.BasicPublish(
                    exchange: EventBusConfig.DefaultTopicName,
                    routingKey: eventName,
                    mandatory: true,
                    basicProperties: properties,
                    body: body);
            });
        }

        public override void Subscribe<TEvent, THandler>()
        {
            var eventName = typeof(TEvent).Name;    
            eventName = ProcessEventName(eventName);
            
            if(!_subscriptionManager.HasSubscriptionForEvent(eventName))
            {
                if(!_rabbitConnection.IsConnected)
                {
                    _rabbitConnection.TryConnect();
                }

                consumerChannel.QueueDeclare(queue: GetSubName(eventName), //ensure queue exists while consuming
                    durable:true,
                    exclusive:false,
                    autoDelete:false,
                    arguments:null);

                consumerChannel.QueueBind(queue: GetSubName(eventName),
                    exchange: EventBusConfig.DefaultTopicName,
                    routingKey: eventName);
            }

            _subscriptionManager.AddSubscription<TEvent, THandler>();
            StartBasicConsume(eventName);
        }

        public override void Unsubscribe<TEvent, THandler>()
        {
            _subscriptionManager.RemoveSubscription<TEvent, THandler>();
            _subscriptionManager.OnEventRemoved += _subscriptionManager_OnEventRemoved;
        }

        private void _subscriptionManager_OnEventRemoved(object? sender, string eventName)
        {
            eventName = ProcessEventName(eventName);

            if (!_rabbitConnection.IsConnected)
            {
                _rabbitConnection.TryConnect();
            }

            consumerChannel.QueueUnbind(queue: eventName,
                exchange:EventBusConfig.DefaultTopicName,
                routingKey: eventName);

            if (_subscriptionManager.IsEmpty)
            {
                consumerChannel.Close();
            }
        }

        private IModel CreateConsumerChannel()
        {
            if (!_rabbitConnection.IsConnected)
            {
                _rabbitConnection.TryConnect();
            }

            var channel = _rabbitConnection.CreateModel();
            channel.ExchangeDeclare(exchange: EventBusConfig.DefaultTopicName, type: "direct");

            return channel;
        }

        private void StartBasicConsume(string eventName)
        {
            if(consumerChannel != null)
            {
                var consumer = new EventingBasicConsumer(consumerChannel);

                consumer.Received += Consumer_Received;

                consumerChannel.BasicConsume(
                    queue:GetSubName(eventName),
                    autoAck:false,
                    consumer:consumer);
            }
        }

        private async void Consumer_Received(object sender, BasicDeliverEventArgs eventArgs)
        {
            var eventName = eventArgs.RoutingKey;
            eventName= ProcessEventName(eventName);

            var message = Encoding.UTF8.GetString(eventArgs.Body.Span);

            try
            {
                await ProcessEvent(eventName, message);
            }
            catch(Exception ex)
            {
                //log
                consumerChannel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            }
        }
    }
}
