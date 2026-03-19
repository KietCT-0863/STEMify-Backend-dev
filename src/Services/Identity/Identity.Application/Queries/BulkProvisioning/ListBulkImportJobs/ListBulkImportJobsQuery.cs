using Identity.Application.Dtos.BulkProvisioning;
using MediatR;

namespace Identity.Application.Queries.BulkProvisioning.ListBulkImportJobs;

public class ListBulkImportJobsQuery : IRequest<BulkImportJobsPageDto>
{
    public int OrganizationId { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}


