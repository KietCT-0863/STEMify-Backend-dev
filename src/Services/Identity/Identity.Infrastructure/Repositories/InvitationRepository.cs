using Identity.Application.Common.Interfaces.Repositories;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Data;
using Infrastructure.Abstractions.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;
using Sieve.Services;

namespace Identity.Infrastructure.Repositories;

public class InvitationRepository : EfRepositoryBase<ApplicationDbContext, Invitation, Guid>, IInvitationRepository
{
    public InvitationRepository(ApplicationDbContext dbContext, ISieveProcessor sieveProcessor)
        : base(dbContext, sieveProcessor)
    {
    }

    public async Task<Invitation?> GetByTokenAsync(
        string token,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .FirstOrDefaultAsync(i => i.Token.Value == token, cancellationToken);
    }

    public async Task<List<Invitation>> GetByJobIdAsync(
        Guid jobId,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(i => i.ProcessedByJobId == jobId)
            .OrderBy(i => i.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsForEmailAsync(
        int organizationId,
        string email,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(i => i.OrganizationId == organizationId
                && i.InviteeEmail.Value == email
                && i.Status == InvitationStatus.Pending,
                cancellationToken);
    }

    public async Task<bool> ExistsForEmailAndSubscriptionAsync(
        int organizationId,
        string email,
        int subscriptionOrderId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(i => i.OrganizationId == organizationId
                && i.InviteeEmail.Value == email
                && i.SubscriptionOrderId == subscriptionOrderId
                && i.Status == InvitationStatus.Pending,
                cancellationToken);
    }

   

    public async Task<List<Invitation>> GetByStatusAsync(
        InvitationStatus status,
        int pageNumber = 1,
        int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(i => i.Status == status)
            .OrderByDescending(i => i.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByJobIdAsync(
        Guid jobId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(i => i.ProcessedByJobId == jobId, cancellationToken);
    }

    public async Task<List<Invitation>> GetExpiredInvitationsAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(i => i.Status == InvitationStatus.Pending
                && i.ExpiresAt <= DateTime.UtcNow)
            .OrderBy(i => i.ExpiresAt)
            .ToListAsync(cancellationToken);
    }

  
    public async Task<Invitation?> GetLatestByEmailAsync(
        int organizationId,
        string email,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(i => i.OrganizationId == organizationId
                && i.InviteeEmail.Value == email)
            .OrderByDescending(i => i.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
    }

   
    public async Task<int> CountPendingAsync(
        int organizationId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(i => i.OrganizationId == organizationId
                && i.Status == InvitationStatus.Pending
                && i.ExpiresAt > DateTime.UtcNow,
                cancellationToken);
    }

    public async Task<List<Invitation>> GetPendingInvitationsAsync(
       int organizationId,
       CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(i => i.OrganizationId == organizationId
                && i.Status == InvitationStatus.Pending
                && i.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Invitation>> GetScheduledInvitationsForDateAsync(
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var startOfDay = date.Date;
        var endOfDay = startOfDay.AddDays(1);

        return await DbSet
            .Where(i => i.ScheduledSendDate.HasValue
                && i.ScheduledSendDate.Value >= startOfDay
                && i.ScheduledSendDate.Value < endOfDay
                && i.Status == InvitationStatus.Pending)
            .OrderBy(i => i.ScheduledSendDate)
            .ToListAsync(cancellationToken);
    }
}
