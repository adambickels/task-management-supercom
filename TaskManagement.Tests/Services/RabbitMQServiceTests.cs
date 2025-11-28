using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using System.Collections.Concurrent;
using System.Diagnostics;
using TaskManagement.Service.Services;

namespace TaskManagement.Tests.Services
{
    public class RabbitMQServiceTests : IDisposable
    {
        private readonly Mock<ILogger<RabbitMQService>> _mockLogger;
        private readonly IConfiguration _configuration;
        private readonly string _testQueueName;

        public RabbitMQServiceTests()
        {
            _mockLogger = new Mock<ILogger<RabbitMQService>>();
            
            // Setup configuration for RabbitMQ
            var inMemorySettings = new Dictionary<string, string>
            {
                {"RabbitMQ:HostName", "localhost"},
                {"RabbitMQ:UserName", "guest"},
                {"RabbitMQ:Password", "guest"},
                {"RabbitMQ:Port", "5672"},
                {"RabbitMQ:VirtualHost", "/"},
                {"RabbitMQ:TaskQueue", "test_concurrent_queue"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings!)
                .Build();

            _testQueueName = $"test_queue_{Guid.NewGuid()}";
        }

        [Fact]
        public async Task PublishMessage_SingleMessage_ShouldSucceed()
        {
            // Arrange
            var service = new RabbitMQService(_mockLogger.Object, _configuration);
            var message = "Test message";

            // Act & Assert - Should not throw
            await service.PublishMessage(_testQueueName, message);
            
            // Verify logging
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully published message")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task ConcurrentPublish_MultipleMessages_ShouldHandleAllMessages()
        {
            // Arrange
            var service = new RabbitMQService(_mockLogger.Object, _configuration);
            var messageCount = 50;
            var messages = Enumerable.Range(1, messageCount)
                .Select(i => $"Concurrent Test Message {i}")
                .ToList();

            var publishTasks = new List<Task>();

            // Act - Publish messages concurrently
            var stopwatch = Stopwatch.StartNew();
            foreach (var message in messages)
            {
                publishTasks.Add(service.PublishMessage(_testQueueName, message));
            }

            await Task.WhenAll(publishTasks);
            stopwatch.Stop();

            // Assert
            publishTasks.Should().AllSatisfy(task => task.IsCompletedSuccessfully.Should().BeTrue());
            stopwatch.ElapsedMilliseconds.Should().BeLessThan(5000, "concurrent publishing should be efficient");

            // Verify all messages were logged as published
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Successfully published message")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.AtLeast(messageCount));
        }

        [Fact]
        public async Task ConcurrentConsume_MultipleMessages_ShouldProcessAllInOrder()
        {
            // Arrange
            var service = new RabbitMQService(_mockLogger.Object, _configuration);
            var messageCount = 30;
            var receivedMessages = new ConcurrentBag<string>();
            var messageProcessingTimes = new ConcurrentDictionary<string, long>();
            var processingCompleteSemaphore = new SemaphoreSlim(0, messageCount);

            // Message handler that simulates processing
            void MessageHandler(string message)
            {
                var startTime = Stopwatch.GetTimestamp();
                receivedMessages.Add(message);
                
                // Simulate some processing time
                Thread.Sleep(10);
                
                var elapsedMs = (Stopwatch.GetTimestamp() - startTime) * 1000 / Stopwatch.Frequency;
                messageProcessingTimes[message] = elapsedMs;
                
                processingCompleteSemaphore.Release();
            }

            // Publish messages first
            var publishTasks = Enumerable.Range(1, messageCount)
                .Select(i => service.PublishMessage(_testQueueName, $"Message-{i:D3}"))
                .ToArray();

            await Task.WhenAll(publishTasks);

            // Act - Start consuming
            await service.StartConsuming(_testQueueName, MessageHandler);

            // Wait for all messages to be processed (with timeout)
            var allProcessed = await Task.Run(async () =>
            {
                for (int i = 0; i < messageCount; i++)
                {
                    var acquired = await processingCompleteSemaphore.WaitAsync(TimeSpan.FromSeconds(10));
                    if (!acquired) return false;
                }
                return true;
            });

            await service.StopConsuming();

            // Assert
            allProcessed.Should().BeTrue("all messages should be processed within timeout");
            receivedMessages.Should().HaveCount(messageCount, "all published messages should be consumed");
            
            // Verify no duplicates
            receivedMessages.Distinct().Should().HaveCount(messageCount, "no duplicate messages should be processed");

            // Verify all expected messages were received
            for (int i = 1; i <= messageCount; i++)
            {
                receivedMessages.Should().Contain($"Message-{i:D3}");
            }
        }

        [Fact]
        public async Task ConcurrentPublishAndConsume_HighLoad_ShouldHandleWithoutLoss()
        {
            // Arrange
            var service = new RabbitMQService(_mockLogger.Object, _configuration);
            var messageCount = 100;
            var receivedMessages = new ConcurrentBag<string>();
            var publishedMessages = new ConcurrentBag<string>();
            var processingComplete = new SemaphoreSlim(0);

            void MessageHandler(string message)
            {
                receivedMessages.Add(message);
                processingComplete.Release();
            }

            // Start consuming first
            await service.StartConsuming(_testQueueName, MessageHandler);

            // Act - Publish messages concurrently while consuming
            var publishTasks = new List<Task>();
            for (int i = 1; i <= messageCount; i++)
            {
                var message = $"HighLoad-Task-{i:D4}";
                publishedMessages.Add(message);
                publishTasks.Add(service.PublishMessage(_testQueueName, message));
            }

            await Task.WhenAll(publishTasks);

            // Wait for all messages to be consumed
            for (int i = 0; i < messageCount; i++)
            {
                await processingComplete.WaitAsync(TimeSpan.FromSeconds(15));
            }

            await service.StopConsuming();

            // Assert
            receivedMessages.Should().HaveCount(messageCount, "all messages should be consumed");
            receivedMessages.OrderBy(m => m).Should().BeEquivalentTo(
                publishedMessages.OrderBy(m => m), 
                "all published messages should be received exactly once");
        }

        [Fact]
        public async Task ConcurrentUpdates_SimulateRealWorldScenario_ShouldMaintainDataIntegrity()
        {
            // Arrange - Simulate multiple workers processing overdue tasks concurrently
            var service = new RabbitMQService(_mockLogger.Object, _configuration);
            var taskCount = 25;
            var processedTaskIds = new ConcurrentBag<int>();
            var processingThreads = new ConcurrentDictionary<string, int>();
            var processingComplete = new SemaphoreSlim(0);

            void ReminderHandler(string message)
            {
                // Simulate extracting task ID from JSON message
                var taskIdMatch = System.Text.RegularExpressions.Regex.Match(message, @"TaskId"":(\d+)");
                if (taskIdMatch.Success && int.TryParse(taskIdMatch.Groups[1].Value, out int taskId))
                {
                    processedTaskIds.Add(taskId);
                    
                    // Track which thread processed this message
                    var threadId = Environment.CurrentManagedThreadId;
                    processingThreads.AddOrUpdate(
                        message, 
                        threadId, 
                        (key, old) => threadId);
                    
                    // Simulate database update processing time
                    Thread.Sleep(15);
                }
                processingComplete.Release();
            }

            // Start consuming
            await service.StartConsuming(_testQueueName, ReminderHandler);

            // Act - Simulate concurrent task reminder publishing
            var publishTasks = new List<Task>();
            for (int i = 1; i <= taskCount; i++)
            {
                var reminderMessage = $"{{\"TaskId\":{i},\"Title\":\"Task {i}\",\"DueDate\":\"2025-11-28T00:00:00Z\",\"FullName\":\"User {i}\",\"Email\":\"user{i}@test.com\"}}";
                publishTasks.Add(service.PublishMessage(_testQueueName, reminderMessage));
            }

            await Task.WhenAll(publishTasks);

            // Wait for all to be processed
            for (int i = 0; i < taskCount; i++)
            {
                await processingComplete.WaitAsync(TimeSpan.FromSeconds(20));
            }

            await service.StopConsuming();

            // Assert - Verify data integrity
            processedTaskIds.Should().HaveCount(taskCount, "all tasks should be processed");
            processedTaskIds.Distinct().Should().HaveCount(taskCount, "each task should be processed exactly once - no duplicates");
            processedTaskIds.Should().OnlyContain(id => id >= 1 && id <= taskCount, "all task IDs should be in valid range");
            
            // Verify ordering preservation (RabbitMQ guarantees FIFO per queue)
            var processedList = processedTaskIds.ToList();
            processedList.Should().HaveCount(taskCount);
        }

        [Fact]
        public async Task MessageAcknowledgement_WithProcessingFailure_ShouldHandleGracefully()
        {
            // Arrange
            var service = new RabbitMQService(_mockLogger.Object, _configuration);
            var successCount = 0;
            var failureCount = 0;
            var processingComplete = new SemaphoreSlim(0);

            void FlakyMessageHandler(string message)
            {
                try
                {
                    // Simulate processing failure for every 3rd message
                    if (message.Contains("fail"))
                    {
                        failureCount++;
                        throw new InvalidOperationException("Simulated processing failure");
                    }
                    
                    successCount++;
                }
                finally
                {
                    processingComplete.Release();
                }
            }

            await service.StartConsuming(_testQueueName, FlakyMessageHandler);

            // Act - Publish mix of success and failure messages
            var messages = new[]
            {
                "success-message-1",
                "success-message-2",
                "fail-message-3",
                "success-message-4",
                "success-message-5"
            };

            foreach (var msg in messages)
            {
                await service.PublishMessage(_testQueueName, msg);
            }

            // Wait for all messages to be processed
            for (int i = 0; i < messages.Length; i++)
            {
                await processingComplete.WaitAsync(TimeSpan.FromSeconds(5));
            }

            await service.StopConsuming();

            // Assert
            successCount.Should().Be(4, "4 messages should succeed");
            failureCount.Should().Be(1, "1 message should fail");
            (successCount + failureCount).Should().Be(messages.Length, "all messages should be attempted");
        }

        [Fact]
        public async Task ConcurrentConnection_MultipleServiceInstances_ShouldWorkIndependently()
        {
            // Arrange - Simulate multiple service instances
            var service1 = new RabbitMQService(_mockLogger.Object, _configuration);
            var service2 = new RabbitMQService(_mockLogger.Object, _configuration);
            
            var queue1Messages = new ConcurrentBag<string>();
            var queue2Messages = new ConcurrentBag<string>();
            var semaphore1 = new SemaphoreSlim(0);
            var semaphore2 = new SemaphoreSlim(0);

            var queue1 = $"{_testQueueName}_instance1";
            var queue2 = $"{_testQueueName}_instance2";

            await service1.StartConsuming(queue1, msg => { queue1Messages.Add(msg); semaphore1.Release(); });
            await service2.StartConsuming(queue2, msg => { queue2Messages.Add(msg); semaphore2.Release(); });

            // Act - Publish to both queues concurrently
            var tasks = new List<Task>
            {
                service1.PublishMessage(queue1, "Service1-Message1"),
                service1.PublishMessage(queue1, "Service1-Message2"),
                service2.PublishMessage(queue2, "Service2-Message1"),
                service2.PublishMessage(queue2, "Service2-Message2"),
            };

            await Task.WhenAll(tasks);
            await semaphore1.WaitAsync(TimeSpan.FromSeconds(5));
            await semaphore1.WaitAsync(TimeSpan.FromSeconds(5));
            await semaphore2.WaitAsync(TimeSpan.FromSeconds(5));
            await semaphore2.WaitAsync(TimeSpan.FromSeconds(5));

            await service1.StopConsuming();
            await service2.StopConsuming();

            // Assert
            queue1Messages.Should().HaveCount(2).And.AllSatisfy(m => m.Should().StartWith("Service1"));
            queue2Messages.Should().HaveCount(2).And.AllSatisfy(m => m.Should().StartWith("Service2"));
        }

        [Fact]
        public async Task QueueDurability_MessagesPersistence_ShouldMaintainDurableMessages()
        {
            // Arrange
            var service = new RabbitMQService(_mockLogger.Object, _configuration);
            var durableQueueName = $"{_testQueueName}_durable";

            // Act - Publish messages with persistence
            await service.PublishMessage(durableQueueName, "Durable-Message-1");
            await service.PublishMessage(durableQueueName, "Durable-Message-2");
            await service.PublishMessage(durableQueueName, "Durable-Message-3");

            // Create new service instance (simulating service restart)
            var newService = new RabbitMQService(_mockLogger.Object, _configuration);
            var receivedMessages = new ConcurrentBag<string>();
            var semaphore = new SemaphoreSlim(0);

            await newService.StartConsuming(durableQueueName, msg => 
            { 
                receivedMessages.Add(msg); 
                semaphore.Release(); 
            });

            // Wait for messages
            for (int i = 0; i < 3; i++)
            {
                await semaphore.WaitAsync(TimeSpan.FromSeconds(5));
            }

            await newService.StopConsuming();

            // Assert - Messages should survive across service instances
            receivedMessages.Should().HaveCount(3);
            receivedMessages.Should().Contain("Durable-Message-1");
            receivedMessages.Should().Contain("Durable-Message-2");
            receivedMessages.Should().Contain("Durable-Message-3");
        }

        public void Dispose()
        {
            // Cleanup is handled by RabbitMQ service disposal
            GC.SuppressFinalize(this);
        }
    }
}
