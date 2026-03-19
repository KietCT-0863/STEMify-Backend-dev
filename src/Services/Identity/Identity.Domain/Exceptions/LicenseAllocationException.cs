using Shared.Exceptions;

namespace Identity.Domain.Exceptions;

/// <summary>
/// Exception thrown when license allocation fails
/// </summary>
public class LicenseAllocationException : DomainException
{
    public LicenseAllocationException(int organizationId, string licenseType, string reason)
        : base(
            $"Failed to allocate {licenseType} license for organization {organizationId}: {reason}",
            "LICENSE_ALLOCATION_FAILED"
        )
    {
    }

    public LicenseAllocationException(int organizationId, int requestedCount, int availableCount)
        : base(
            $"Insufficient licenses for organization {organizationId}. Requested: {requestedCount}, Available: {availableCount}",
            "INSUFFICIENT_LICENSES"
        )
    {
    }
}
