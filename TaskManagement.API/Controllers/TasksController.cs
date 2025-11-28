using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using TaskManagement.Core.DTOs;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Interfaces;
using TaskManagement.Core.Models;
using Asp.Versioning;

namespace TaskManagement.API.Controllers
{
    /// <summary>
    /// Controller for managing tasks
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly ITaskItemRepository _taskRepository;
        private readonly ITagRepository _tagRepository;
        private readonly ILogger<TasksController> _logger;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public TasksController(
            ITaskItemRepository taskRepository,
            ITagRepository tagRepository,
            ILogger<TasksController> logger,
            IMapper mapper,
            IConfiguration configuration)
        {
            _taskRepository = taskRepository;
            _tagRepository = tagRepository;
            _logger = logger;
            _mapper = mapper;
            _configuration = configuration;
        }

        /// <summary>
        /// Get all tasks (paginated)
        /// </summary>
        /// <param name="page">Page number (default: 1)</param>
        /// <param name="pageSize">Number of items per page (default: 10, max: 100)</param>
        /// <returns>Paginated list of tasks</returns>
        /// <response code="200">Returns the paginated list of tasks</response>
        /// <response code="500">If an error occurs while retrieving tasks</response>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PagedResult<TaskItemDto>>> GetAllTasks(
            [FromQuery] int page = 1, 
            [FromQuery] int? pageSize = null)
        {
            try
            {
                // Get pagination settings from configuration
                var defaultPageSize = _configuration.GetValue<int>("Pagination:DefaultPageSize", 10);
                var maxPageSize = _configuration.GetValue<int>("Pagination:MaxPageSize", 100);
                
                // Validate pagination parameters
                if (page < 1) page = 1;
                if (!pageSize.HasValue || pageSize < 1) pageSize = defaultPageSize;
                if (pageSize > maxPageSize) pageSize = maxPageSize;

                var pagedTasks = await _taskRepository.GetPagedAsync(page, pageSize.Value);
                var taskDtos = _mapper.Map<IEnumerable<TaskItemDto>>(pagedTasks.Items);

                var result = new PagedResult<TaskItemDto>
                {
                    Items = taskDtos,
                    CurrentPage = pagedTasks.CurrentPage,
                    PageSize = pagedTasks.PageSize,
                    TotalCount = pagedTasks.TotalCount
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all tasks");
                throw; // Let global exception handler deal with it
            }
        }

        /// <summary>
        /// Get a specific task by ID
        /// </summary>
        /// <param name="id">The task ID</param>
        /// <returns>The task details</returns>
        /// <response code="200">Returns the task</response>
        /// <response code="404">If the task is not found</response>
        /// <response code="500">If an error occurs while retrieving the task</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<TaskItemDto>> GetTask(int id)
        {
            try
            {
                var task = await _taskRepository.GetByIdAsync(id);
                if (task == null)
                {
                    return NotFound($"Task with ID {id} not found");
                }

                var taskDto = _mapper.Map<TaskItemDto>(task);
                return Ok(taskDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving task with ID {TaskId}", id);
                throw;
            }
        }

        /// <summary>
        /// Create a new task
        /// </summary>
        /// <param name="taskDto">The task data</param>
        /// <returns>The created task</returns>
        /// <response code="201">Returns the newly created task</response>
        /// <response code="400">If the task data is invalid</response>
        /// <response code="500">If an error occurs while creating the task</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

                var taskItem = _mapper.Map<TaskItem>(taskDto);
                taskItem.TaskItemTags = taskDto.TagIds.Select(tagId => new TaskItemTag
                {
                    TagId = tagId
                }).ToList();

                var createdTask = await _taskRepository.CreateAsync(taskItem);
                var createdDto = _mapper.Map<TaskItemDto>(createdTask);

                return CreatedAtAction(nameof(GetTask), new { version = "1.0", id = createdDto.Id }, createdDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task");
                throw;
            }
        }

        /// <summary>
        /// Update an existing task
        /// </summary>
        /// <param name="id">The task ID</param>
        /// <param name="taskDto">The updated task data</param>
        /// <returns>The updated task</returns>
        /// <response code="200">Returns the updated task</response>
        /// <response code="400">If the task data is invalid or IDs don't match</response>
        /// <response code="404">If the task is not found</response>
        /// <response code="500">If an error occurs while updating the task</response>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

                var taskItem = _mapper.Map<TaskItem>(taskDto);
                taskItem.TaskItemTags = taskDto.TagIds.Select(tagId => new TaskItemTag
                {
                    TagId = tagId
                }).ToList();

                var updatedTask = await _taskRepository.UpdateAsync(taskItem);
                var updatedDto = _mapper.Map<TaskItemDto>(updatedTask);

                return Ok(updatedDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating task with ID {TaskId}", id);
                throw;
            }
        }

        /// <summary>
        /// Delete a task (soft delete)
        /// </summary>
        /// <param name="id">The task ID</param>
        /// <returns>No content</returns>
        /// <response code="204">If the task was successfully deleted</response>
        /// <response code="404">If the task is not found</response>
        /// <response code="500">If an error occurs while deleting the task</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
                throw;
            }
        }
    }
}
