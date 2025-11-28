using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManagement.API.Controllers;
using TaskManagement.Core.DTOs;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Interfaces;

namespace TaskManagement.Tests.Controllers
{
    public class TasksControllerTests
    {
        private readonly Mock<ITaskItemRepository> _mockTaskRepository;
        private readonly Mock<ITagRepository> _mockTagRepository;
        private readonly Mock<ILogger<TasksController>> _mockLogger;
        private readonly TasksController _controller;

        public TasksControllerTests()
        {
            _mockTaskRepository = new Mock<ITaskItemRepository>();
            _mockTagRepository = new Mock<ITagRepository>();
            _mockLogger = new Mock<ILogger<TasksController>>();
            _controller = new TasksController(
                _mockTaskRepository.Object,
                _mockTagRepository.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task GetAllTasks_ShouldReturnOkResult_WithListOfTasks()
        {
            // Arrange
            var tasks = new List<TaskItem>
            {
                new TaskItem
                {
                    Id = 1,
                    Title = "Test Task 1",
                    Description = "Description 1",
                    DueDate = DateTime.UtcNow.AddDays(1),
                    Priority = 3,
                    FullName = "John Doe",
                    Telephone = "+1-555-0001",
                    Email = "john@example.com",
                    TaskItemTags = new List<TaskItemTag>()
                },
                new TaskItem
                {
                    Id = 2,
                    Title = "Test Task 2",
                    Description = "Description 2",
                    DueDate = DateTime.UtcNow.AddDays(2),
                    Priority = 5,
                    FullName = "Jane Doe",
                    Telephone = "+1-555-0002",
                    Email = "jane@example.com",
                    TaskItemTags = new List<TaskItemTag>()
                }
            };

            _mockTaskRepository.Setup(repo => repo.GetAllAsync())
                .ReturnsAsync(tasks);

            // Act
            var result = await _controller.GetAllTasks();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedTasks = okResult.Value.Should().BeAssignableTo<IEnumerable<TaskItemDto>>().Subject;
            returnedTasks.Should().HaveCount(2);
            returnedTasks.First().Title.Should().Be("Test Task 1");
        }

        [Fact]
        public async Task GetTask_WithValidId_ShouldReturnOkResult_WithTask()
        {
            // Arrange
            var taskId = 1;
            var task = new TaskItem
            {
                Id = taskId,
                Title = "Test Task",
                Description = "Test Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = 3,
                FullName = "John Doe",
                Telephone = "+1-555-0001",
                Email = "john@example.com",
                TaskItemTags = new List<TaskItemTag>()
            };

            _mockTaskRepository.Setup(repo => repo.GetByIdAsync(taskId))
                .ReturnsAsync(task);

            // Act
            var result = await _controller.GetTask(taskId);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedTask = okResult.Value.Should().BeOfType<TaskItemDto>().Subject;
            returnedTask.Id.Should().Be(taskId);
            returnedTask.Title.Should().Be("Test Task");
        }

        [Fact]
        public async Task GetTask_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var taskId = 999;
            _mockTaskRepository.Setup(repo => repo.GetByIdAsync(taskId))
                .ReturnsAsync((TaskItem?)null);

            // Act
            var result = await _controller.GetTask(taskId);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task CreateTask_WithValidData_ShouldReturnCreatedResult()
        {
            // Arrange
            var taskDto = new TaskItemDto
            {
                Title = "New Task",
                Description = "New Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = 3,
                FullName = "John Doe",
                Telephone = "+1-555-0001",
                Email = "john@example.com",
                TagIds = new List<int> { 1, 2 }
            };

            var tags = new List<Tag>
            {
                new Tag { Id = 1, Name = "Tag1" },
                new Tag { Id = 2, Name = "Tag2" }
            };

            var createdTask = new TaskItem
            {
                Id = 1,
                Title = taskDto.Title,
                Description = taskDto.Description,
                DueDate = taskDto.DueDate,
                Priority = taskDto.Priority,
                FullName = taskDto.FullName,
                Telephone = taskDto.Telephone,
                Email = taskDto.Email,
                TaskItemTags = tags.Select(t => new TaskItemTag { TagId = t.Id, Tag = t }).ToList()
            };

            _mockTagRepository.Setup(repo => repo.GetTagsByIdsAsync(taskDto.TagIds))
                .ReturnsAsync(tags);
            _mockTaskRepository.Setup(repo => repo.CreateAsync(It.IsAny<TaskItem>()))
                .ReturnsAsync(createdTask);

            // Act
            var result = await _controller.CreateTask(taskDto);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            var returnedTask = createdResult.Value.Should().BeOfType<TaskItemDto>().Subject;
            returnedTask.Title.Should().Be("New Task");
        }

        [Fact]
        public async Task CreateTask_WithInvalidTags_ShouldReturnBadRequest()
        {
            // Arrange
            var taskDto = new TaskItemDto
            {
                Title = "New Task",
                Description = "New Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = 3,
                FullName = "John Doe",
                Telephone = "+1-555-0001",
                Email = "john@example.com",
                TagIds = new List<int> { 999 }
            };

            _mockTagRepository.Setup(repo => repo.GetTagsByIdsAsync(taskDto.TagIds))
                .ReturnsAsync(new List<Tag>());

            // Act
            var result = await _controller.CreateTask(taskDto);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateTask_WithValidData_ShouldReturnOkResult()
        {
            // Arrange
            var taskDto = new TaskItemDto
            {
                Id = 1,
                Title = "Updated Task",
                Description = "Updated Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = 4,
                FullName = "John Doe",
                Telephone = "+1-555-0001",
                Email = "john@example.com",
                TagIds = new List<int> { 1 }
            };

            var existingTask = new TaskItem
            {
                Id = 1,
                Title = "Old Task",
                Description = "Old Description",
                DueDate = DateTime.UtcNow,
                Priority = 3,
                FullName = "John Doe",
                Telephone = "+1-555-0001",
                Email = "john@example.com",
                TaskItemTags = new List<TaskItemTag>()
            };

            var tags = new List<Tag> { new Tag { Id = 1, Name = "Tag1" } };

            _mockTaskRepository.Setup(repo => repo.GetByIdAsync(taskDto.Id))
                .ReturnsAsync(existingTask);
            _mockTagRepository.Setup(repo => repo.GetTagsByIdsAsync(taskDto.TagIds))
                .ReturnsAsync(tags);
            _mockTaskRepository.Setup(repo => repo.UpdateAsync(It.IsAny<TaskItem>()))
                .ReturnsAsync(existingTask);

            // Act
            var result = await _controller.UpdateTask(taskDto.Id, taskDto);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedTask = okResult.Value.Should().BeOfType<TaskItemDto>().Subject;
            returnedTask.Id.Should().Be(1);
        }

        [Fact]
        public async Task UpdateTask_WithMismatchedIds_ShouldReturnBadRequest()
        {
            // Arrange
            var taskDto = new TaskItemDto
            {
                Id = 1,
                Title = "Updated Task",
                Description = "Updated Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = 4,
                FullName = "John Doe",
                Telephone = "+1-555-0001",
                Email = "john@example.com",
                TagIds = new List<int> { 1 }
            };

            // Act
            var result = await _controller.UpdateTask(2, taskDto);

            // Assert
            result.Result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task DeleteTask_WithValidId_ShouldReturnNoContent()
        {
            // Arrange
            var taskId = 1;
            _mockTaskRepository.Setup(repo => repo.DeleteAsync(taskId))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteTask(taskId);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task DeleteTask_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var taskId = 999;
            _mockTaskRepository.Setup(repo => repo.DeleteAsync(taskId))
                .ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteTask(taskId);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
