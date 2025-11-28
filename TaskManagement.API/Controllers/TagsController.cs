using Microsoft.AspNetCore.Mvc;
using AutoMapper;
using TaskManagement.Core.DTOs;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Interfaces;
using Asp.Versioning;

namespace TaskManagement.API.Controllers
{
    /// <summary>
    /// Controller for managing tags
    /// </summary>
    [ApiController]
    [ApiVersion("1.0")]
    [Route("api/v{version:apiVersion}/[controller]")]
    public class TagsController : ControllerBase
    {
        private readonly ITagRepository _tagRepository;
        private readonly ILogger<TagsController> _logger;
        private readonly IMapper _mapper;

        public TagsController(ITagRepository tagRepository, ILogger<TagsController> logger, IMapper mapper)
        {
            _tagRepository = tagRepository;
            _logger = logger;
            _mapper = mapper;
        }

        /// <summary>
        /// Get all tags
        /// </summary>
        /// <returns>List of all tags</returns>
        /// <response code="200">Returns the list of tags</response>
        [HttpGet]
        [ResponseCache(Duration = 300)] // Cache for 5 minutes
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<TagDto>>> GetAllTags()
        {
            try
            {
                var tags = await _tagRepository.GetAllAsync();
                var tagDtos = _mapper.Map<IEnumerable<TagDto>>(tags);
                return Ok(tagDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all tags");
                throw;
            }
        }

        /// <summary>
        /// Get a specific tag by ID
        /// </summary>
        /// <param name="id">The tag ID</param>
        /// <returns>The tag details</returns>
        /// <response code="200">Returns the tag</response>
        /// <response code="404">If the tag is not found</response>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TagDto>> GetTag(int id)
        {
            try
            {
                var tag = await _tagRepository.GetByIdAsync(id);
                if (tag == null)
                {
                    return NotFound($"Tag with ID {id} not found");
                }

                var tagDto = _mapper.Map<TagDto>(tag);
                return Ok(tagDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tag with ID {TagId}", id);
                throw;
            }
        }

        /// <summary>
        /// Create a new tag
        /// </summary>
        /// <param name="tagDto">The tag data</param>
        /// <returns>The created tag</returns>
        /// <response code="201">Returns the newly created tag</response>
        /// <response code="400">If the tag data is invalid</response>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<TagDto>> CreateTag([FromBody] TagDto tagDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var tag = _mapper.Map<Tag>(tagDto);
                var createdTag = await _tagRepository.CreateAsync(tag);
                var createdDto = _mapper.Map<TagDto>(createdTag);

                return CreatedAtAction(nameof(GetTag), new { version = "1.0", id = createdDto.Id }, createdDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tag");
                throw;
            }
        }

        /// <summary>
        /// Update an existing tag
        /// </summary>
        /// <param name="id">The tag ID</param>
        /// <param name="tagDto">The updated tag data</param>
        /// <returns>The updated tag</returns>
        /// <response code="200">Returns the updated tag</response>
        /// <response code="400">If the tag data is invalid or IDs don't match</response>
        /// <response code="404">If the tag is not found</response>
        [HttpPut("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TagDto>> UpdateTag(int id, [FromBody] TagDto tagDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                if (id != tagDto.Id)
                {
                    return BadRequest("ID in URL does not match ID in body");
                }

                var existingTag = await _tagRepository.GetByIdAsync(id);
                if (existingTag == null)
                {
                    return NotFound($"Tag with ID {id} not found");
                }

                existingTag.Name = tagDto.Name;
                var updatedTag = await _tagRepository.UpdateAsync(existingTag);
                var updatedDto = _mapper.Map<TagDto>(updatedTag);

                return Ok(updatedDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tag with ID {TagId}", id);
                throw;
            }
        }

        /// <summary>
        /// Delete a tag
        /// </summary>
        /// <param name="id">The tag ID</param>
        /// <returns>No content</returns>
        /// <response code="204">If the tag was successfully deleted</response>
        /// <response code="404">If the tag is not found</response>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteTag(int id)
        {
            try
            {
                var result = await _tagRepository.DeleteAsync(id);
                if (!result)
                {
                    return NotFound($"Tag with ID {id} not found");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tag with ID {TagId}", id);
                throw;
            }
        }
    }
}
