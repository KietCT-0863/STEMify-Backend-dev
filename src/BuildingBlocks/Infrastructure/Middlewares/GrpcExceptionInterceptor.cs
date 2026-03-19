using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Npgsql;
using Shared.Exceptions;
using System.Text.Json;

namespace Infrastructure.Middlewares
{
    public class GrpcExceptionInterceptor : Interceptor
    {
        private readonly ILogger<GrpcExceptionInterceptor> _logger;
        private readonly IHostEnvironment _env;
        private readonly JsonSerializerOptions _jsonOptions;

        public GrpcExceptionInterceptor(
            ILogger<GrpcExceptionInterceptor> logger,
            IHostEnvironment env
        )
        {
            _logger = logger;
            _env = env;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
        }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation
        )
        {
            try
            {
                return await continuation(request, context);
            }
            catch (NotFoundException ex)
            {
                _logger.LogWarning(ex, "NotFoundException: {Message}", ex.Message);
                throw CreateRpcException(StatusCode.NotFound, ex.Message);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "NotFoundException: {Message}", ex.Message);
                throw CreateRpcException(StatusCode.NotFound, ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "ArgumentException: {Message}", ex.Message);
                throw CreateRpcException(StatusCode.InvalidArgument, ex.Message);
            }
            catch (FormatException ex)
            {
                _logger.LogWarning(ex, "FormatException: {Message}", ex.Message);
                throw CreateRpcException(StatusCode.InvalidArgument, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "InvalidOperationException: {Message}", ex.Message);
                throw CreateRpcException(StatusCode.InvalidArgument, ex.Message);
            }
            catch (FluentValidation.ValidationException ex)
            {
                var errors = ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}").ToList();
                var detailMessage = string.Join(" | ", errors);

                _logger.LogWarning(ex, "Validation failed: {Errors}", detailMessage);
                throw CreateRpcException(StatusCode.InvalidArgument, detailMessage);
            }
            catch (DbUpdateConcurrencyException ex)
            {
                // Concurrency conflict: return Aborted or FailedPrecondition depending on semantics
                _logger.LogWarning(ex, "DbUpdateConcurrencyException: {Message}", ex.Message);

                var entryInfo = ex.Entries?.Select(e => e.Entity?.GetType().Name ?? "Unknown").ToArray() ?? Array.Empty<string>();
                var message = _env.IsDevelopment()
                    ? $"{ex.Message} | Entries: {string.Join(", ", entryInfo)}"
                    : "A concurrency conflict occurred.";

                throw CreateRpcException(StatusCode.Aborted, message);
            }
            catch (DbUpdateException ex)
            {
                // Database update errors often wrap provider-specific exceptions
                _logger.LogError(ex, "DbUpdateException when saving changes");

                // If there's an inner Postgres exception, surface more info in logs
                if (ex.InnerException is PostgresException pgEx)
                {
                    _logger.LogError(pgEx, "PostgresException: SQLState={SqlState}, Detail={Detail}, Constraint={ConstraintName}", pgEx.SqlState, pgEx.Detail, pgEx.ConstraintName);
                }
                else if (ex.InnerException is not null)
                {
                    _logger.LogError(ex.InnerException, "Inner exception of DbUpdateException: {Message}", ex.InnerException.Message);
                }

                var message = _env.IsDevelopment()
                    ? BuildDetailedMessage(ex)
                    : "A database error occurred.";

                throw CreateRpcException(StatusCode.Internal, message);
            }
            catch (PostgresException ex)
            {
                // provider-level Postgres exception
                _logger.LogError(ex, "PostgresException: SQLState={SqlState}, Detail={Detail}, Constraint={ConstraintName}", ex.SqlState, ex.Detail, ex.ConstraintName);

                var message = _env.IsDevelopment()
                    ? BuildDetailedMessage(ex)
                    : "A database error occurred.";

                throw CreateRpcException(StatusCode.Internal, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled exception");

                var message = _env.IsDevelopment() ? BuildDetailedMessage(ex) : "An unexpected error occurred.";
                throw CreateRpcException(StatusCode.Internal, message);
            }
        }

        private static string BuildDetailedMessage(Exception ex)
        {
            // Concatenate exception and inner exception messages (avoids EF's generic wrapper message).
            var parts = new List<string>();
            var current = ex;
            while (current is not null)
            {
                if (current is PostgresException pg)
                {
                    parts.Add($"{current.GetType().Name}: {current.Message} (SQLState={pg.SqlState}, Constraint={pg.ConstraintName}, Detail={pg.Detail})");
                }
                else
                {
                    parts.Add($"{current.GetType().Name}: {current.Message}");
                }

                current = current.InnerException;
            }

            return string.Join(" | ", parts);
        }

        private RpcException CreateRpcException(StatusCode statusCode, string message)
        {
            //var error = new ApiErrorResult(message, (int)statusCode, "gRPC service error");
            //var errorJson = JsonSerializer.Serialize(error, _jsonOptions);
            return new RpcException(new Status(statusCode, message));
        }
    }
}
