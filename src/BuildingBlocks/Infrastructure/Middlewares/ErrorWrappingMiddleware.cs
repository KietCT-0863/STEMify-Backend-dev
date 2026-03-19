using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Exceptions;
using Shared.SeedWork;

namespace Infrastructure.Middlewares
{
    public class ErrorWrappingMiddleware
    {
        private readonly IHostEnvironment _env;
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorWrappingMiddleware> _logger;

        public ErrorWrappingMiddleware(
            IHostEnvironment env,
            RequestDelegate next,
            ILogger<ErrorWrappingMiddleware> logger
        )
        {
            _env = env;
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                // Pass the request to the next middleware in the pipeline
                await _next(context);
            }
            catch (NotFoundException ex)
            {
                await HandleExceptionAsync(context, ex, StatusCodes.Status404NotFound);
            }
            catch (ArgumentException ex)
            {
                await HandleExceptionAsync(context, ex, StatusCodes.Status400BadRequest);
            }
            catch (FormatException ex)
            {
                await HandleExceptionAsync(context, ex, StatusCodes.Status400BadRequest);
            }
            catch (InvalidOperationException ex)
            {
                await HandleExceptionAsync(context, ex, StatusCodes.Status400BadRequest);
            }
            catch (FluentValidation.ValidationException ex)
            {
                _logger.LogWarning(ex, "Validation failed: {Errors}", ex.Errors);
                var errorMessages = ex
                    .Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}")
                    .ToList();

                var result = new ApiErrorResult(errorMessages, StatusCodes.Status400BadRequest);
                await WriteToResponse(context, StatusCodes.Status400BadRequest, result);
            }
            catch (Exception ex)
            {
                await HandleExceptionAsync(context, ex, StatusCodes.Status500InternalServerError);
            }
        }

        // Format the error response
        private async Task HandleExceptionAsync(HttpContext context, Exception ex, int statusCode)
        {
            _logger.LogError(ex, ex.Message);
            var errorResult = _env.IsDevelopment()
                ? new ApiErrorResult(ex.Message, statusCode, ex.StackTrace)
                : new ApiErrorResult(ex.Message, statusCode, "An unexpected error occurred.");

            await WriteToResponse(context, statusCode, errorResult);
        }

        private async Task WriteToResponse(
            HttpContext context,
            int statusCode,
            ApiErrorResult errorResult
        )
        {
            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(errorResult);
        }
    }
}
