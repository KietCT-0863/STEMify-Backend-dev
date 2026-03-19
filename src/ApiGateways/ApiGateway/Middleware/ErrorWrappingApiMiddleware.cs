using Grpc.Core;
using Shared.SeedWork;
using System.Text.Json;

namespace ApiGateway.Middleware
{
    public class ErrorWrappingApiMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorWrappingApiMiddleware> _logger;
        private readonly JsonSerializerOptions _jsonSerializerOptions;
        private readonly IHostEnvironment _env;

        public ErrorWrappingApiMiddleware(
            RequestDelegate next,
            ILogger<ErrorWrappingApiMiddleware> logger,
            IHostEnvironment env
        )
        {
            _next = next;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
            _env = env;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next.Invoke(context);
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogWarning(ex,
                    "Operation cancelled - Request: {Method} {Path} - Message: {Message}",
                    context.Request.Method,
                    context.Request.Path,
                    ex.Message);

                await HandleExceptionAsync(
                    context,
                    ex,
                    StatusCodes.Status504GatewayTimeout,
                    "Request timeout, please try again later.",
                    "The request was cancelled due to timeout"
                );
            }
            catch (RpcException ex)
            {
                _logger.LogError(ex,
                    "gRPC error - Request: {Method} {Path} - Status: {Status} - Detail: {Detail}",
                    context.Request.Method,
                    context.Request.Path,
                    ex.StatusCode,
                    ex.Status.Detail);

                var (statusCode, userMessage) = MapGrpcStatusToHttp(ex.StatusCode);

                var errorResult = new ApiErrorResult(
                    message: userMessage,
                    statusCode: statusCode,
                    details: _env.IsDevelopment() ?
                        $"gRPC Error: {ex.Status.Detail} (Status: {ex.StatusCode})" :
                        "Service temporarily unavailable"
                );

                await WriteToResponse(context, statusCode, errorResult);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "Validation error - Request: {Method} {Path} - Message: {Message}",
                    context.Request.Method,
                    context.Request.Path,
                    ex.Message);

                await HandleExceptionAsync(
                    context,
                    ex,
                    StatusCodes.Status400BadRequest,
                    "Invalid request parameters",
                    ex.Message
                );
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex,
                    "Unauthorized access - Request: {Method} {Path} - Message: {Message}",
                    context.Request.Method,
                    context.Request.Path,
                    ex.Message);

                await HandleExceptionAsync(
                    context,
                    ex,
                    StatusCodes.Status401Unauthorized,
                    "Unauthorized access",
                    "Access denied"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unhandled exception - Request: {Method} {Path} - Type: {ExceptionType} - Message: {Message}",
                    context.Request.Method,
                    context.Request.Path,
                    ex.GetType().Name,
                    ex.Message);

                await HandleExceptionAsync(
                    context,
                    ex,
                    StatusCodes.Status500InternalServerError,
                    "An unexpected error occurred",
                    _env.IsDevelopment() ? ex.Message : "Internal server error"
                );
            }
        }

        private static (int StatusCode, string UserMessage) MapGrpcStatusToHttp(StatusCode grpcStatus)
        {
            return grpcStatus switch
            {
                StatusCode.InvalidArgument => (StatusCodes.Status400BadRequest, "Invalid request parameters"),
                StatusCode.NotFound => (StatusCodes.Status404NotFound, "Resource not found"),
                StatusCode.AlreadyExists => (StatusCodes.Status409Conflict, "Resource already exists"),
                StatusCode.Unauthenticated => (StatusCodes.Status401Unauthorized, "Authentication required"),
                StatusCode.PermissionDenied => (StatusCodes.Status403Forbidden, "Access forbidden"),
                StatusCode.Unavailable => (StatusCodes.Status503ServiceUnavailable, "Service temporarily unavailable"),
                StatusCode.DeadlineExceeded => (StatusCodes.Status504GatewayTimeout, "Request timeout"),
                StatusCode.ResourceExhausted => (StatusCodes.Status429TooManyRequests, "Too many requests"),
                StatusCode.Cancelled => (StatusCodes.Status499ClientClosedRequest, "Request cancelled"),
                _ => (StatusCodes.Status500InternalServerError, "Internal server error"),
            };
        }

        private async Task HandleExceptionAsync(
            HttpContext context,
            Exception ex,
            int statusCode,
            string userMessage,
            string? details = null
        )
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Cannot write error response - response has already started");
                return;
            }

            var errorDetails = details ?? (_env.IsDevelopment() ?
                $"{ex.Message}\n{ex.StackTrace}" :
                "An error occurred while processing your request");

            var errorResult = new ApiErrorResult(
                message: userMessage,
                statusCode: statusCode,
                details: errorDetails
            );

            await WriteToResponse(context, statusCode, errorResult);
        }

        private async Task WriteToResponse(
            HttpContext context,
            int statusCode,
            ApiErrorResult errorResult
        )
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("Cannot write error response - response has already started");
                return;
            }

            try
            {
                context.Response.Clear();
                context.Response.StatusCode = statusCode;
                context.Response.ContentType = "application/json";

                var jsonResponse = JsonSerializer.Serialize(errorResult, _jsonSerializerOptions);
                await context.Response.WriteAsync(jsonResponse);

                _logger.LogDebug("Error response written - Status: {StatusCode}, Message: {Message}",
                    statusCode, errorResult.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write error response");
                // Fallback: try to write a minimal error response
                try
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("Internal Server Error");
                }
                catch
                {
                    // If even this fails, there's nothing more we can do
                }
            }
        }
    }
}