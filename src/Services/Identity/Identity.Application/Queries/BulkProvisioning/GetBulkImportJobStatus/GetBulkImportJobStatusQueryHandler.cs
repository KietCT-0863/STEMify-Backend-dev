using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Dtos.BulkProvisioning;
using Identity.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Queries.BulkProvisioning.GetBulkImportJobStatus;

public class GetBulkImportJobStatusQueryHandler
    : IRequestHandler<GetBulkImportJobStatusQuery, BulkImportJobStatusDto>
{
    private readonly IBulkImportJobRepository _jobRepository;
    private readonly ILogger<GetBulkImportJobStatusQueryHandler> _logger;

    public GetBulkImportJobStatusQueryHandler(
        IBulkImportJobRepository jobRepository,
        ILogger<GetBulkImportJobStatusQueryHandler> logger)
    {
        _jobRepository = jobRepository;
        _logger = logger;
    }

    public async Task<BulkImportJobStatusDto> Handle(
        GetBulkImportJobStatusQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Getting status for bulk import job {JobId}",
            request.JobId);

        var job = await _jobRepository.FindByIdAsync(request.JobId, cancellationToken);

        if (job == null)
        {
            throw new BulkImportJobNotFoundException(request.JobId);
        }

        return new BulkImportJobStatusDto
        {
            Id = job.Id,
            OrganizationId = job.OrganizationId,
            Status = job.Status,
            TotalCount = job.TotalCount,
            ProcessedCount = job.ProcessedCount,
            SuccessCount = job.SuccessCount,
            FailedCount = job.FailedCount,
            ProgressPercentage = job.ProgressPercentage,
            CreatedAt = job.CreatedAt,
            StartedAt = job.StartedAt,
            CompletedAt = job.CompletedAt,
            Duration = job.ProcessingDuration,
            ErrorMessage = job.HasFailed()
                ? (job.Failures.LastOrDefault(f => f.Email == "SYSTEM")?.Reason
                   ?? job.Failures.LastOrDefault()?.Reason)
                : null
        };
    }
}
