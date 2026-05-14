using MediatR;
using Microsoft.AspNetCore.Mvc;
using Resource.Application.Commands.Section;
using Resource.Application.Queries.Section;

namespace Resource.API.Controllers;

[ApiController]
[Route("api/sections")]
public class SectionController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<SectionController> _logger;

    public SectionController(IMediator mediator, ILogger<SectionController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get section by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSection(int id)
    {
        try
        {
            var query = new GetSectionByIdQuery(id);
            var result = await _mediator.Send(query);

            if (result == null)
                return NotFound(new { message = $"Section with ID {id} not found." });

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting section {SectionId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the section" });
        }
    }

    /// <summary>
    /// Query sections with filters
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> QuerySections(
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? orderBy = null,
        [FromQuery] int? sortDirection = null,
        [FromQuery] int? lessonId = null,
        [FromQuery] string? status = null)
    {
        try
        {
            Resource.Domain.Enums.SectionStatus? statusEnum = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (Enum.TryParse<Resource.Domain.Enums.SectionStatus>(status, true, out var parsedStatus))
                {
                    statusEnum = parsedStatus;
                }
            }

            var query = new QuerySectionsQuery
            {
                Search = search,
                PageNumber = pageNumber,
                PageSize = pageSize,
                OrderBy = orderBy,
                SortDirection = sortDirection,
                LessonId = lessonId,
                Status = statusEnum
            };

            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying sections");
            return StatusCode(500, new { message = "An error occurred while querying sections" });
        }
    }

    /// <summary>
    /// Create new section
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateSection([FromBody] CreateSectionCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetSection), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating section");
            return StatusCode(500, new { message = "An error occurred while creating the section" });
        }
    }

    /// <summary>
    /// Update section
    /// </summary>
    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateSection(int id, [FromBody] UpdateSectionCommand command)
    {
        try
        {
            if (id != command.Id)
                return BadRequest(new { message = "ID mismatch" });

            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Section not found: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating section {SectionId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the section" });
        }
    }

    /// <summary>
    /// Delete section (soft delete for Published, hard delete for Draft)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteSection(int id)
    {
        try
        {
            _logger.LogInformation("Attempting to delete section {SectionId}", id);
            var command = new DeleteSectionCommand { Id = id };
            await _mediator.Send(command);
            _logger.LogInformation("Successfully deleted section {SectionId}", id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Section not found: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting section {SectionId}. Exception: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}", 
                id, ex.GetType().Name, ex.Message, ex.StackTrace);
            return StatusCode(500, new { message = "An error occurred while deleting the section", error = ex.Message });
        }
    }

    /// <summary>
    /// Update sections order within a lesson
    /// </summary>
    [HttpPatch("/api/lessons/{lessonId}/sections-reorder")]
    public async Task<IActionResult> UpdateSectionsOrder(int lessonId, [FromBody] UpdateSectionsOrderCommand command)
    {
        try
        {
            if (lessonId != command.LessonId)
                return BadRequest(new { message = "Lesson ID mismatch" });

            await _mediator.Send(command);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating sections order for lesson {LessonId}", lessonId);
            return StatusCode(500, new { message = "An error occurred while updating sections order" });
        }
    }
}
