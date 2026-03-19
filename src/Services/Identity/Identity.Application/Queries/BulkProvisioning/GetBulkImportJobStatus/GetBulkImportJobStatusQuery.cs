using Identity.Application.Dtos.BulkProvisioning;
using MediatR;

namespace Identity.Application.Queries.BulkProvisioning.GetBulkImportJobStatus;

public class GetBulkImportJobStatusQuery : IRequest<BulkImportJobStatusDto>
{
    public Guid JobId { get; set; }
}
