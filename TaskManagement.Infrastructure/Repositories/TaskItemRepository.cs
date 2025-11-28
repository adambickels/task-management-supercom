using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Interfaces;
using TaskManagement.Core.Models;
using TaskManagement.Infrastructure.Data;

namespace TaskManagement.Infrastructure.Repositories
{
    public class TaskItemRepository : ITaskItemRepository
    {
        private readonly TaskManagementDbContext _context;
        private readonly ILogger<TaskItemRepository> _logger;

        public TaskItemRepository(TaskManagementDbContext context, ILogger<TaskItemRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<IEnumerable<TaskItem>> GetAllAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all task items");
                // Use AsNoTracking for better performance on read-only queries
                var tasks = await _context.TaskItems
                    .Where(t => !t.IsDeleted)
                    .Include(t => t.TaskItemTags)
                    .ThenInclude(tt => tt.Tag)
                    .OrderByDescending(t => t.CreatedAt)
                    .AsNoTracking()
                    .ToListAsync();
                _logger.LogInformation("Retrieved {TaskCount} task items", tasks.Count());
                return tasks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all task items");
                throw;
            }
        }

        public async Task<PagedResult<TaskItem>> GetPagedAsync(int page, int pageSize)
        {
            try
            {
                // Normalize page number and page size
                page = Math.Max(1, page);
                pageSize = Math.Clamp(pageSize > 0 ? pageSize : 10, 1, 100);

                _logger.LogInformation("Retrieving paged task items: Page {Page}, PageSize {PageSize}", page, pageSize);
                
                // Use AsNoTracking for better performance since we're just reading
                // Split count query to avoid loading all data for counting
                var countQuery = _context.TaskItems
                    .Where(t => !t.IsDeleted)
                    .AsNoTracking();

                var totalCount = await countQuery.CountAsync();

                // Optimized query with projection - only load needed fields and related data
                var items = await _context.TaskItems
                    .Where(t => !t.IsDeleted)
                    .Include(t => t.TaskItemTags)
                    .ThenInclude(tt => tt.Tag)
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .AsNoTracking() // No tracking for read-only operations
                    .ToListAsync();

                _logger.LogInformation("Retrieved {ItemCount} of {TotalCount} task items for page {Page}", 
                    items.Count, totalCount, page);

                return new PagedResult<TaskItem>
                {
                    Items = items,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalCount = totalCount
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving paged task items");
                throw;
            }
        }

        public async Task<TaskItem?> GetByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving task item with ID: {TaskId}", id);
                // Use AsNoTracking for read-only operations
                var task = await _context.TaskItems
                    .Where(t => !t.IsDeleted)
                    .Include(t => t.TaskItemTags)
                    .ThenInclude(tt => tt.Tag)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == id);
                
                if (task == null)
                    _logger.LogWarning("Task item with ID {TaskId} not found", id);
                else
                    _logger.LogInformation("Successfully retrieved task item with ID: {TaskId}", id);
                
                return task;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving task item with ID: {TaskId}", id);
                throw;
            }
        }

        public async Task<TaskItem> CreateAsync(TaskItem taskItem)
        {
            try
            {
                _logger.LogInformation("Creating new task item: {TaskTitle}", taskItem.Title);
                taskItem.CreatedAt = DateTime.UtcNow;
                _context.TaskItems.Add(taskItem);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully created task item with ID: {TaskId}", taskItem.Id);
                return taskItem;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task item: {TaskTitle}", taskItem.Title);
                throw;
            }
        }

        public async Task<TaskItem> UpdateAsync(TaskItem taskItem)
        {
            try
            {
                _logger.LogInformation("Updating task item with ID: {TaskId}", taskItem.Id);
                taskItem.UpdatedAt = DateTime.UtcNow;
                
                // Load the existing task with its tags
                var existingTask = await _context.TaskItems
                    .Include(t => t.TaskItemTags)
                    .FirstOrDefaultAsync(t => t.Id == taskItem.Id);

                if (existingTask == null)
                {
                    _logger.LogWarning("Task with ID {TaskId} not found for update", taskItem.Id);
                    throw new InvalidOperationException($"Task with ID {taskItem.Id} not found");
                }

                // Update properties
                existingTask.Title = taskItem.Title;
                existingTask.Description = taskItem.Description;
                existingTask.DueDate = taskItem.DueDate;
                existingTask.Priority = taskItem.Priority;
                existingTask.FullName = taskItem.FullName;
                existingTask.Telephone = taskItem.Telephone;
                existingTask.Email = taskItem.Email;
                existingTask.UpdatedAt = taskItem.UpdatedAt;

                // Update tags - remove old ones and add new ones
                existingTask.TaskItemTags.Clear();
                foreach (var taskItemTag in taskItem.TaskItemTags)
                {
                    existingTask.TaskItemTags.Add(new TaskItemTag
                    {
                        TaskItemId = existingTask.Id,
                        TagId = taskItemTag.TagId
                    });
                }

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully updated task item with ID: {TaskId}", taskItem.Id);
                
                // Reload to get the updated tags
                return await GetByIdAsync(existingTask.Id) ?? existingTask;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task item with ID: {TaskId}", taskItem.Id);
                throw;
            }
        }

        public async Task<bool> DeleteAsync(int id)
        {
            try
            {
                _logger.LogInformation("Soft deleting task item with ID: {TaskId}", id);
                var taskItem = await _context.TaskItems.FindAsync(id);
                if (taskItem == null)
                {
                    _logger.LogWarning("Task item with ID {TaskId} not found for deletion", id);
                    return false;
                }

                taskItem.IsDeleted = true;
                taskItem.DeletedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully soft deleted task item with ID: {TaskId}", id);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting task item with ID: {TaskId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<TaskItem>> GetOverdueTasksAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving overdue task items");
                var now = DateTime.UtcNow;
                // Use AsNoTracking for read-only operations
                var overdueTasks = await _context.TaskItems
                    .Where(t => !t.IsDeleted && t.DueDate < now)
                    .Include(t => t.TaskItemTags)
                    .ThenInclude(tt => tt.Tag)
                    .AsNoTracking()
                    .ToListAsync();
                _logger.LogInformation("Found {OverdueCount} overdue task items", overdueTasks.Count());
                return overdueTasks;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving overdue task items");
                throw;
            }
        }
    }
}
