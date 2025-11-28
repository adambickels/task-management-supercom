using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using AutoMapper;
using TaskManagement.API.Controllers;
using TaskManagement.Core.DTOs;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Enums;
using TaskManagement.Core.Interfaces;
using TaskManagement.Core.Models;

namespace TaskManagement.Tests.Controllers
{
    public class TasksControllerTests
    {
        private readonly Mock<ITaskItemRepository> _mockTaskRepository;
        private readonly Mock<ITagRepository> _mockTagRepository;
        private readonly Mock<ILogger<TasksController>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IConfiguration> _mockConfiguration;
        private readonly TasksController _controller;

        public TasksControllerTests()
        {
            _mockTaskRepository = new Mock<ITaskItemRepository>();
            _mockTagRepository = new Mock<ITagRepository>();
            _mockLogger = new Mock<ILogger<TasksController>>();
            _mockMapper = new Mock<IMapper>();
            _mockConfiguration = new Mock<IConfiguration>();

            // Configuration will use default values since we're not setting anything
            
            _controller = new TasksController(
                _mockTaskRepository.Object,
                _mockTagRepository.Object,
                _mockLogger.Object,
                _mockMapper.Object,
                _mockConfiguration.Object);
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
                Description = "Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = TaskPriority.Medium,
                FullName = "John Doe",
                Telephone = "+1-555-0001",
                Email = "john@example.com",
                TaskItemTags = new List<TaskItemTag>()
            };

            var taskDto = new TaskItemDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate,
                Priority = task.Priority,
                FullName = task.FullName,
                Telephone = task.Telephone,
                Email = task.Email,
                TagIds = task.TaskItemTags.Select(tt => tt.TagId).ToList()
            };

            _mockTaskRepository.Setup(repo => repo.GetByIdAsync(taskId))
                .ReturnsAsync(task);
            _mockMapper.Setup(m => m.Map<TaskItemDto>(It.IsAny<TaskItem>()))
                .Returns(taskDto);

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
                Description = "Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = TaskPriority.Medium,
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

            var createdTaskDto = new TaskItemDto
            {
                Id = 1,
                Title = createdTask.Title,
                Description = createdTask.Description,
                DueDate = createdTask.DueDate,
                Priority = createdTask.Priority,
                FullName = createdTask.FullName,
                Telephone = createdTask.Telephone,
                Email = createdTask.Email,
                TagIds = taskDto.TagIds
            };

            _mockTagRepository.Setup(repo => repo.GetTagsByIdsAsync(taskDto.TagIds))
                .ReturnsAsync(tags);
            _mockMapper.Setup(m => m.Map<TaskItem>(It.IsAny<TaskItemDto>()))
                .Returns(new TaskItem { Title = taskDto.Title, Description = taskDto.Description });
            _mockTaskRepository.Setup(repo => repo.CreateAsync(It.IsAny<TaskItem>()))
                .ReturnsAsync(createdTask);
            _mockMapper.Setup(m => m.Map<TaskItemDto>(It.IsAny<TaskItem>()))
                .Returns(createdTaskDto);

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
                Priority = TaskPriority.Medium,
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
        public async Task UpdateTask_WithMismatchedIds_ShouldReturnBadRequest()
        {
            // Arrange
            var taskDto = new TaskItemDto
            {
                Id = 1,
                Title = "Updated Task",
                Description = "Updated Description",
                DueDate = DateTime.UtcNow.AddDays(1),
                Priority = TaskPriority.High,
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


