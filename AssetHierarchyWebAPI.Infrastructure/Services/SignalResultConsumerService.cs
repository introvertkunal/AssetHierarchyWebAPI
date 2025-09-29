using AssetHierarchyWebAPI.Application.DTOs;
using AssetHierarchyWebAPI.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using AssetHierarchyWebAPI.Infrastructure.RabbitMQConfig;
using System.Text.Json;


namespace AssetHierarchyWebAPI.Services
{
    public class SignalResultConsumerService : BackgroundService
    {
        private readonly INotificationService _notificationService;
        private readonly RabbitMQSettings _settings;
        private readonly ILogger<SignalResultConsumerService> _logger;
        private IConnection? _connection;
        private RabbitMQ.Client.IModel? _channel;
        

        public SignalResultConsumerService(
            INotificationService notificationService,
            ILogger<SignalResultConsumerService> logger,
            RabbitMQSettings settings
            )
        {
            _notificationService = notificationService;
            _logger = logger;
            _settings = settings;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {

            var QueueName = _settings.ResultQueue;

            var factory = new ConnectionFactory() 
            {
                HostName = _settings.HostName,
                UserName = _settings.UserName,
                Password = _settings.Password,
                DispatchConsumersAsync = true 
            };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(queue: QueueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.Received += async (model, ea) =>
            {
                var body = ea.Body.ToArray();
                var messageJson = Encoding.UTF8.GetString(body);

                var result = JsonSerializer.Deserialize<MessageDto>(messageJson);
                if (result != null)
                {
                  
                    _logger.LogInformation("Received result: {Message} for {UserName}", result.Message, result.UserName);
                    await _notificationService.SendToUserAsync(result.UserName, result.Message);
                }

                _channel.BasicAck(ea.DeliveryTag, false);
            };

            _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);

            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping SignalResultConsumerService...");
            _channel?.Close();
            _connection?.Close();
            return base.StopAsync(cancellationToken);
        }
    }
}
