using Shared.Exceptions;

namespace Identity.Domain.Exceptions;

/// <summary>
/// Exception thrown when a bulk import job is not found
/// </summary>
public class BulkImportJobNotFoundException : NotFoundException
{
    public BulkImportJobNotFoundException(Guid jobId)
        : base($"Bulk import job with ID {jobId} was not found")
    {
    }
}
