using Microsoft.Extensions.Options;
using TaskManagement.Core.Interfaces;
using TaskManagement.Service.Services;
using System.Text.Json;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace TaskManagement.Service;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly IConfiguration _configuration;
    private readonly HealthCheckService _healthCheckService;
    private readonly string _queueName;
    private readonly TimeSpan _checkInterval;

    public Worker(
        ILogger<Worker> logger,
        IServiceProvider serviceProvider,
        IRabbitMQService rabbitMQService,
        IConfiguration configuration,
        HealthCheckService healthCheckService)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _rabbitMQService = rabbitMQService;
        _configuration = configuration;
        _healthCheckService = healthCheckService;
        _queueName = _configuration.GetSection("RabbitMQ").GetValue("TaskQueue", "task_reminders");
        _checkInterval = TimeSpan.FromMinutes(_configuration.GetValue("CheckIntervalMinutes", 5));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Task Reminder Service is starting with queue: {QueueName}", _queueName);

        // Perform initial health check
        await PerformHealthCheck();

        // Start consuming messages from the queue
        await _rabbitMQService.StartConsuming(_queueName, HandleReminderMessage);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogInformation("Checking for overdue tasks at: {Time}", DateTimeOffset.Now);
                
                await CheckOverdueTasks();
                
                // Perform health check periodically
                await PerformHealthCheck();
                
                // Wait for the configured interval
                _logger.LogDebug("Waiting {CheckInterval} before next check", _checkInterval);
                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Service cancellation requested");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while checking for overdue tasks");
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        await _rabbitMQService.StopConsuming();
    }

    private async Task CheckOverdueTasks()
    {
        using var scope = _serviceProvider.CreateScope();
        var taskRepository = scope.ServiceProvider.GetRequiredService<ITaskItemRepository>();

        try
        {
            var overdueTasks = await taskRepository.GetOverdueTasksAsync();
            
            foreach (var task in overdueTasks)
            {
                var reminderMessage = JsonSerializer.Serialize(new
                {
                    TaskId = task.Id,
                    Title = task.Title,
                    DueDate = task.DueDate,
                    FullName = task.FullName,
                    Email = task.Email
                });

                await _rabbitMQService.PublishMessage(_queueName, reminderMessage);
                _logger.LogInformation($"Published reminder for overdue task: {task.Id} - {task.Title}");
            }

            if (overdueTasks.Any())
            {
                _logger.LogInformation($"Found and published {overdueTasks.Count()} overdue task reminders");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking overdue tasks");
        }
    }

    private void HandleReminderMessage(string message)
    {
        try
        {
            var reminder = JsonSerializer.Deserialize<dynamic>(message);
            _logger.LogInformation("REMINDER: Hi your Task is due - {Message}", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling reminder message");
        }
    }

    private async Task PerformHealthCheck()
    {
        try
        {
            _logger.LogDebug("Performing health check");
            var healthCheckResult = await _healthCheckService.CheckHealthAsync();
            
            if (healthCheckResult.Status == HealthStatus.Healthy)
            {
                _logger.LogDebug("All health checks passed");
            }
            else
            {
                var failedChecks = healthCheckResult.Entries
                    .Where(e => e.Value.Status != HealthStatus.Healthy)
                    .Select(e => $"{e.Key}: {e.Value.Status} - {e.Value.Description}")
                    .ToArray();
                
                _logger.LogWarning("Health check failed for: {FailedChecks}", string.Join(", ", failedChecks));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing health check");
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Task Reminder Service is stopping");
        await _rabbitMQService.StopConsuming();
        await base.StopAsync(cancellationToken);
    }
}

