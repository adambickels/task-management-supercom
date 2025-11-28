using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Enums;
using TaskManagement.Infrastructure.Data;
using TaskManagement.Infrastructure.Repositories;

namespace TaskManagement.Tests.Repositories
{
    public class TaskItemRepositoryTests : IDisposable
    {
        private readonly TaskManagementDbContext _context;
        private readonly TaskItemRepository _repository;
        private readonly Mock<ILogger<TaskItemRepository>> _mockLogger;

        public TaskItemRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new TaskManagementDbContext(options);
            _mockLogger = new Mock<ILogger<TaskItemRepository>>();
            _repository = new TaskItemRepository(_context, _mockLogger.Object);

            // Seed test data
            SeedTestData();
        }

        private void SeedTestData()
        {
            var tags = new List<Tag>
            {
                new Tag { Id = 1, Name = "Urgent" },
                new Tag { Id = 2, Name = "Backend" },
                new Tag { Id = 3, Name = "Frontend" }
            };

            var tasks = new List<TaskItem>
            {
                new TaskItem
                {
                    Id = 1,
                    Title = "Task 1",
                    Description = "Description 1",
                    DueDate = DateTime.UtcNow.AddDays(1),
                    Priority = TaskPriority.High,
                    FullName = "John Doe",
                    Telephone = "+1-555-0001",
                    Email = "john@example.com",
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                },
                new TaskItem
                {
                    Id = 2,
                    Title = "Task 2",
                    Description = "Description 2",
                    DueDate = DateTime.UtcNow.AddDays(-1),
                    Priority = TaskPriority.Low,
                    FullName = "Jane Doe",
                    Telephone = "+1-555-0002",
                    Email = "jane@example.com",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    IsDeleted = false
                },
                new TaskItem
                {
                    Id = 3,
                    Title = "Deleted Task",
                    Description = "This is deleted",
                    DueDate = DateTime.UtcNow.AddDays(2),
                    Priority = TaskPriority.Medium,
                    FullName = "Bob Smith",
                    Telephone = "+1-555-0003",
                    Email = "bob@example.com",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    IsDeleted = true,
                    DeletedAt = DateTime.UtcNow
                }
            };

            _context.Tags.AddRange(tags);
            _context.TaskItems.AddRange(tasks);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnOnlyNonDeletedTasks()
        {
            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            result.Should().HaveCount(2);
            result.Should().NotContain(t => t.IsDeleted);
            result.Should().Contain(t => t.Title == "Task 1");
            result.Should().Contain(t => t.Title == "Task 2");
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnTask()
        {
            // Act
            var result = await _repository.GetByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result!.Title.Should().Be("Task 1");
            result.Email.Should().Be("john@example.com");
        }

        [Fact]
        public async Task GetByIdAsync_WithDeletedTaskId_ShouldReturnNull()
        {
            // Act
            var result = await _repository.GetByIdAsync(3);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ShouldReturnNull()
        {
            // Act
            var result = await _repository.GetByIdAsync(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task GetPagedAsync_ShouldReturnCorrectPageSize()
        {
            // Act
            var result = await _repository.GetPagedAsync(1, 1);

            // Assert
            result.Items.Should().HaveCount(1);
            result.CurrentPage.Should().Be(1);
            result.PageSize.Should().Be(1);
            result.TotalCount.Should().Be(2); // Only non-deleted
            result.TotalPages.Should().Be(2);
        }

        [Fact]
        public async Task GetPagedAsync_SecondPage_ShouldReturnCorrectItems()
        {
            // Act
            var result = await _repository.GetPagedAsync(2, 1);

            // Assert
            result.Items.Should().HaveCount(1);
            result.CurrentPage.Should().Be(2);
            result.HasPreviousPage.Should().BeTrue();
            result.HasNextPage.Should().BeFalse();
        }

        [Fact]
        public async Task GetPagedAsync_PageBeyondTotal_ShouldReturnEmpty()
        {
            // Act
            var result = await _repository.GetPagedAsync(10, 10);

            // Assert
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(2);
            result.HasPreviousPage.Should().BeTrue();
            result.HasNextPage.Should().BeFalse();
        }

        [Fact]
        public async Task CreateAsync_ShouldAddTaskToDatabase()
        {
            // Arrange
            var newTask = new TaskItem
            {
                Title = "New Task",
                Description = "New Description",
                DueDate = DateTime.UtcNow.AddDays(3),
                Priority = TaskPriority.Critical,
                FullName = "Alice Johnson",
                Telephone = "+1-555-0004",
                Email = "alice@example.com"
            };

            // Act
            var result = await _repository.CreateAsync(newTask);

            // Assert
            result.Id.Should().BeGreaterThan(0);
            result.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
            
            var savedTask = await _context.TaskItems.FindAsync(result.Id);
            savedTask.Should().NotBeNull();
            savedTask!.Title.Should().Be("New Task");
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyExistingTask()
        {
            // Arrange
            var task = await _repository.GetByIdAsync(1);
            task!.Title = "Updated Title";
            task.Priority = TaskPriority.Critical;

            // Act
            var result = await _repository.UpdateAsync(task);

            // Assert
            result.Title.Should().Be("Updated Title");
            result.Priority.Should().Be(TaskPriority.Critical);
            result.UpdatedAt.Should().NotBeNull();
            result.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        }

        [Fact]
        public async Task DeleteAsync_ShouldSoftDeleteTask()
        {
            // Act
            var result = await _repository.DeleteAsync(1);

            // Assert
            result.Should().BeTrue();

            // Verify soft delete
            var deletedTask = await _context.TaskItems
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == 1);

            deletedTask.Should().NotBeNull();
            deletedTask!.IsDeleted.Should().BeTrue();
            deletedTask.DeletedAt.Should().NotBeNull();

            // Should not appear in normal queries
            var normalQuery = await _repository.GetByIdAsync(1);
            normalQuery.Should().BeNull();
        }

        [Fact]
        public async Task DeleteAsync_WithInvalidId_ShouldReturnFalse()
        {
            // Act
            var result = await _repository.DeleteAsync(999);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task GetOverdueTasksAsync_ShouldReturnOnlyOverdueTasks()
        {
            // Act
            var result = await _repository.GetOverdueTasksAsync();

            // Assert
            result.Should().HaveCount(1);
            result.First().Title.Should().Be("Task 2");
            result.First().DueDate.Should().BeBefore(DateTime.UtcNow);
        }

        [Fact]
        public async Task GetOverdueTasksAsync_ShouldNotIncludeDeletedTasks()
        {
            // Arrange - Make the deleted task overdue
            var deletedTask = await _context.TaskItems
                .IgnoreQueryFilters()
                .FirstAsync(t => t.Id == 3);
            deletedTask.DueDate = DateTime.UtcNow.AddDays(-5);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetOverdueTasksAsync();

            // Assert
            result.Should().NotContain(t => t.Id == 3);
        }

        [Fact]
        public async Task GetPagedAsync_WithPageNumberZero_ShouldReturnFirstPage()
        {
            // Act
            var result = await _repository.GetPagedAsync(0, 10);

            // Assert
            result.Items.Should().HaveCount(2);
            result.CurrentPage.Should().Be(1); // Should default to page 1
        }

        [Fact]
        public async Task GetPagedAsync_WithNegativePageNumber_ShouldReturnFirstPage()
        {
            // Act
            var result = await _repository.GetPagedAsync(-5, 10);

            // Assert
            result.Items.Should().HaveCount(2);
            result.CurrentPage.Should().Be(1); // Should default to page 1
        }

        [Fact]
        public async Task GetPagedAsync_WithPageSizeZero_ShouldUseDefaultPageSize()
        {
            // Act
            var result = await _repository.GetPagedAsync(1, 0);

            // Assert
            result.Items.Should().NotBeEmpty();
            result.PageSize.Should().BeGreaterThan(0); // Should use default
        }

        [Fact]
        public async Task GetPagedAsync_WithExcessivePageSize_ShouldCapAtMaximum()
        {
            // Act
            var result = await _repository.GetPagedAsync(1, 1000);

            // Assert
            result.Items.Should().HaveCount(2);
            result.PageSize.Should().BeLessThanOrEqualTo(100); // Should cap at max
        }

        [Fact]
        public async Task GetPagedAsync_WithEmptyDatabase_ShouldReturnEmptyResult()
        {
            // Arrange - Delete all tasks
            var allTasks = await _context.TaskItems.IgnoreQueryFilters().ToListAsync();
            _context.TaskItems.RemoveRange(allTasks);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetPagedAsync(1, 10);

            // Assert
            result.Items.Should().BeEmpty();
            result.TotalCount.Should().Be(0);
            result.TotalPages.Should().Be(0);
            result.HasPreviousPage.Should().BeFalse();
            result.HasNextPage.Should().BeFalse();
        }

        [Fact]
        public async Task GetPagedAsync_LastPagePartial_ShouldReturnRemainingItems()
        {
            // Arrange - Add one more task so we have 3 total
            var newTask = new TaskItem
            {
                Title = "Task 4",
                Description = "Description 4",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = TaskPriority.Low,
                FullName = "Test User",
                Telephone = "+1-555-0004",
                Email = "test@example.com",
                IsDeleted = false
            };
            await _context.TaskItems.AddAsync(newTask);
            await _context.SaveChangesAsync();

            // Act - Request page 2 with page size 2 (should return 1 item)
            var result = await _repository.GetPagedAsync(2, 2);

            // Assert
            result.Items.Should().HaveCount(1);
            result.CurrentPage.Should().Be(2);
            result.HasNextPage.Should().BeFalse();
            result.HasPreviousPage.Should().BeTrue();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
