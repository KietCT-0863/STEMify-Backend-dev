using MediatR;
using Microsoft.AspNetCore.Mvc;
using Resource.Application.Commands.Agent;
using System.Text;

namespace Resource.API.Controllers;

[ApiController]
[Route("api/ai")]
public class AIController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<AIController> _logger;

    public AIController(IMediator mediator, ILogger<AIController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Answer general STEM questions with streaming response
    /// </summary>
    [HttpPost("general-question")]
    public async Task AnswerGeneralStemQuestion([FromBody] GeneralQuestionRequest request)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("Connection", "keep-alive");
        Response.Headers.Add("X-Accel-Buffering", "no"); // Disable nginx buffering

        try
        {
            var command = new AnswerGeneralStemQuestionCommand
            {
                UserPrompt = request.UserPrompt
            };

            var stream = await _mediator.Send(command);

            await foreach (var chunk in stream)
            {
                // Send as Server-Sent Event
                //var message = $"data: {chunk}\n\n";
                await Response.WriteAsync(chunk, Encoding.UTF8);
                await Response.Body.FlushAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming answer for general STEM question");
            await Response.WriteAsync($"event: error\ndata: {ex.Message}\n\n");
            await Response.Body.FlushAsync();
        }
    }

    /// <summary>
    /// Generate course recommendations with streaming response
    /// </summary>
    [HttpPost("course-recommendations")]
    public async Task GenerateCourseRecommendation([FromBody] CourseRecommendationRequest request)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("Connection", "keep-alive");
        Response.Headers.Add("X-Accel-Buffering", "no");

        try
        {
            var command = new GenerateCourseRecommendationCommand
            {
                UserPrompt = request.UserPrompt
            };

            var stream = await _mediator.Send(command);

            await foreach (var chunk in stream)
            {
                //var message = $"data: {chunk}\n\n";
                await Response.WriteAsync(chunk, Encoding.UTF8);
                await Response.Body.FlushAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming course recommendations");
            await Response.WriteAsync($"event: error\ndata: {ex.Message}\n\n");
            await Response.Body.FlushAsync();
        }
    }

    /// <summary>
    /// Summarize lesson with streaming response
    /// </summary>
    [HttpPost("lesson-summary")]
    public async Task SummarizeLesson([FromBody] LessonSummaryRequest request)
    {
        Response.ContentType = "text/event-stream";
        Response.Headers.Add("Cache-Control", "no-cache");
        Response.Headers.Add("Connection", "keep-alive");
        Response.Headers.Add("X-Accel-Buffering", "no");

        try
        {
            var command = new SummaryLessonCommand
            {
                LessonId = request.LessonId
            };

            var stream = await _mediator.Send(command);

            await foreach (var chunk in stream)
            {
                //var message = $"data: {chunk}\n\n";
                await Response.WriteAsync(chunk, Encoding.UTF8);
                await Response.Body.FlushAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error streaming lesson summary");
            await Response.WriteAsync($"event: error\ndata: {ex.Message}\n\n");
            await Response.Body.FlushAsync();
        }
    }
}

// Request models
public class GeneralQuestionRequest
{
    public string UserPrompt { get; set; } = string.Empty;
}

public class CourseRecommendationRequest
{
    public string UserPrompt { get; set; } = string.Empty;
}

public class LessonSummaryRequest
{
    public int LessonId { get; set; }
}
