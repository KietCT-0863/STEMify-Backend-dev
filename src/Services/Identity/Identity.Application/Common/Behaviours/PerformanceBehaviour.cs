using System.Diagnostics;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Identity.Application.Common.Behaviours;

/// <summary>
/// Performance monitoring behavior for MediatR requests
/// </summary>
/// <typeparam name="TRequest"></typeparam>
/// <typeparam name="TResponse"></typeparam>
public class PerformanceBehaviour<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly Stopwatch _timer;
    private readonly ILogger<PerformanceBehaviour<TRequest, TResponse>> _logger;

    public PerformanceBehaviour(ILogger<PerformanceBehaviour<TRequest, TResponse>> logger)
    {
        _timer = new Stopwatch();
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        _timer.Start();

        var response = await next();

        _timer.Stop();

        var elapsedMilliseconds = _timer.ElapsedMilliseconds;

        if (elapsedMilliseconds > 500)
        {
            var requestName = typeof(TRequest).Name;

            // Safely serialize request, excluding IFormFile properties that can throw NullReferenceException
            var safeRequest = CreateSafeRequestForLogging(request);

            _logger.LogWarning(
                "Identity Long Running Request: {Name} ({ElapsedMilliseconds} milliseconds) {@Request}",
                requestName,
                elapsedMilliseconds,
                safeRequest
            );
        }

        return response;
    }

    /// <summary>
    /// Creates a safe version of the request for logging by replacing IFormFile with safe metadata
    /// </summary>
    private static object CreateSafeRequestForLogging(TRequest request)
    {
        // Use reflection to check if request contains IFormFile
        var requestType = typeof(TRequest);
        var formFileProperties = requestType.GetProperties()
            .Where(p => typeof(IFormFile).IsAssignableFrom(p.PropertyType))
            .ToList();

        if (!formFileProperties.Any())
        {
            // No IFormFile properties, safe to serialize directly
            return request;
        }

        // Create a dictionary representation, replacing IFormFile with safe metadata
        var safeRequest = new Dictionary<string, object?>();
        var allProperties = requestType.GetProperties();

        foreach (var property in allProperties)
        {
            var value = property.GetValue(request);
            
            if (formFileProperties.Contains(property))
            {
                // Replace IFormFile with safe metadata
                if (value is IFormFile formFile)
                {
                    try
                    {
                        safeRequest[property.Name] = new
                        {
                            FileName = GetSafeFileName(formFile),
                            Length = GetSafeLength(formFile),
                            Name = GetSafeName(formFile),
                            ContentType = GetSafeContentType(formFile),
                            ContentDisposition = GetSafeContentDisposition(formFile)
                        };
                    }
                    catch
                    {
                        // If any property access fails, just log that file exists
                        safeRequest[property.Name] = new { FileExists = true, Error = "Unable to read file metadata" };
                    }
                }
                else
                {
                    safeRequest[property.Name] = null;
                }
            }
            else
            {
                safeRequest[property.Name] = value;
            }
        }

        return safeRequest;
    }

    private static string? GetSafeContentType(IFormFile? formFile)
    {
        if (formFile == null)
            return null;

        try
        {
            // ContentType getter can throw NullReferenceException if internal state is invalid
            // Use null-conditional operator and null-coalescing for safety
            return formFile.ContentType ?? "unknown";
        }
        catch (NullReferenceException)
        {
            // Internal state of IFormFile may be invalid
            return null;
        }
        catch
        {
            // Catch any other exceptions
            return null;
        }
    }

    private static string? GetSafeContentDisposition(IFormFile? formFile)
    {
        if (formFile == null)
            return null;

        try
        {
            // ContentDisposition getter can throw NullReferenceException if internal state is invalid
            return formFile.ContentDisposition;
        }
        catch (NullReferenceException)
        {
            // Internal state of IFormFile may be invalid
            return null;
        }
        catch
        {
            // Catch any other exceptions
            return null;
        }
    }

    private static string? GetSafeFileName(IFormFile? formFile)
    {
        if (formFile == null)
            return null;

        try
        {
            return formFile.FileName;
        }
        catch
        {
            return null;
        }
    }

    private static long GetSafeLength(IFormFile? formFile)
    {
        if (formFile == null)
            return 0;

        try
        {
            return formFile.Length;
        }
        catch
        {
            return 0;
        }
    }

    private static string? GetSafeName(IFormFile? formFile)
    {
        if (formFile == null)
            return null;

        try
        {
            return formFile.Name;
        }
        catch
        {
            return null;
        }
    }
}
