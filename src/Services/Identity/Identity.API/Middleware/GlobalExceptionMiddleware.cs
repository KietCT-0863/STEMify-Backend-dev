using System.Net;
using System.Text.Json;
using Identity.Application.Common.Exceptions;
using Shared.SeedWork;

namespace Identity.API.Middleware;

/// <summary>
/// Global exception handling middleware for consistent error responses
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger
    )
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var response = (object)(
            exception switch
            {
                NotFoundException notFoundException => new ApiResult<string>
                {
                    IsSucceeded = false,
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Message = notFoundException.Message,
                    Data = null,
                },
                ValidationException validationException => new ApiResult<object>
                {
                    IsSucceeded = false,
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = "Validation failed",
                    Data = validationException.Errors,
                },
                ForbiddenAccessException forbiddenException => new ApiResult<string>
                {
                    IsSucceeded = false,
                    StatusCode = (int)HttpStatusCode.Forbidden,
                    Message = forbiddenException.Message,
                    Data = null,
                },
                InvalidOperationException invalidOpException => new ApiResult<string>
                {
                    IsSucceeded = false,
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = invalidOpException.Message,
                    Data = null,
                },
                NotImplementedException notImplementedException => new ApiResult<string>
                {
                    IsSucceeded = false,
                    StatusCode = (int)HttpStatusCode.NotImplemented,
                    Message = notImplementedException.Message,
                    Data = null,
                },
                ArgumentException argException => new ApiResult<string>
                {
                    IsSucceeded = false,
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Message = argException.Message,
                    Data = null,
                },
                UnauthorizedAccessException unauthorizedException => new ApiResult<string>
                {
                    IsSucceeded = false,
                    StatusCode = (int)HttpStatusCode.Unauthorized,
                    Message = unauthorizedException.Message,
                    Data = null,
                },
                _ => new ApiResult<string>
                {
                    IsSucceeded = false,
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Message = "An internal server error occurred",
                    Data = null,
                },
            }
        );

        context.Response.StatusCode = ((ApiResult)response).StatusCode;

        // Log the exception
        var statusCode = ((ApiResult)response).StatusCode;
        if (statusCode >= 500)
        {
            _logger.LogError(
                exception,
                "Internal server error occurred: {Message}",
                exception.Message
            );
        }
        else if (statusCode >= 400)
        {
            _logger.LogWarning(exception, "Client error occurred: {Message}", exception.Message);
        }

        var jsonResponse = JsonSerializer.Serialize(
            response,
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
        );

        await context.Response.WriteAsync(jsonResponse);
    }
}
