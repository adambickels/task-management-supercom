using Microsoft.AspNetCore.Mvc;
using TaskManagement.Core.DTOs;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Interfaces;

namespace TaskManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskItemRepository _taskRepository;
        private readonly ITagRepository _tagRepository;
        private readonly ILogger<TasksController> _logger;

        public TasksController(
            ITaskItemRepository taskRepository,
            ITagRepository tagRepository,
            ILogger<TasksController> logger)
        {
            _taskRepository = taskRepository;
            _tagRepository = tagRepository;
            _logger = logger;
        }

        // GET: api/tasks
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TaskItemDto>>> GetAllTasks()
        {
            try
            {
                var tasks = await _taskRepository.GetAllAsync();
                var taskDtos = tasks.Select(MapToDto);
                return Ok(taskDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all tasks");
                return StatusCode(500, "An error occurred while retrieving tasks");
            }
        }

        // GET: api/tasks/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TaskItemDto>> GetTask(int id)
        {
            try
            {
                var task = await _taskRepository.GetByIdAsync(id);
                if (task == null)
                {
                    return NotFound($"Task with ID {id} not found");
                }

                return Ok(MapToDto(task));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving task with ID {TaskId}", id);
                return StatusCode(500, "An error occurred while retrieving the task");
            }
        }

        // POST: api/tasks
        [HttpPost]
        public async Task<ActionResult<TaskItemDto>> CreateTask([FromBody] TaskItemDto taskDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                // Validate that tags exist
                var tags = await _tagRepository.GetTagsByIdsAsync(taskDto.TagIds);
                if (tags.Count() != taskDto.TagIds.Count)
                {
                    return BadRequest("One or more tag IDs are invalid");
                }

                var taskItem = MapToEntity(taskDto);
                var createdTask = await _taskRepository.CreateAsync(taskItem);
                var createdDto = MapToDto(createdTask);

                return CreatedAtAction(nameof(GetTask), new { id = createdDto.Id }, createdDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task");
                return StatusCode(500, "An error occurred while creating the task");
            }
        }

        // PUT: api/tasks/5
        [HttpPut("{id}")]
        public async Task<ActionResult<TaskItemDto>> UpdateTask(int id, [FromBody] TaskItemDto taskDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (id != taskDto.Id)
                {
                    return BadRequest("ID in URL does not match ID in body");
                }

                var existingTask = await _taskRepository.GetByIdAsync(id);
                if (existingTask == null)
                {
                    return NotFound($"Task with ID {id} not found");
                }

                // Validate that tags exist
                var tags = await _tagRepository.GetTagsByIdsAsync(taskDto.TagIds);
                if (tags.Count() != taskDto.TagIds.Count)
                {
                    return BadRequest("One or more tag IDs are invalid");
                }

                var taskItem = MapToEntity(taskDto);
                var updatedTask = await _taskRepository.UpdateAsync(taskItem);
                var updatedDto = MapToDto(updatedTask);

                return Ok(updatedDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task with ID {TaskId}", id);
                return StatusCode(500, "An error occurred while updating the task");
            }
        }

        // DELETE: api/tasks/5
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTask(int id)
        {
            try
            {
                var result = await _taskRepository.DeleteAsync(id);
                if (!result)
                {
                    return NotFound($"Task with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting task with ID {TaskId}", id);
                return StatusCode(500, "An error occurred while deleting the task");
            }
        }

        private TaskItemDto MapToDto(TaskItem task)
        {
            return new TaskItemDto
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                DueDate = task.DueDate,
                Priority = task.Priority,
                FullName = task.FullName,
                Telephone = task.Telephone,
                Email = task.Email,
                CreatedAt = task.CreatedAt,
                UpdatedAt = task.UpdatedAt,
                TagIds = task.TaskItemTags.Select(tt => tt.TagId).ToList(),
                Tags = task.TaskItemTags.Select(tt => new TagDto
                {
                    Id = tt.Tag.Id,
                    Name = tt.Tag.Name
                }).ToList()
            };
        }

        private TaskItem MapToEntity(TaskItemDto dto)
        {
            return new TaskItem
            {
                Id = dto.Id,
                Title = dto.Title,
                Description = dto.Description,
                DueDate = dto.DueDate,
                Priority = dto.Priority,
                FullName = dto.FullName,
                Telephone = dto.Telephone,
                Email = dto.Email,
                TaskItemTags = dto.TagIds.Select(tagId => new TaskItemTag
                {
                    TagId = tagId
                }).ToList()
            };
        }
    }
}
