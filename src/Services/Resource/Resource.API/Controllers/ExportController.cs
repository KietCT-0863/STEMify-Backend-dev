using MediatR;
using Microsoft.AspNetCore.Mvc;
using Resource.Application.Queries.Exporter;

namespace Resource.API.Controllers;

[ApiController]
[Route("api/courses")]
public class ExportController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ExportController> _logger;

    public ExportController(IMediator mediator, ILogger<ExportController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Export course to RSA format
    /// </summary>
    [HttpGet("{courseId}/export")]
    public async Task<IActionResult> ExportCourse(int courseId)
    {
        try
        {
            var query = new GetExportedCourse(courseId);
            var result = await _mediator.Send(query);

            // Convert ByteString to base64 for JSON response
            var base64ZipData = Convert.ToBase64String(result.ZipData.ToByteArray());

            return Ok(new
            {
                zipData = base64ZipData,
                filename = result.Filename
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Course not found: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting course {CourseId}", courseId);
            return StatusCode(500, new { message = "An error occurred while exporting the course" });
        }
    }

    /// <summary>
    /// Export lesson to RSA format
    /// </summary>
    [HttpGet("/api/lessons/{lessonId}/export")]
    public async Task<IActionResult> ExportLesson(int lessonId)
    {
        try
        {
            var query = new GetExportedLesson(lessonId);
            var result = await _mediator.Send(query);

            // Convert ByteString to base64 for JSON response
            var base64ZipData = Convert.ToBase64String(result.ZipData.ToByteArray());

            return Ok(new
            {
                zipData = base64ZipData,
                filename = result.Filename
            });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Lesson not found: {Message}", ex.Message);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting lesson {LessonId}", lessonId);
            return StatusCode(500, new { message = "An error occurred while exporting the lesson" });
        }
    }
}
