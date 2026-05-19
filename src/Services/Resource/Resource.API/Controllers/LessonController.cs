using MediatR;
using Microsoft.AspNetCore.Mvc;
using Resource.Application.Commands.Lesson;
using Resource.Application.Queries.Lesson;

namespace Resource.API.Controllers;

[ApiController]
[Route("api/lessons")]
public class LessonController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<LessonController> _logger;

    public LessonController(IMediator mediator, ILogger<LessonController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get lesson by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetLesson(int id)
    {
        try
        {
            var query = new GetLessonByIdQuery(id);
            var result = await _mediator.Send(query);

            if (result == null)
                return NotFound(new { message = $"Lesson with ID {id} not found." });

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Lesson not found: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting lesson {LessonId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the lesson" });
        }
    }

    /// <summary>
    /// Query lessons with filters
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> QueryLessons(
        [FromQuery] string? search,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? orderBy = null,
        [FromQuery] int? sortDirection = null,
        [FromQuery] int? courseId = null,
        [FromQuery] string? status = null,
        [FromQuery] int? duration = null,
        [FromQuery] int? ageRangeId = null,
        [FromQuery] int? topicId = null,
        [FromQuery] int? skillId = null,
        [FromQuery] int? standardId = null,
        [FromQuery] string? createdByUserId = null)
    {
        try
        {
            Resource.Domain.Enums.LessonStatus? statusEnum = null;
            if (!string.IsNullOrWhiteSpace(status))
            {
                if (Enum.TryParse<Resource.Domain.Enums.LessonStatus>(status, true, out var parsedStatus))
                {
                    statusEnum = parsedStatus;
                }
            }

            Shared.Enums.SortDirection? sortDirectionEnum = null;
            if (sortDirection.HasValue)
            {
                if (Enum.IsDefined(typeof(Shared.Enums.SortDirection), sortDirection.Value))
                {
                    sortDirectionEnum = (Shared.Enums.SortDirection)sortDirection.Value;
                }
            }

            var query = new QueryLessonsQuery
            {
                Search = search,
                PageNumber = pageNumber,
                PageSize = pageSize,
                OrderBy = orderBy,
                SortDirection = sortDirectionEnum,
                CourseId = courseId,
                Status = statusEnum,
                Duration = duration,
                AgeRangeId = ageRangeId,
                TopicId = topicId,
                SkillId = skillId,
                StandardId = standardId,
                CreatedByUserId = createdByUserId
            };

            var result = await _mediator.Send(query);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying lessons");
            return StatusCode(500, new { message = "An error occurred while querying lessons" });
        }
    }

    /// <summary>
    /// Create new lesson
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateLesson([FromBody] CreateLessonCommand command)
    {
        try
        {
            _logger.LogInformation("Creating lesson with Title: {Title}, CourseId: {CourseId}, CreatedByUserId: {UserId}, ImageBytes length: {ImageLength}", 
                command.Title, command.CourseId, command.CreatedByUserId, command.ImageBytes?.Length ?? 0);
            
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Model validation failed: {Errors}", 
                    string.Join(", ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return BadRequest(ModelState);
            }
            
            var result = await _mediator.Send(command);
            return CreatedAtAction(nameof(GetLesson), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating lesson");
            return StatusCode(500, new { message = "An error occurred while creating the lesson" });
        }
    }

    /// <summary>
    /// Update lesson
    /// </summary>
    [HttpPatch("{id}")]
    public async Task<IActionResult> UpdateLesson(int id, [FromBody] UpdateLessonCommand command)
    {
        try
        {
            // Auto-set ID from URL to avoid mismatch
            command.Id = id;

            var result = await _mediator.Send(command);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Lesson not found: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating lesson {LessonId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the lesson" });
        }
    }

    /// <summary>
    /// Delete lesson (soft delete for Published, hard delete for Draft)
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteLesson(int id)
    {
        try
        {
            _logger.LogInformation("Attempting to delete lesson {LessonId}", id);
            var command = new DeleteLessonCommand { Id = id };
            await _mediator.Send(command);
            _logger.LogInformation("Successfully deleted lesson {LessonId}", id);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning("Lesson not found: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting lesson {LessonId}. Exception: {ExceptionType}, Message: {Message}, StackTrace: {StackTrace}", 
                id, ex.GetType().Name, ex.Message, ex.StackTrace);
            return StatusCode(500, new { message = "An error occurred while deleting the lesson", error = ex.Message });
        }
    }

    /// <summary>
    /// Update lessons order within a course
    /// </summary>
    [HttpPatch("/api/courses/{courseId}/lessons-reorder")]
    public async Task<IActionResult> UpdateLessonsOrder(int courseId, [FromBody] UpdateLessonsOrderCommand command)
    {
        try
        {
            if (courseId != command.CourseId)
                return BadRequest(new { message = "Course ID mismatch" });

            await _mediator.Send(command);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating lessons order for course {CourseId}", courseId);
            return StatusCode(500, new { message = "An error occurred while updating lessons order" });
        }
    }
}
