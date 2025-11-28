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
                var tasks = await _context.TaskItems
                    .Include(t => t.TaskItemTags)
                    .ThenInclude(tt => tt.Tag)
                    .OrderByDescending(t => t.CreatedAt)
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

        public async Task<TaskItem?> GetByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving task item with ID: {TaskId}", id);
                var task = await _context.TaskItems
                    .Include(t => t.TaskItemTags)
                    .ThenInclude(tt => tt.Tag)
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
                _logger.LogInformation("Deleting task item with ID: {TaskId}", id);
                var taskItem = await _context.TaskItems.FindAsync(id);
                if (taskItem == null)
                {
                    _logger.LogWarning("Task item with ID {TaskId} not found for deletion", id);
                    return false;
                }

                _context.TaskItems.Remove(taskItem);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Successfully deleted task item with ID: {TaskId}", id);
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
                var overdueTasks = await _context.TaskItems
                    .Include(t => t.TaskItemTags)
                    .ThenInclude(tt => tt.Tag)
                    .Where(t => t.DueDate < now)
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
