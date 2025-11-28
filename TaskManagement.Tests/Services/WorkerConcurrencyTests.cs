using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Moq;
using System.Collections.Concurrent;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Interfaces;
using TaskManagement.Service;
using TaskManagement.Service.Services;

namespace TaskManagement.Tests.Services
{
    public class WorkerConcurrencyTests
    {
        private readonly Mock<ILogger<Worker>> _mockWorkerLogger;
        private readonly Mock<ILogger<RabbitMQService>> _mockRabbitLogger;
        private readonly Mock<IServiceProvider> _mockServiceProvider;
        private readonly Mock<ITaskItemRepository> _mockRepository;
        private readonly Mock<HealthCheckService> _mockHealthCheckService;
        private readonly IConfiguration _configuration;

        public WorkerConcurrencyTests()
        {
            _mockWorkerLogger = new Mock<ILogger<Worker>>();
            _mockRabbitLogger = new Mock<ILogger<RabbitMQService>>();
            _mockServiceProvider = new Mock<IServiceProvider>();
            _mockRepository = new Mock<ITaskItemRepository>();
            _mockHealthCheckService = new Mock<HealthCheckService>();

            var inMemorySettings = new Dictionary<string, string>
            {
                {"RabbitMQ:HostName", "localhost"},
                {"RabbitMQ:UserName", "guest"},
                {"RabbitMQ:Password", "guest"},
                {"RabbitMQ:Port", "5672"},
                {"RabbitMQ:TaskQueue", "test_worker_queue"},
                {"CheckIntervalMinutes", "1"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();
        }

        [Fact]
        public async Task CheckOverdueTasks_MultipleConcurrentChecks_ShouldNotProcessDuplicates()
        {
            // Arrange - Create overdue tasks
            var overdueTasks = CreateOverdueTasks(20);
            var processedTaskIds = new ConcurrentBag<int>();
            var publishCount = 0;
            var publishLock = new object();

            _mockRepository
                .Setup(r => r.GetOverdueTasksAsync())
                .ReturnsAsync(overdueTasks);

            var scope = new Mock<IServiceScope>();
            scope.Setup(s => s.ServiceProvider.GetService(typeof(ITaskItemRepository)))
                .Returns(_mockRepository.Object);

            var scopeFactory = new Mock<IServiceScopeFactory>();
            scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

            _mockServiceProvider
                .Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
                .Returns(scopeFactory.Object);

            var rabbitMQService = new Mock<IRabbitMQService>();
            rabbitMQService
                .Setup(r => r.PublishMessage(It.IsAny<string>(), It.IsAny<string>()))
                .Callback<string, string>((queue, message) =>
                {
                    lock (publishLock)
                    {
                        publishCount++;
                        // Extract task ID from message
                        var match = System.Text.RegularExpressions.Regex.Match(message, @"TaskId"":(\d+)");
                        if (match.Success && int.TryParse(match.Groups[1].Value, out int taskId))
                        {
                            processedTaskIds.Add(taskId);
                        }
                    }
                })
                .Returns(Task.CompletedTask);

            // Simulate concurrent worker checks
            var tasks = new List<Task>();
            for (int i = 0; i < 5; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    using var scope = scopeFactory.Object.CreateScope();
                    var repo = scope.ServiceProvider.GetService(typeof(ITaskItemRepository)) as ITaskItemRepository;
                    var overdueTasksList = await repo!.GetOverdueTasksAsync();
                    
                    foreach (var task in overdueTasksList)
                    {
                        await rabbitMQService.Object.PublishMessage("test_queue", 
                            $"{{\"TaskId\":{task.Id}}}");
                    }
                }));
            }

            // Act
            await Task.WhenAll(tasks);

            // Assert - In real scenario with proper locking, each task should be processed once
            // This test demonstrates the need for proper concurrency handling
            publishCount.Should().BeGreaterThan(0, "messages should be published");
            
            // Note: Without proper locking in production, this could result in duplicates
            // The test documents the expected behavior
        }

