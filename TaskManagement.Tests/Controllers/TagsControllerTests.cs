using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManagement.API.Controllers;
using TaskManagement.Core.DTOs;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Interfaces;
using AutoMapper;

namespace TaskManagement.Tests.Controllers
{
    public class TagsControllerTests
    {
        private readonly Mock<ITagRepository> _mockTagRepository;
        private readonly Mock<ILogger<TagsController>> _mockLogger;
        private readonly Mock<IMapper> _mockMapper;
        private readonly TagsController _controller;

        public TagsControllerTests()
        {
            _mockTagRepository = new Mock<ITagRepository>();
            _mockLogger = new Mock<ILogger<TagsController>>();
            _mockMapper = new Mock<IMapper>();
            _controller = new TagsController(
                _mockTagRepository.Object,
                _mockLogger.Object,
                _mockMapper.Object
            );
        }

        [Fact]
        public async Task GetAllTags_ShouldReturnOkWithTags()
        {
            // Arrange
            var tags = new List<Tag>
            {
                new Tag { Id = 1, Name = "Urgent" },
                new Tag { Id = 2, Name = "Backend" }
            };
            var tagDtos = new List<TagDto>
            {
                new TagDto { Id = 1, Name = "Urgent" },
                new TagDto { Id = 2, Name = "Backend" }
            };

            _mockTagRepository.Setup(r => r.GetAllAsync()).ReturnsAsync(tags);
            _mockMapper.Setup(m => m.Map<IEnumerable<TagDto>>(tags)).Returns(tagDtos);

            // Act
            var result = await _controller.GetAllTags();

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedTags = okResult.Value.Should().BeAssignableTo<IEnumerable<TagDto>>().Subject;
            returnedTags.Should().HaveCount(2);
        }

        [Fact]
        public async Task GetTag_WithValidId_ShouldReturnOkWithTag()
        {
            // Arrange
            var tag = new Tag { Id = 1, Name = "Urgent" };
            var tagDto = new TagDto { Id = 1, Name = "Urgent" };

            _mockTagRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(tag);
            _mockMapper.Setup(m => m.Map<TagDto>(tag)).Returns(tagDto);

            // Act
            var result = await _controller.GetTag(1);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedTag = okResult.Value.Should().BeOfType<TagDto>().Subject;
            returnedTag.Name.Should().Be("Urgent");
        }

        [Fact]
        public async Task GetTag_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            _mockTagRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Tag?)null);

            // Act
            var result = await _controller.GetTag(999);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task CreateTag_WithValidData_ShouldReturnCreatedResult()
        {
            // Arrange
            var tagDto = new TagDto { Name = "New Tag" };
            var tag = new Tag { Name = "New Tag" };
            var createdTag = new Tag { Id = 1, Name = "New Tag" };
            var createdTagDto = new TagDto { Id = 1, Name = "New Tag" };

            _mockMapper.Setup(m => m.Map<Tag>(tagDto)).Returns(tag);
            _mockTagRepository.Setup(r => r.CreateAsync(tag)).ReturnsAsync(createdTag);
            _mockMapper.Setup(m => m.Map<TagDto>(createdTag)).Returns(createdTagDto);

            // Act
            var result = await _controller.CreateTag(tagDto);

            // Assert
            var createdResult = result.Result.Should().BeOfType<CreatedAtActionResult>().Subject;
            createdResult.ActionName.Should().Be(nameof(TagsController.GetTag));
            var returnedTag = createdResult.Value.Should().BeOfType<TagDto>().Subject;
            returnedTag.Id.Should().Be(1);
            returnedTag.Name.Should().Be("New Tag");
        }

        [Fact]
        public async Task UpdateTag_WithValidData_ShouldReturnOk()
        {
            // Arrange
            var tagDto = new TagDto { Id = 1, Name = "Updated Tag" };
            var existingTag = new Tag { Id = 1, Name = "Old Name" };
            var updatedTag = new Tag { Id = 1, Name = "Updated Tag" };
            var updatedTagDto = new TagDto { Id = 1, Name = "Updated Tag" };

            _mockTagRepository.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(existingTag);
            _mockMapper.Setup(m => m.Map(tagDto, existingTag));
            _mockTagRepository.Setup(r => r.UpdateAsync(existingTag)).ReturnsAsync(updatedTag);
            _mockMapper.Setup(m => m.Map<TagDto>(updatedTag)).Returns(updatedTagDto);

            // Act
            var result = await _controller.UpdateTag(1, tagDto);

            // Assert
            var okResult = result.Result.Should().BeOfType<OkObjectResult>().Subject;
            var returnedTag = okResult.Value.Should().BeOfType<TagDto>().Subject;
            returnedTag.Name.Should().Be("Updated Tag");
        }

        [Fact]
        public async Task UpdateTag_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            var tagDto = new TagDto { Id = 999, Name = "Updated Tag" };
            _mockTagRepository.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Tag?)null);

            // Act
            var result = await _controller.UpdateTag(999, tagDto);

            // Assert
            result.Result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task DeleteTag_WithValidId_ShouldReturnNoContent()
        {
            // Arrange
            _mockTagRepository.Setup(r => r.DeleteAsync(1)).ReturnsAsync(true);

            // Act
            var result = await _controller.DeleteTag(1);

            // Assert
            result.Should().BeOfType<NoContentResult>();
        }

        [Fact]
        public async Task DeleteTag_WithInvalidId_ShouldReturnNotFound()
        {
            // Arrange
            _mockTagRepository.Setup(r => r.DeleteAsync(999)).ReturnsAsync(false);

            // Act
            var result = await _controller.DeleteTag(999);

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }


    }
}

