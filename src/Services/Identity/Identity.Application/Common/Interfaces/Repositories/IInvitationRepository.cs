using Contracts.Abstractions.Persistence;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Identity.Application.Common.Interfaces.Repositories;

public interface IInvitationRepository : IRepositoryBaseAsync<Invitation, Guid>
{
    /// <summary>
    /// Get invitation by token string
    /// </summary>
    Task<Invitation?> GetByTokenAsync(
        string token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all invitations for a bulk import job
    /// </summary>
    Task<List<Invitation>> GetByJobIdAsync(
        Guid jobId,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get expired invitations that need cleanup
    /// </summary>
    Task<List<Invitation>> GetExpiredInvitationsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if invitation exists for email in organization
    /// </summary>
    Task<bool> ExistsForEmailAsync(
        int organizationId,
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if invitation exists for email in organization with specific subscription
    /// Allows multiple invitations for same email if they belong to different subscriptions
    /// </summary>
    Task<bool> ExistsForEmailAndSubscriptionAsync(
        int organizationId,
        string email,
        int subscriptionOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get invitations by status
    /// </summary>
    Task<List<Invitation>> GetByStatusAsync(
        InvitationStatus status,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Count invitations by job
    /// </summary>
    Task<int> CountByJobIdAsync(
        Guid jobId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get invitation by email for organization (latest pending)
    /// </summary>
    Task<Invitation?> GetLatestByEmailAsync(
        int organizationId,
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Count pending invitations for organization
    /// </summary>
    Task<int> CountPendingAsync(
        int organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get pending invitations for organization
    /// </summary>
    Task<List<Invitation>> GetPendingInvitationsAsync(
       int organizationId,
       CancellationToken cancellationToken = default);
    Task<List<Invitation>> GetScheduledInvitationsForDateAsync(
        DateTime date,
        CancellationToken cancellationToken = default);
}
