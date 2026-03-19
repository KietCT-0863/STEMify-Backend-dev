using Grpc.Core;
using Identity.Application.Commands.BulkProvisioning.UploadBulkInvitationCsv;
using Identity.Application.Queries.BulkProvisioning.GetBulkImportJobStatus;
using Identity.Application.Queries.BulkProvisioning.ListBulkImportJobs;
using Identity.Domain.Exceptions;
using MediatR;
using Shared.Protos.User;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Identity.API.Services;

public class BulkProvisioningGrpcService : GrpcBulkProvisioning.GrpcBulkProvisioningBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<BulkProvisioningGrpcService> _logger;

    public BulkProvisioningGrpcService(
        IMediator mediator,
        ILogger<BulkProvisioningGrpcService> _logger)
    {
        _mediator = mediator;
        this._logger = _logger;
    }

    /// <summary>
    /// Upload CSV file for bulk user invitation
    /// </summary>
    public override async Task<UploadBulkInvitationResponse> UploadBulkInvitation(
        UploadBulkInvitationRequest request,
        ServerCallContext context)
    {
        try
        {
            var requesterId = Guid.Parse(ExtractUserIdOrThrow(context));
            using var stream = new MemoryStream(request.CsvData.ToByteArray());

            var command = new UploadBulkInvitationCsvCommand
            {
                OrganizationId = int.TryParse(request.OrganizationId, out var orgId)
                    ? orgId
                    : throw new RpcException(new Status(StatusCode.InvalidArgument, "organization_id must be an integer")),
                CsvFile = new FormFile(stream, 0, request.CsvData.Length, request.FileName, request.FileName),
                CreatedBy = requesterId,
                SubscriptionOrderId = string.IsNullOrWhiteSpace(request.SubscriptionOrderId)
                    ? (int?)null
                    : (int.TryParse(request.SubscriptionOrderId, out var subId)
                        ? subId
                        : throw new RpcException(new Status(StatusCode.InvalidArgument, "subscription_order_id must be an integer when provided")))
            };

            var result = await _mediator.Send(command, context.CancellationToken);

            var response = new UploadBulkInvitationResponse
            {
                JobId = result.Id.ToString(),
                TotalCount = result.TotalCount,
                ValidCount = result.TotalCount,
                InvalidCount = 0,
                Message = "Bulk invitation job created successfully. Processing will begin shortly."
            };

            return response;
        }
        catch (InvalidCsvDataException csvEx)
        {
            _logger.LogWarning(csvEx, "CSV validation failed for organization {OrganizationId}: {ErrorCount} errors",
                request.OrganizationId, csvEx.ErrorCount);

            var response = new UploadBulkInvitationResponse
            {
                TotalCount = csvEx.TotalRowCount,
                ValidCount = csvEx.ValidRowCount,
                InvalidCount = csvEx.ErrorCount,
                Message = csvEx.Message
            };

            foreach (var error in csvEx.Errors.Take(100)) // Limit to first 100 errors
            {
                response.Errors.Add(new CsvParseError
                {
                    RowNumber = error.RowNumber,
                    FieldName = error.FieldName,
                    RawValue = error.RawValue,
                    ErrorMessage = error.ErrorMessage
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading bulk invitation CSV for organization {OrganizationId}",
                request.OrganizationId);
            throw new RpcException(new Status(StatusCode.Internal,
                "An error occurred while processing your request"));
        }
    }

    /// <summary>
    /// Get bulk import job status
    /// </summary>
    public override async Task<BulkImportJobStatusResponse> GetBulkImportJobStatus(
        GetBulkImportJobStatusRequest request,
        ServerCallContext context)
    {
        try
        {
            var query = new GetBulkImportJobStatusQuery
            {
                JobId = Guid.TryParse(request.JobId, out var jobId)
                    ? jobId
                    : throw new RpcException(new Status(StatusCode.InvalidArgument, "job_id must be a GUID"))
            };

            var result = await _mediator.Send(query, context.CancellationToken);

            var response = new BulkImportJobStatusResponse
            {
                JobId = result.Id.ToString(),
                OrganizationId = result.OrganizationId.ToString(),
                Status = MapStatus(result.Status),
                TotalCount = result.TotalCount,
                ProcessedCount = result.ProcessedCount,
                SuccessCount = result.SuccessCount,
                FailedCount = result.FailedCount,
                ProgressPercentage = (double)result.ProgressPercentage,
                CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                    DateTime.SpecifyKind(result.CreatedAt, DateTimeKind.Utc)),
            };

            if (result.StartedAt.HasValue)
            {
                response.StartedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                    DateTime.SpecifyKind(result.StartedAt.Value, DateTimeKind.Utc));
            }

            if (result.CompletedAt.HasValue)
            {
                response.CompletedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                    DateTime.SpecifyKind(result.CompletedAt.Value, DateTimeKind.Utc));
            }

            if (!string.IsNullOrEmpty(result.ErrorMessage))
            {
                response.FailureReason = result.ErrorMessage;
            }

            // Map failures
            if (result.Failures != null && result.Failures.Any())
            {
                foreach (var failure in result.Failures)
                {
                    response.Failures.Add(new BulkImportFailure
                    {
                        Email = failure.Email,
                        Reason = failure.Reason,
                        FailedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                            DateTime.SpecifyKind(failure.FailedAt, DateTimeKind.Utc))
                    });
                }
            }

            return response;
        }
        catch (KeyNotFoundException)
        {
            throw new RpcException(new Status(StatusCode.NotFound,
                $"Job {request.JobId} not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting job status for {JobId}", request.JobId);
            throw new RpcException(new Status(StatusCode.Internal,
                "An error occurred while retrieving job status"));
        }
    }

    /// <summary>
    /// List bulk import jobs for an organization
    /// </summary>
    public override async Task<ListBulkImportJobsResponse> ListBulkImportJobs(
        ListBulkImportJobsRequest request,
        ServerCallContext context)
    {
        try
        {
            var query = new ListBulkImportJobsQuery
            {
                OrganizationId = int.TryParse(request.OrganizationId, out var orgId)
                    ? orgId
                    : throw new RpcException(new Status(StatusCode.InvalidArgument, "organization_id must be an integer")),
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            var result = await _mediator.Send(query, context.CancellationToken);

            var response = new ListBulkImportJobsResponse
            {
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            };

            foreach (var item in result.Items)
            {
                response.Items.Add(new BulkImportJobSummary
                {
                    JobId = item.Id.ToString(),
                    OrganizationId = item.OrganizationId.ToString(),
                Status = MapStatus(item.Status),
                TotalCount = item.TotalCount,
                    SuccessCount = item.SuccessCount,
                    FailedCount = item.FailedCount,
                    CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                        DateTime.SpecifyKind(item.CreatedAt, DateTimeKind.Utc))
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing jobs for organization {OrganizationId}",
                request.OrganizationId);
            throw new RpcException(new Status(StatusCode.Internal,
                "An error occurred while retrieving jobs"));
        }
    }

    /// <summary>
    /// Get failed invitations for a job
    /// </summary>
    public override async Task<JobFailuresResponse> GetJobFailures(
        GetJobFailuresRequest request,
        ServerCallContext context)
    {
        try
        {
            var query = new GetBulkImportJobStatusQuery
            {
                JobId = Guid.TryParse(request.JobId, out var jobId)
                    ? jobId
                    : throw new RpcException(new Status(StatusCode.InvalidArgument, "job_id must be a GUID"))
            };

            var result = await _mediator.Send(query, context.CancellationToken);

            var response = new JobFailuresResponse();

            if (result.Failures != null && result.Failures.Any())
            {
                foreach (var failure in result.Failures)
                {
                    response.Failures.Add(new BulkImportFailure
                    {
                        Email = failure.Email,
                        Reason = failure.Reason,
                        FailedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                            DateTime.SpecifyKind(failure.FailedAt, DateTimeKind.Utc))
                    });
                }
            }

            return response;
        }
        catch (KeyNotFoundException)
        {
            throw new RpcException(new Status(StatusCode.NotFound,
                $"Job {request.JobId} not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting failures for job {JobId}", request.JobId);
            throw new RpcException(new Status(StatusCode.Internal,
                "An error occurred while retrieving failures"));
        }
    }

    private static BulkImportStatus MapStatus(Identity.Domain.Enums.BulkImportStatus status)
    {
        return status switch
        {
            Identity.Domain.Enums.BulkImportStatus.Pending => BulkImportStatus.Pending,
            Identity.Domain.Enums.BulkImportStatus.Processing => BulkImportStatus.Processing,
            Identity.Domain.Enums.BulkImportStatus.Completed => BulkImportStatus.Completed,
            Identity.Domain.Enums.BulkImportStatus.Failed => BulkImportStatus.Failed,
            _ => BulkImportStatus.Unspecified
        };
    }

    private static string ExtractUserIdOrThrow(ServerCallContext context)
    {
        var httpContext = context.GetHttpContext();

        // Try principal claims first
        var principal = httpContext.User;
        var userId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? principal?.FindFirst("sub")?.Value
                     ?? httpContext.Request.Headers["X-User-Id"].FirstOrDefault();

        // Fallback: parse Authorization header (no signature validation)
        if (string.IsNullOrWhiteSpace(userId))
        {
            var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(authHeader))
            {
                var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? authHeader.Substring("Bearer ".Length)
                    : authHeader;
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(token))
                {
                    var jwt = handler.ReadJwtToken(token);
                    userId = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
                          ?? jwt.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(userId))
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Missing user identity"));

        return userId;
    }
}
