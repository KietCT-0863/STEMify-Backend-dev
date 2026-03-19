using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Dtos.BulkProvisioning;
using MediatR;

namespace Identity.Application.Queries.BulkProvisioning.ListBulkImportJobs;

public class ListBulkImportJobsQueryHandler : IRequestHandler<ListBulkImportJobsQuery, BulkImportJobsPageDto>
{
    private readonly IBulkImportJobRepository _jobRepository;

    public ListBulkImportJobsQueryHandler(IBulkImportJobRepository jobRepository)
    {
        _jobRepository = jobRepository;
    }

    public async Task<BulkImportJobsPageDto> Handle(ListBulkImportJobsQuery request, CancellationToken cancellationToken)
    {
        var jobs = await _jobRepository.GetByOrganizationAsync(request.OrganizationId, request.PageNumber, request.PageSize, cancellationToken);
        var total = await _jobRepository.CountByOrganizationAsync(request.OrganizationId, cancellationToken);

        var items = jobs.Select(j => new BulkImportJobDto
        {
            Id = j.Id,
            OrganizationId = j.OrganizationId,
            Status = j.Status,
            TotalCount = j.TotalCount,
            ProcessedCount = j.ProcessedCount,
            SuccessCount = j.SuccessCount,
            FailedCount = j.FailedCount,
            ProgressPercentage = j.ProgressPercentage,
            CreatedBy = j.CreatedBy,
            CreatedAt = j.CreatedAt,
            StartedAt = j.StartedAt,
            CompletedAt = j.CompletedAt,
            Duration = j.ProcessingDuration
        }).ToList();

        return new BulkImportJobsPageDto
        {
            Items = items,
            TotalCount = total,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
    }
}


