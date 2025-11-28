using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Interfaces;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Infrastructure.Repositories
{
    public class TagRepository : ITagRepository
    {
        private readonly TaskManagementDbContext _context;
        private readonly ILogger<TagRepository> _logger;

        public TagRepository(TaskManagementDbContext context, ILogger<TagRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<Tag>> GetAllAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all tags");
                var tags = await _context.Tags
                    .OrderBy(t => t.Name)
                    .ToListAsync();
                _logger.LogInformation("Retrieved {TagCount} tags", tags.Count());
                return tags;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all tags");
                throw;
            }
        }

        public async Task<Tag?> GetByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving tag with ID: {TagId}", id);
                var tag = await _context.Tags.FindAsync(id);
                
                if (tag == null)
                    _logger.LogWarning("Tag with ID {TagId} not found", id);
                else
                    _logger.LogInformation("Successfully retrieved tag with ID: {TagId}", id);
                
                return tag;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tag with ID: {TagId}", id);
                throw;
            }
        }

        public async Task<Tag> CreateAsync(Tag tag)
        {
            try
            {
                _logger.LogInformation("Creating new tag: {TagName}", tag.Name);
                _context.Tags.Add(tag);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully created tag with ID: {TagId}", tag.Id);
                return tag;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tag: {TagName}", tag.Name);
                throw;
            }
        }

        public async Task<Tag> UpdateAsync(Tag tag)
        {
            try
            {
                _logger.LogInformation("Updating tag with ID: {TagId}", tag.Id);
                _context.Tags.Update(tag);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully updated tag with ID: {TagId}", tag.Id);
                return tag;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tag with ID: {TagId}", tag.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting tag with ID: {TagId}", id);
                var tag = await _context.Tags.FindAsync(id);
                if (tag == null)
                {
                    _logger.LogWarning("Tag with ID {TagId} not found for deletion", id);
                    return false;
                }

                _context.Tags.Remove(tag);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully deleted tag with ID: {TagId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tag with ID: {TagId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Tag>> GetTagsByIdsAsync(IEnumerable<int> ids)
        {
            try
            {
                _logger.LogInformation("Retrieving tags by IDs: {TagIds}", string.Join(", ", ids));
                var tags = await _context.Tags
                    .Where(t => ids.Contains(t.Id))
                    .ToListAsync();
                _logger.LogInformation("Retrieved {TagCount} tags by IDs", tags.Count());
                return tags;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tags by IDs: {TagIds}", string.Join(", ", ids));
                throw;
            }
        }
    }
}
