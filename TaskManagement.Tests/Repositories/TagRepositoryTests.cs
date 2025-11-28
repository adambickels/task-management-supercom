using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TaskManagement.Core.Entities;
using TaskManagement.Infrastructure.Data;
using TaskManagement.Infrastructure.Repositories;

namespace TaskManagement.Tests.Repositories
{
    public class TagRepositoryTests : IDisposable
    {
        private readonly TaskManagementDbContext _context;
        private readonly TagRepository _repository;
        private readonly Mock<ILogger<TagRepository>> _mockLogger;

        public TagRepositoryTests()
        {
            var options = new DbContextOptionsBuilder<TaskManagementDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new TaskManagementDbContext(options);
            _mockLogger = new Mock<ILogger<TagRepository>>();
            _repository = new TagRepository(_context, _mockLogger.Object);

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

            _context.Tags.AddRange(tags);
            _context.SaveChanges();
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnAllTags()
        {
            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            result.Should().HaveCount(3);
            result.Should().Contain(t => t.Name == "Urgent");
            result.Should().Contain(t => t.Name == "Backend");
            result.Should().Contain(t => t.Name == "Frontend");
        }

        [Fact]
        public async Task GetByIdAsync_WithValidId_ShouldReturnTag()
        {
            // Act
            var result = await _repository.GetByIdAsync(1);

            // Assert
            result.Should().NotBeNull();
            result!.Name.Should().Be("Urgent");
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
        public async Task CreateAsync_ShouldAddTagToDatabase()
        {
            // Arrange
            var newTag = new Tag { Name = "Bug" };

            // Act
            var result = await _repository.CreateAsync(newTag);

            // Assert
            result.Id.Should().BeGreaterThan(0);
            result.Name.Should().Be("Bug");
            
            var savedTag = await _context.Tags.FindAsync(result.Id);
            savedTag.Should().NotBeNull();
            savedTag!.Name.Should().Be("Bug");
        }

        [Fact]
        public async Task UpdateAsync_ShouldModifyExistingTag()
        {
            // Arrange
            var tag = await _repository.GetByIdAsync(1);
            tag!.Name = "Very Urgent";

            // Act
            var result = await _repository.UpdateAsync(tag);

            // Assert
            result.Name.Should().Be("Very Urgent");
            
            var updatedTag = await _context.Tags.FindAsync(1);
            updatedTag!.Name.Should().Be("Very Urgent");
        }

        [Fact]
        public async Task DeleteAsync_ShouldRemoveTagFromDatabase()
        {
            // Act
            var result = await _repository.DeleteAsync(1);

            // Assert
            result.Should().BeTrue();

            var deletedTag = await _context.Tags.FindAsync(1);
            deletedTag.Should().BeNull();
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
        public async Task GetAllAsync_ShouldReturnEmptyListWhenNoTags()
        {
            // Arrange - Clear all tags
            var allTags = await _context.Tags.ToListAsync();
            _context.Tags.RemoveRange(allTags);
            await _context.SaveChangesAsync();

            // Act
            var result = await _repository.GetAllAsync();

            // Assert
            result.Should().BeEmpty();
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}
