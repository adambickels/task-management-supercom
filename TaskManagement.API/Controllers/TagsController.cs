using Microsoft.AspNetCore.Mvc;
using TaskManagement.Core.DTOs;
using TaskManagement.Core.Entities;
using TaskManagement.Core.Interfaces;

namespace TaskManagement.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TagsController : ControllerBase
    {
        private readonly ITagRepository _tagRepository;
        private readonly ILogger<TagsController> _logger;

        public TagsController(ITagRepository tagRepository, ILogger<TagsController> logger)
        {
            _tagRepository = tagRepository;
            _logger = logger;
        }

        // GET: api/tags
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TagDto>>> GetAllTags()
        {
            try
            {
                var tags = await _tagRepository.GetAllAsync();
                var tagDtos = tags.Select(t => new TagDto
                {
                    Id = t.Id,
                    Name = t.Name
                });
                return Ok(tagDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all tags");
                return StatusCode(500, "An error occurred while retrieving tags");
            }
        }

        // GET: api/tags/5
        [HttpGet("{id}")]
        public async Task<ActionResult<TagDto>> GetTag(int id)
        {
            try
            {
                var tag = await _tagRepository.GetByIdAsync(id);
                if (tag == null)
                {
                    return NotFound($"Tag with ID {id} not found");
                }

                return Ok(new TagDto { Id = tag.Id, Name = tag.Name });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tag with ID {TagId}", id);
                return StatusCode(500, "An error occurred while retrieving the tag");
            }
        }

        // POST: api/tags
        [HttpPost]
        public async Task<ActionResult<TagDto>> CreateTag([FromBody] TagDto tagDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var tag = new Tag { Name = tagDto.Name };
                var createdTag = await _tagRepository.CreateAsync(tag);
                var createdDto = new TagDto { Id = createdTag.Id, Name = createdTag.Name };

                return CreatedAtAction(nameof(GetTag), new { id = createdDto.Id }, createdDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tag");
                return StatusCode(500, "An error occurred while creating the tag");
            }
        }

        // PUT: api/tags/5
        [HttpPut("{id}")]
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
                var updatedDto = new TagDto { Id = updatedTag.Id, Name = updatedTag.Name };

                return Ok(updatedDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tag with ID {TagId}", id);
                return StatusCode(500, "An error occurred while updating the tag");
            }
        }

        // DELETE: api/tags/5
        [HttpDelete("{id}")]
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
                return StatusCode(500, "An error occurred while deleting the tag");
            }
        }
    }
}