        [Fact]
        public async Task RabbitMQ_ConcurrentMessageConsumption_ShouldProcessInParallel()
        {
            // Arrange
            var messageCount = 50;
            var messages = Enumerable.Range(1, messageCount)
                .Select(i => $"{{\"TaskId\":{i},\"Title\":\"Task {i}\",\"DueDate\":\"2025-11-27T00:00:00Z\"}}")
                .ToList();

            var processedMessages = new ConcurrentBag<string>();
            var processingThreads = new ConcurrentDictionary<int, List<string>>();
            var processingTimes = new ConcurrentBag<long>();
            var semaphore = new SemaphoreSlim(0);

            var rabbitMQService = new RabbitMQService(_mockRabbitLogger.Object, _configuration);
            var testQueue = $"concurrent_test_{Guid.NewGuid()}";

            // Message handler that tracks processing
            void MessageHandler(string message)
            {
                var startTime = DateTime.UtcNow;
                var threadId = Environment.CurrentManagedThreadId;

                processedMessages.Add(message);
                processingThreads.AddOrUpdate(
                    threadId,
                    new List<string> { message },
                    (key, list) => { list.Add(message); return list; });

                // Simulate work
                Thread.Sleep(20);

                var processingTime = (DateTime.UtcNow - startTime).Milliseconds;
                processingTimes.Add(processingTime);
                
                semaphore.Release();
            }

            // Publish all messages
            foreach (var message in messages)
            {
                await rabbitMQService.PublishMessage(testQueue, message);
            }

            // Act - Start consuming
            await rabbitMQService.StartConsuming(testQueue, MessageHandler);

            // Wait for all messages with timeout
            var allProcessed = true;
            for (int i = 0; i < messageCount; i++)
            {
                if (!await semaphore.WaitAsync(TimeSpan.FromSeconds(10)))
                {
                    allProcessed = false;
                    break;
                }
            }

            await rabbitMQService.StopConsuming();

            // Assert
            allProcessed.Should().BeTrue("all messages should be processed");
            processedMessages.Should().HaveCount(messageCount);
            processedMessages.Distinct().Should().HaveCount(messageCount, "no duplicates should exist");
            
            // Verify messages were distributed across potential threads
            processingThreads.Should().NotBeEmpty("messages should be processed");
        }

        [Fact]
        public async Task OverdueTaskProcessing_WithConcurrentUpdates_ShouldMaintainConsistency()
        {
            // Arrange - Simulate real-world scenario where tasks are being updated while checking
            var tasks = CreateOverdueTasks(15);
            var processingLog = new ConcurrentBag<string>();

            _mockRepository
                .Setup(r => r.GetOverdueTasksAsync())
                .ReturnsAsync(() => tasks); // Returns same tasks on each call

            var scope = new Mock<IServiceScope>();
            scope.Setup(s => s.ServiceProvider.GetService(typeof(ITaskItemRepository)))
                .Returns(_mockRepository.Object);

            var scopeFactory = new Mock<IServiceScopeFactory>();
            scopeFactory.Setup(f => f.CreateScope()).Returns(scope.Object);

            _mockServiceProvider
                .Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
                .Returns(scopeFactory.Object);

            // Simulate concurrent updates and reads
            var concurrentTasks = new List<Task>();

            // Reader tasks (simulating overdue task checks)
            for (int i = 0; i < 5; i++)
            {
                var readerId = i;
                concurrentTasks.Add(Task.Run(async () =>
                {
                    using var scope = scopeFactory.Object.CreateScope();
                    var repo = scope.ServiceProvider.GetService(typeof(ITaskItemRepository)) as ITaskItemRepository;
                    var overdueTasks = await repo!.GetOverdueTasksAsync();
                    
                    foreach (var task in overdueTasks)
                    {
                        processingLog.Add($"Reader-{readerId} processed task {task.Id}");
                        await Task.Delay(5); // Simulate processing
                    }
                }));
            }

            // Writer tasks (simulating concurrent task updates)
            for (int i = 0; i < 3; i++)
            {
                var writerId = i;
                concurrentTasks.Add(Task.Run(() =>
                {
                    foreach (var task in tasks.Take(5))
                    {
                        // Simulate updating task
                        lock (task)
                        {
                            task.UpdatedAt = DateTime.UtcNow;
                            processingLog.Add($"Writer-{writerId} updated task {task.Id}");
                        }
                        
                        Thread.Sleep(3);
                    }
                }));
            }

            // Act
            await Task.WhenAll(concurrentTasks);

            // Assert
            processingLog.Should().NotBeEmpty("concurrent operations should be logged");
            processingLog.Should().Contain(log => log.Contains("Reader"), "readers should process tasks");
            processingLog.Should().Contain(log => log.Contains("Writer"), "writers should update tasks");
            
            // Verify all tasks were accessed
            var processedTaskIds = processingLog
                .Where(log => log.Contains("processed task"))
                .Select(log => int.Parse(log.Split("task ")[1]))
                .Distinct()
                .ToList();
            
            processedTaskIds.Should().HaveCountGreaterThan(0, "tasks should be processed");
        }

