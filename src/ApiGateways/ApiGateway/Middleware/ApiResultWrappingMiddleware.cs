using Shared.SeedWork;
using System.Text;
using System.Text.Json;

namespace ApiGateway.Middleware
{
    public class ApiResultWrappingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ApiResultWrappingMiddleware> _logger;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public ApiResultWrappingMiddleware(RequestDelegate next, ILogger<ApiResultWrappingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
            _jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            };
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLower();

            // Skip wrapping for non-API endpoints
            if (ShouldSkipWrapping(path))
            {
                await _next(context);
                return;
            }

            var originalBodyStream = context.Response.Body;
            using var memoryStream = new MemoryStream();
            context.Response.Body = memoryStream;

            try
            {
                await _next(context);

                // Reset stream position to read the response
                memoryStream.Seek(0, SeekOrigin.Begin);
                var responseBody = await new StreamReader(memoryStream, Encoding.UTF8).ReadToEndAsync();

                // Restore original body stream
                context.Response.Body = originalBodyStream;

                // Don't modify responses that have already been processed by ErrorWrappingMiddleware
                if (IsAlreadyWrappedResponse(responseBody))
                {
                    await WriteRawResponse(context, responseBody);
                    return;
                }

             
                if (ContainsRedirectUrl(responseBody, out var redirectUrl))
                {
                    context.Response.Clear();
                    context.Response.StatusCode = StatusCodes.Status302Found;
                    context.Response.Headers["Location"] = redirectUrl!;
                    return;
                }

                await WriteWrappedResponse(context, responseBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error in ApiResultWrappingMiddleware - Request: {Method} {Path} - Message: {Message}",
                    context.Request.Method,
                    context.Request.Path,
                    ex.Message);

                // Restore original body stream
                context.Response.Body = originalBodyStream;

                // Create error response if response hasn't started
                if (!context.Response.HasStarted)
                {
                    await WriteErrorResponse(context, ex);
                }
            }
        }

        private static bool ShouldSkipWrapping(string? path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            var skipPaths = new[]
            {
                "/swagger", 
                "/swagger-ui",
                "/favicon",
                "/health",
                "/metrics",
                "/api/ai/",
                "/api/invitations/accept-oauth",  
                "/api/auth/google/callback"       
            };

            var skipExtensions = new[]
            {
                ".css", ".js", ".png", ".jpg", ".jpeg", ".gif", ".ico",
                ".html", ".htm", ".svg", ".woff", ".woff2", ".ttf", ".eot"
            };

            return skipPaths.Any(skipPath => path.StartsWith(skipPath, StringComparison.OrdinalIgnoreCase)) ||
                   skipExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsAlreadyWrappedResponse(string responseBody)
        {
            if (string.IsNullOrWhiteSpace(responseBody))
                return false;

            try
            {
                using var document = JsonDocument.Parse(responseBody);
                var root = document.RootElement;

                // Check if it looks like an ApiErrorResult or ApiSuccessResult
                return (root.TryGetProperty("success", out _) && root.TryGetProperty("statusCode", out _)) ||
                       (root.TryGetProperty("message", out _) && root.TryGetProperty("statusCode", out _)) ||
                       (root.TryGetProperty("errors", out _) && root.TryGetProperty("statusCode", out _));
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private static bool ContainsRedirectUrl(string responseBody, out string? redirectUrl)
        {
            redirectUrl = null;
            
            if (string.IsNullOrWhiteSpace(responseBody))
                return false;

            try
            {
                using var document = JsonDocument.Parse(responseBody);
                var root = document.RootElement;

                // Check root level: redirect_url or redirectUrl
                if (root.TryGetProperty("redirect_url", out var redirectSnake) && redirectSnake.ValueKind == JsonValueKind.String)
                {
                    redirectUrl = redirectSnake.GetString();
                    return !string.IsNullOrWhiteSpace(redirectUrl);
                }
                
                if (root.TryGetProperty("redirectUrl", out var redirectCamel) && redirectCamel.ValueKind == JsonValueKind.String)
                {
                    redirectUrl = redirectCamel.GetString();
                    return !string.IsNullOrWhiteSpace(redirectUrl);
                }

                if (root.TryGetProperty("data", out var dataNode) && dataNode.ValueKind == JsonValueKind.Object)
                {
                    if (dataNode.TryGetProperty("redirect_url", out var dataRedirectSnake) && dataRedirectSnake.ValueKind == JsonValueKind.String)
                    {
                        redirectUrl = dataRedirectSnake.GetString();
                        return !string.IsNullOrWhiteSpace(redirectUrl);
                    }
                    
                    if (dataNode.TryGetProperty("redirectUrl", out var dataRedirectCamel) && dataRedirectCamel.ValueKind == JsonValueKind.String)
                    {
                        redirectUrl = dataRedirectCamel.GetString();
                        return !string.IsNullOrWhiteSpace(redirectUrl);
                    }
                }

                return false;
            }
            catch (JsonException)
            {
                return false;
            }
        }

        private async Task WriteWrappedResponse(HttpContext context, string responseBody)
        {
            context.Response.ContentType = "application/json";

            if (IsSuccessStatusCode(context.Response.StatusCode))
            {
                await WriteSuccessResponse(context, responseBody);
            }
            else
            {
                await WriteErrorResponseFromBody(context, responseBody);
            }
        }

        private async Task WriteSuccessResponse(HttpContext context, string responseBody)
        {
            try
            {
                object? result = null;

                if (!string.IsNullOrWhiteSpace(responseBody))
                {
                    try
                    {
                        // Try to parse as JSON first
                        result = JsonSerializer.Deserialize<JsonElement>(responseBody);
                    }
                    catch (JsonException)
                    {
                        // If not valid JSON, treat as string
                        result = responseBody;
                        _logger.LogDebug("Response body is not valid JSON, wrapping as string");
                    }
                }

                var wrappedResponse = new ApiSuccessResult<object>(result, context.Response.StatusCode);
                var json = JsonSerializer.Serialize(wrappedResponse, _jsonSerializerOptions);

                await context.Response.WriteAsync(json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write success response");
                await WriteErrorResponse(context, ex);
            }
        }

        private async Task WriteErrorResponseFromBody(HttpContext context, string responseBody)
        {
            try
            {
                var errorMessages = new List<string>();

                if (!string.IsNullOrWhiteSpace(responseBody))
                {
                    try
                    {
                        // Try to parse the error response body
                        using var document = JsonDocument.Parse(responseBody);
                        var root = document.RootElement;

                        // Extract error information from various possible formats
                        if (root.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var element in root.EnumerateArray())
                            {
                                errorMessages.Add(element.GetString() ?? "Unknown error");
                            }
                        }
                        else if (root.ValueKind == JsonValueKind.Object)
                        {
                            // Try common error properties
                            if (root.TryGetProperty("message", out var messageElement))
                            {
                                errorMessages.Add(messageElement.GetString() ?? "Unknown error");
                            }
                            else if (root.TryGetProperty("error", out var errorElement))
                            {
                                errorMessages.Add(errorElement.GetString() ?? "Unknown error");
                            }
                            else if (root.TryGetProperty("title", out var titleElement))
                            {
                                errorMessages.Add(titleElement.GetString() ?? "Unknown error");
                            }
                            else
                            {
                                errorMessages.Add(responseBody);
                            }
                        }
                        else
                        {
                            errorMessages.Add(root.GetString() ?? responseBody);
                        }
                    }
                    catch (JsonException)
                    {
                        // Not JSON, add as plain text
                        errorMessages.Add(responseBody);
                    }
                }

                if (!errorMessages.Any())
                {
                    errorMessages.Add(GetDefaultErrorMessage(context.Response.StatusCode));
                }

                var errorResult = errorMessages.Count == 1
                    ? new ApiErrorResult(
                        message: errorMessages.First(),
                        statusCode: context.Response.StatusCode,
                        details: null)
                    : new ApiErrorResult(errorMessages, context.Response.StatusCode);

                var json = JsonSerializer.Serialize(errorResult, _jsonSerializerOptions);
                await context.Response.WriteAsync(json, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write error response from body");
                await WriteErrorResponse(context, ex);
            }
        }

        private async Task WriteErrorResponse(HttpContext context, Exception ex)
        {
            try
            {
                if (context.Response.HasStarted)
                {
                    _logger.LogWarning("Cannot write error response - response has already started");
                    return;
                }

                context.Response.Clear();
                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                var errorResult = new ApiErrorResult(
                    message: "An unexpected error occurred while processing the response",
                    statusCode: StatusCodes.Status500InternalServerError,
                    details: ex.Message
                );

                var json = JsonSerializer.Serialize(errorResult, _jsonSerializerOptions);
                await context.Response.WriteAsync(json, Encoding.UTF8);
            }
            catch (Exception writeEx)
            {
                _logger.LogError(writeEx, "Failed to write fallback error response");
                // Last resort - try to write plain text
                try
                {
                    await context.Response.WriteAsync("Internal Server Error");
                }
                catch
                {
                    // Nothing more we can do
                }
            }
        }

        private async Task WriteRawResponse(HttpContext context, string responseBody)
        {
            try
            {
                await context.Response.WriteAsync(responseBody, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write raw response");
            }
        }

        private static bool IsSuccessStatusCode(int statusCode)
        {
            return statusCode >= 200 && statusCode <= 299;
        }

        private static string GetDefaultErrorMessage(int statusCode)
        {
            return statusCode switch
            {
                400 => "Bad Request",
                401 => "Unauthorized",
                403 => "Forbidden",
                404 => "Not Found",
                405 => "Method Not Allowed",
                409 => "Conflict",
                422 => "Unprocessable Entity",
                429 => "Too Many Requests",
                500 => "Internal Server Error",
                501 => "Not Implemented",
                502 => "Bad Gateway",
                503 => "Service Unavailable",
                504 => "Gateway Timeout",
                _ => "An error occurred"
            };
        }
    }
}