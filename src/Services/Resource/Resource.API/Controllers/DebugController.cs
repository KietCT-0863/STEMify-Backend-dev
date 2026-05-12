using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Resource.Infrastructure.Persistence;

namespace Resource.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DebugController : ControllerBase
{
    private readonly ResourceDbContext _context;
    private readonly ILogger<DebugController> _logger;

    public DebugController(ResourceDbContext context, ILogger<DebugController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet("lesson/{lessonId}")]
    public async Task<IActionResult> CheckLesson(int lessonId)
    {
        _logger.LogInformation("Checking lesson {LessonId}", lessonId);

        var lesson = await _context.Lessons
            .Where(l => l.Id == lessonId)
            .Select(l => new
            {
                l.Id,
                l.Title,
                l.OrderIndex,
                l.CourseId,
                l.CreatedDate
            })
            .FirstOrDefaultAsync();

        if (lesson == null)
        {
            return NotFound(new { message = $"Lesson {lessonId} not found" });
        }

        var assets = await _context.LessonAssets
            .Where(a => a.LessonId == lessonId)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.Type,
                a.AssetUrl,
                a.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            lesson,
            assetCount = assets.Count,
            assets,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("lesson/{lessonId}/assets")]
    public async Task<IActionResult> CheckLessonAssets(
        int lessonId,
        [FromQuery] string? type = null,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        _logger.LogInformation(
            "Checking lesson assets - LessonId: {LessonId}, Type: {Type}, Page: {PageNumber}, Size: {PageSize}",
            lessonId, type ?? "all", pageNumber, pageSize);

        var query = _context.LessonAssets
            .Where(a => a.LessonId == lessonId);

        if (!string.IsNullOrEmpty(type))
        {
            query = query.Where(a => a.Type.ToLower() == type.ToLower());
        }

        var totalCount = await query.CountAsync();
        var assets = await query
            .OrderByDescending(a => a.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new
            {
                a.Id,
                a.Name,
                a.Type,
                a.AssetUrl,
                a.Width,
                a.Height,
                a.CreatedAt
            })
            .ToListAsync();

        return Ok(new
        {
            lessonId,
            type,
            pageNumber,
            pageSize,
            totalCount,
            totalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            items = assets,
            timestamp = DateTime.UtcNow
        });
    }

    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            service = "Resource API Debug",
            timestamp = DateTime.UtcNow,
            database = _context.Database.CanConnect() ? "connected" : "disconnected"
        });
    }
}
