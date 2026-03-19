using Contracts.Abstractions.Persistence;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Common.Interfaces.Repositories;

public interface IOrganizationUserRepository : IRepositoryBaseAsync<OrganizationUser, Guid>
{
    /// <summary>
    /// Get organization user by user ID and organization ID
    /// Used to check if user already has membership in an organization
    /// </summary>
    Task<OrganizationUser?> GetByUserAndOrganizationAsync(
        Guid userId,
        int organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all users in an organization
    /// </summary>
    Task<List<OrganizationUser>> GetByOrganizationAsync(
        int organizationId,
        bool activeOnly = true,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all organizations for a user
    /// </summary>
    Task<List<OrganizationUser>> GetByUserIdAsync(
        Guid userId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user is member of organization
    /// </summary>
    Task<bool> IsUserInOrganizationAsync(
        Guid userId,
        int organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user is admin of organization
    /// </summary>
    Task<bool> IsUserOrganizationAdminAsync(
        Guid userId,
        int organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Count users in organization
    /// </summary>
    Task<int> CountByOrganizationAsync(
        int organizationId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get users by organization and role
    /// </summary>
    Task<List<OrganizationUser>> GetByOrganizationAndRoleAsync(
        int organizationId,
        OrganizationRole role,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get admins of an organization
    /// </summary>
    Task<List<OrganizationUser>> GetOrganizationAdminsAsync(
       int organizationId,
       CancellationToken cancellationToken = default);

    /// <summary>
    /// Count active users in organization
    /// </summary>
    Task<int> CountActiveUsersAsync(
       int organizationId,
       CancellationToken cancellationToken = default);

    // NOTE: All queries by license type or subscription have been moved
    // to the Order/License bounded context and read models.

    /// <summary>
    /// Get unique user IDs in organization with pagination (for grouped queries)
    /// Returns distinct user IDs that can be used to fetch their subscriptions
    /// </summary>
    Task<(List<Guid> UserIds, int TotalCount)> GetDistinctUserIdsByOrganizationAsync(
        int organizationId,
        bool activeOnly = true,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all subscriptions for specific users in an organization
    /// Used to load subscriptions after paginating users
    /// </summary>
    Task<List<OrganizationUser>> GetSubscriptionsForUsersAsync(
        int organizationId,
        List<Guid> userIds,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    Task<List<OrganizationUser>> GetStudentsByGroupIdAsync(
        int groupId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

   
    Task<int> CountStudentsByGroupIdAsync(
        int groupId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

   
    Task<Dictionary<int, int>> CountStudentsByGroupIdsAsync(
        List<int> groupIds,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    Task<Dictionary<int, List<OrganizationUser>>> GetStudentsByGroupIdsAsync(
        List<int> groupIds,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    Task<List<OrganizationUser>> GetByIdsAsync(
        List<Guid> ids,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get organization users by subscription order ID
    /// Uses license read model to find matching OrganizationUserIds, then fetches OrganizationUsers
    /// </summary>
    Task<List<OrganizationUser>> GetBySubscriptionOrderIdAsync(
        int subscriptionOrderId,
        bool activeOnly = true,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update multiple organization users (for batch operations)
    /// </summary>
    Task UpdateRangeAsync(
        List<OrganizationUser> organizationUsers,
        CancellationToken cancellationToken = default);
}
