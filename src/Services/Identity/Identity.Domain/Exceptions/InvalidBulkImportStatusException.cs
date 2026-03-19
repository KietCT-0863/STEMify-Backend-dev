using Identity.Domain.Enums;
using Shared.Exceptions;

namespace Identity.Domain.Exceptions;

/// <summary>
/// Exception thrown when attempting an operation on a bulk import job with invalid status
/// </summary>
public class InvalidBulkImportStatusException : DomainException
{
    public InvalidBulkImportStatusException(Guid jobId, BulkImportStatus currentStatus, string operation)
        : base(
            $"Cannot perform operation '{operation}' on bulk import job {jobId} with status {currentStatus}",
            "INVALID_BULK_IMPORT_STATUS"
        )
    {
    }
}
