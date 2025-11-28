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

                // Declare dead letter exchange
                await _channel.ExchangeDeclareAsync(
                    exchange: "task_reminders_dlx",
                    type: "direct",
                    durable: true,
                    autoDelete: false);

                // Declare dead letter queue
                await _channel.QueueDeclareAsync(
                    queue: "task_reminders_dlq",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                // Bind dead letter queue to dead letter exchange
                await _channel.QueueBindAsync(
                    queue: "task_reminders_dlq",
                    exchange: "task_reminders_dlx",
                    routingKey: queueName);

                // Declare main queue with dead letter exchange configuration
                var args = new Dictionary<string, object?>
                {
                    { "x-dead-letter-exchange", "task_reminders_dlx" },
                    { "x-dead-letter-routing-key", queueName }
                };

                await _channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: args);

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

                // Declare dead letter exchange
                await _channel.ExchangeDeclareAsync(
                    exchange: "task_reminders_dlx",
                    type: "direct",
                    durable: true,
                    autoDelete: false);

                // Declare dead letter queue
                await _channel.QueueDeclareAsync(
                    queue: "task_reminders_dlq",
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: null);

                // Bind dead letter queue to dead letter exchange
                await _channel.QueueBindAsync(
                    queue: "task_reminders_dlq",
                    exchange: "task_reminders_dlx",
                    routingKey: queueName);

                // Declare main queue with dead letter exchange configuration
                var args = new Dictionary<string, object?>
                {
                    { "x-dead-letter-exchange", "task_reminders_dlx" },
                    { "x-dead-letter-routing-key", queueName }
                };

                await _channel.QueueDeclareAsync(
                    queue: queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false,
                    arguments: args);

                _consumer = new AsyncEventingBasicConsumer(_channel);
                _consumer.ReceivedAsync += async (model, ea) =>
                {
                    const int maxRetryAttempts = 3;
                    var retryCount = 0;
                    
                    if (ea.BasicProperties.Headers != null && ea.BasicProperties.Headers.ContainsKey("x-retry-count"))
                    {
                        retryCount = Convert.ToInt32(ea.BasicProperties.Headers["x-retry-count"]);
                    }

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
                        _logger.LogError(ex, "Error processing message from queue {QueueName}. Retry attempt {RetryCount}/{MaxRetries}", 
                            queueName, retryCount, maxRetryAttempts);

                        if (retryCount < maxRetryAttempts)
                        {
                            // Retry with exponential backoff
                            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryCount)));
                            
                            // Re-publish with incremented retry count
                            var newProperties = new BasicProperties
                            {
                                Persistent = true,
                                Headers = new Dictionary<string, object?>
                                {
                                    { "x-retry-count", retryCount + 1 }
                                }
                            };

                            await _channel.BasicPublishAsync(
                                exchange: string.Empty,
                                routingKey: queueName,
                                mandatory: false,
                                basicProperties: newProperties,
                                body: ea.Body);

                            await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false);
                            _logger.LogWarning("Message re-queued for retry {RetryCount}/{MaxRetries}", retryCount + 1, maxRetryAttempts);
                        }
                        else
                        {
                            // Max retries exceeded - send to dead letter queue
                            await _channel.BasicNackAsync(deliveryTag: ea.DeliveryTag, multiple: false, requeue: false);
                            _logger.LogError("Message sent to dead letter queue after {MaxRetries} failed attempts", maxRetryAttempts);
                        }
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

