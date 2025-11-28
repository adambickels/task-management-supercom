using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace TaskManagement.Service.Services
{
    public interface IRabbitMQService
    {
        Task PublishMessage(string queueName, string message);
        Task StartConsuming(string queueName, Action<string> messageHandler);
        Task StopConsuming();
        bool IsConnected { get; }
    }

    public class RabbitMQService : IRabbitMQService, IDisposable
    {
        private readonly ILogger<RabbitMQService> _logger;
        private readonly IConfiguration _configuration;
        private IConnection? _connection;
        private IChannel? _channel;
        private AsyncEventingBasicConsumer? _consumer;
        private string? _consumerTag;
        private bool _disposed = false;

        public bool IsConnected => _connection?.IsOpen == true && _channel?.IsOpen == true;

        public RabbitMQService(ILogger<RabbitMQService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            InitializeRabbitMQ().GetAwaiter().GetResult();
        }

        private async Task InitializeRabbitMQ()
        {
            try
            {
                _logger.LogInformation("Initializing RabbitMQ connection");
                
                var rabbitMQSection = _configuration.GetSection("RabbitMQ");
                var factory = new ConnectionFactory
                {
                    HostName = rabbitMQSection.GetValue("HostName", "localhost"),
                    UserName = rabbitMQSection.GetValue("UserName", "guest"),
                    Password = rabbitMQSection.GetValue("Password", "guest"),
                    VirtualHost = rabbitMQSection.GetValue("VirtualHost", "/"),
                    Port = rabbitMQSection.GetValue("Port", 5672),
                    RequestedHeartbeat = TimeSpan.FromSeconds(60),
                    NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                    AutomaticRecoveryEnabled = true,
                    TopologyRecoveryEnabled = true
                };

                _logger.LogInformation("Connecting to RabbitMQ at {HostName}:{Port}", factory.HostName, factory.Port);
                
                _connection = await factory.CreateConnectionAsync();
                _channel = await _connection.CreateChannelAsync();
                
                _logger.LogInformation("Successfully connected to RabbitMQ");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to connect to RabbitMQ. Service will continue without message queue support. Please ensure RabbitMQ is running and accessible.");
                // Don't throw - allow service to continue without RabbitMQ
            }
        }

        public async Task PublishMessage(string queueName, string message)
        {
            try
            {
                if (_channel == null || !IsConnected)
                {
                    _logger.LogWarning("RabbitMQ channel is not initialized or connection is lost. Attempting to reconnect...");
                    await InitializeRabbitMQ();
                    
                    if (_channel == null)
                    {
                        _logger.LogWarning("RabbitMQ reconnection failed. Message not sent: {Message}", message);
                        return;
                    }
                }

                await _channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                var body = Encoding.UTF8.GetBytes(message);
                var properties = new BasicProperties
                {
                    Persistent = true,
                    Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                };

                await _channel.BasicPublishAsync(
                    exchange: string.Empty,
                    routingKey: queueName,
                    mandatory: false,
                    basicProperties: properties,
                    body: body);

                _logger.LogInformation("Successfully published message to queue {QueueName}: {Message}", queueName, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message to queue {QueueName}: {Message}", queueName, message);
            }
        }

        public async Task StartConsuming(string queueName, Action<string> messageHandler)
        {
            try
            {
                if (_channel == null || !IsConnected)
                {
                    _logger.LogError("RabbitMQ channel is not initialized or connection is lost. Cannot start consuming from queue {QueueName}", queueName);
                    return;
                }

                await _channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                _consumer = new AsyncEventingBasicConsumer(_channel);
                _consumer.ReceivedAsync += async (model, ea) =>
                {
                    try
                    {
                        var body = ea.Body.ToArray();
                        var message = Encoding.UTF8.GetString(body);
                        _logger.LogInformation("Received message from queue {QueueName}: {Message}", queueName, message);
                        
                        messageHandler(message);
                        
                        await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                        _logger.LogDebug("Acknowledged message from queue {QueueName}", queueName);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error processing message from queue {QueueName}", queueName);
                        // Consider implementing retry logic or dead letter queue here
                        await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                    }
                };

                _consumerTag = await _channel.BasicConsumeAsync(
                    queue: queueName,
                    autoAck: false,
                    consumer: _consumer);

                _logger.LogInformation("Started consuming messages from queue {QueueName} with consumer tag: {ConsumerTag}", queueName, _consumerTag);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting to consume from queue {QueueName}", queueName);
            }
        }

        public async Task StopConsuming()
        {
            try
            {
                if (_channel != null && _consumerTag != null)
                {
                    await _channel.BasicCancelAsync(_consumerTag);
                    _logger.LogInformation("Stopped consuming messages with consumer tag: {ConsumerTag}", _consumerTag);
                    _consumerTag = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error stopping consumer");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    StopConsuming().GetAwaiter().GetResult();
                    _channel?.Dispose();
                    _connection?.Dispose();
                    _logger.LogInformation("RabbitMQ resources disposed");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error disposing RabbitMQ resources");
                }
                _disposed = true;
            }
        }
    }
}

