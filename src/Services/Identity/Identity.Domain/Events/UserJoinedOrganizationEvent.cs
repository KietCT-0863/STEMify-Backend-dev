using Contracts.Domains;
using Identity.Domain.Enums;

namespace Identity.Domain.Events;

/// <summary>
/// Domain event raised when a user joins an organization
/// Used for audit logging, analytics, and triggering onboarding workflows
/// </summary>
public record UserJoinedOrganizationEvent : DomainEvent
{
    public Guid UserId { get; init; }
    public int OrganizationId { get; init; }
    public OrganizationRole OrganizationRole { get; init; }
    public DateTime JoinedAt { get; init; }

    public UserJoinedOrganizationEvent(
        Guid userId,
        int organizationId,
        OrganizationRole organizationRole,
        DateTime joinedAt)
    {
        UserId = userId;
        OrganizationId = organizationId;
        OrganizationRole = organizationRole;
        JoinedAt = joinedAt;
    }
}
