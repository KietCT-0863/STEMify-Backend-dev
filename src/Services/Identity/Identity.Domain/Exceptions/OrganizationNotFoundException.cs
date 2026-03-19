using Shared.Exceptions;

namespace Identity.Domain.Exceptions;

/// <summary>
/// Exception thrown when an organization is not found in Order Service
/// This is a cross-service exception
/// </summary>
public class OrganizationNotFoundException : NotFoundException
{
    public OrganizationNotFoundException(int organizationId)
        : base($"Organization with ID {organizationId} was not found")
    {
    }
}
