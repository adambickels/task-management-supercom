using System.Collections.Generic;
using System.Threading.Tasks;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Models;

namespace TaskManagement.Core.Interfaces
{
    public interface ITaskItemRepository
    {
        Task<IEnumerable<TaskItem>> GetAllAsync();
        Task<PagedResult<TaskItem>> GetPagedAsync(int page, int pageSize);
        Task<TaskItem?> GetByIdAsync(int id);
        Task<TaskItem> CreateAsync(TaskItem taskItem);
        Task<TaskItem> UpdateAsync(TaskItem taskItem);
        Task<bool> DeleteAsync(int id);
        Task<IEnumerable<TaskItem>> GetOverdueTasksAsync();
    }
}
