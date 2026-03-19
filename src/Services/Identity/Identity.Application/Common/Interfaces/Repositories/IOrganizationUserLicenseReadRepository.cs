using Identity.Application.ReadModels;

namespace Identity.Application.Common.Interfaces.Repositories;

public interface IOrganizationUserLicenseReadRepository
{
    Task<OrganizationUserLicenseReadModel?> GetByLicenseAssignmentIdAsync(
        int licenseAssignmentId,
        CancellationToken cancellationToken = default);


    Task<IReadOnlyList<OrganizationUserLicenseReadModel>> GetByOrganizationUserIdAsync(
        Guid organizationUserId,
        CancellationToken cancellationToken = default);


    Task<IReadOnlyList<OrganizationUserLicenseReadModel>> GetActiveByOrganizationUserIdAsync(
        Guid organizationUserId,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrganizationUserLicenseReadModel>> GetByOrganizationIdAsync(
        int organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationUserLicenseReadModel>> GetActiveByOrganizationIdAsync(
        int organizationId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrganizationUserLicenseReadModel>> GetBySubscriptionOrderIdAsync(
        int subscriptionOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if OrganizationUser has active license in a specific subscription
    /// Returns true if there is at least one active license assignment for the OrganizationUser in the subscription
    /// </summary>
    Task<bool> IsOrganizationUserActiveInSubscriptionAsync(
        Guid organizationUserId,
        int subscriptionOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if OrganizationUser has any active license (in any subscription)
    /// Returns true if there is at least one active license assignment for the OrganizationUser
    /// </summary>
    Task<bool> IsOrganizationUserActiveAsync(
        Guid organizationUserId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get list of OrganizationUserIds that have active licenses
    /// Used for filtering OrganizationUsers by active status
    /// </summary>
    Task<HashSet<Guid>> GetActiveOrganizationUserIdsAsync(
        int? organizationId = null,
        int? subscriptionOrderId = null,
        CancellationToken cancellationToken = default);
}