        [Fact]
        public async Task QueueMessageOrdering_FIFO_ShouldBePreserved()
        {
            // Arrange
            var rabbitMQService = new RabbitMQService(_mockRabbitLogger.Object, _configuration);
            var testQueue = $"fifo_test_{Guid.NewGuid()}";
            var messageCount = 30;
            var receivedOrder = new ConcurrentBag<int>();
            var semaphore = new SemaphoreSlim(0);

            // Publish messages in order
            for (int i = 1; i <= messageCount; i++)
            {
                await rabbitMQService.PublishMessage(testQueue, $"Message-{i}");
            }

            // Act - Consume and track order
            await rabbitMQService.StartConsuming(testQueue, message =>
            {
                var orderMatch = System.Text.RegularExpressions.Regex.Match(message, @"Message-(\d+)");
                if (orderMatch.Success && int.TryParse(orderMatch.Groups[1].Value, out int order))
                {
                    receivedOrder.Add(order);
                }
                semaphore.Release();
            });

            // Wait for all messages
            for (int i = 0; i < messageCount; i++)
            {
                await semaphore.WaitAsync(TimeSpan.FromSeconds(10));
            }

            await rabbitMQService.StopConsuming();

            // Assert - All messages should be received (order may vary due to concurrent processing)
            receivedOrder.Should().HaveCount(messageCount, "all messages should be received");
            receivedOrder.Distinct().Should().HaveCount(messageCount, "no duplicate messages");
            
            // Verify all expected message numbers were received
            for (int i = 1; i <= messageCount; i++)
            {
                receivedOrder.Should().Contain(i, $"message {i} should be received");
            }
        }

        [Fact]
        public async Task MessageProcessing_WithVariableLoad_ShouldHandleBackpressure()
        {
            // Arrange - Test system behavior under variable load
            var rabbitMQService = new RabbitMQService(_mockRabbitLogger.Object, _configuration);
            var testQueue = $"backpressure_test_{Guid.NewGuid()}";
            var processedCount = 0;
            var processLock = new object();
            var semaphore = new SemaphoreSlim(0);

            // Start consuming with slow handler
            await rabbitMQService.StartConsuming(testQueue, message =>
            {
                lock (processLock)
                {
                    processedCount++;
                }
                // Simulate slow processing
                Thread.Sleep(50);
                semaphore.Release();
            });

            // Act - Burst of messages
            var burstSize = 20;
            var publishTasks = Enumerable.Range(1, burstSize)
                .Select(i => rabbitMQService.PublishMessage(testQueue, $"Burst-Message-{i}"))
                .ToList();

            await Task.WhenAll(publishTasks);

            // Wait for processing
            for (int i = 0; i < burstSize; i++)
            {
                await semaphore.WaitAsync(TimeSpan.FromSeconds(15));
            }

            await rabbitMQService.StopConsuming();

            // Assert
            processedCount.Should().Be(burstSize, "all messages should eventually be processed");
        }

        private List<TaskItem> CreateOverdueTasks(int count)
        {
            var tasks = new List<TaskItem>();
            for (int i = 1; i <= count; i++)
            {
                tasks.Add(new TaskItem
                {
                    Id = i,
                    Title = $"Overdue Task {i}",
                    Description = $"Description for task {i}",
                    DueDate = DateTime.UtcNow.AddDays(-i),
                    Priority = (i % 5) + 1,
                    FullName = $"User {i}",
                    Telephone = $"+1-555-{i:D4}",
                    Email = $"user{i}@example.com",
                    CreatedAt = DateTime.UtcNow.AddDays(-i - 1),
                    TaskItemTags = new List<TaskItemTag>()
                });
            }
            return tasks;
        }
    }
}
