using Shared.Exceptions;

namespace Identity.Domain.Exceptions;

/// <summary>
/// Exception thrown when an organization user relationship is not found
/// </summary>
public class OrganizationUserNotFoundException : NotFoundException
{
    public OrganizationUserNotFoundException(Guid userId, int organizationId)
        : base($"User {userId} is not a member of organization {organizationId}")
    {
    }

    public OrganizationUserNotFoundException(Guid organizationUserId)
        : base($"Organization user with ID {organizationUserId} was not found")
    {
    }
}
