using Shared.Exceptions;

namespace Identity.Domain.Exceptions;

/// <summary>
/// Exception thrown when attempting to add a user to an organization they are already a member of
/// </summary>
public class DuplicateOrganizationUserException : DomainException
{
    public DuplicateOrganizationUserException(Guid userId, int organizationId)
        : base(
            $"User {userId} is already a member of organization {organizationId}",
            "DUPLICATE_ORGANIZATION_USER"
        )
    {
    }
}
