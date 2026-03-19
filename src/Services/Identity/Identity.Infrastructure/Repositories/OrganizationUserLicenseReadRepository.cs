using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.ReadModels;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;

namespace Identity.Infrastructure.Repositories;

public class OrganizationUserLicenseReadRepository : IOrganizationUserLicenseReadRepository
{
    private readonly ApplicationDbContext _dbContext;

    public OrganizationUserLicenseReadRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<OrganizationUserLicenseReadModel?> GetByLicenseAssignmentIdAsync(
        int licenseAssignmentId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbContext.OrganizationUserLicenseReadModels
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.LicenseAssignmentId == licenseAssignmentId, cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationUserLicenseReadModel>> GetByOrganizationUserIdAsync(
        Guid organizationUserId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbContext.OrganizationUserLicenseReadModels
            .AsNoTracking()
            .Where(x => x.OrganizationUserId == organizationUserId)
            .OrderByDescending(x => x.AssignedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationUserLicenseReadModel>> GetActiveByOrganizationUserIdAsync(
        Guid organizationUserId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbContext.OrganizationUserLicenseReadModels
            .AsNoTracking()
            .Where(x => x.OrganizationUserId == organizationUserId && x.Status == LicenseAssignmentStatus.Active)
            .OrderByDescending(x => x.AssignedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationUserLicenseReadModel>> GetByOrganizationIdAsync(
        int organizationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbContext.OrganizationUserLicenseReadModels
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId)
            .OrderByDescending(x => x.AssignedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationUserLicenseReadModel>> GetActiveByOrganizationIdAsync(
        int organizationId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbContext.OrganizationUserLicenseReadModels
            .AsNoTracking()
            .Where(x => x.OrganizationId == organizationId && x.Status == LicenseAssignmentStatus.Active)
            .OrderByDescending(x => x.AssignedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OrganizationUserLicenseReadModel>> GetBySubscriptionOrderIdAsync(
        int subscriptionOrderId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbContext.OrganizationUserLicenseReadModels
            .AsNoTracking()
            .Where(x => x.SubscriptionOrderId == subscriptionOrderId)
            .OrderByDescending(x => x.AssignedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsOrganizationUserActiveInSubscriptionAsync(
        Guid organizationUserId,
        int subscriptionOrderId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbContext.OrganizationUserLicenseReadModels
            .AsNoTracking()
            .AnyAsync(x => x.OrganizationUserId == organizationUserId
                        && x.SubscriptionOrderId == subscriptionOrderId
                        && x.Status == LicenseAssignmentStatus.Active,
                cancellationToken);
    }

    public async Task<bool> IsOrganizationUserActiveAsync(
        Guid organizationUserId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _dbContext.OrganizationUserLicenseReadModels
            .AsNoTracking()
            .AnyAsync(x => x.OrganizationUserId == organizationUserId
                        && x.Status == LicenseAssignmentStatus.Active,
                cancellationToken);
    }

    public async Task<HashSet<Guid>> GetActiveOrganizationUserIdsAsync(
        int? organizationId = null,
        int? subscriptionOrderId = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var query = _dbContext.OrganizationUserLicenseReadModels
            .AsNoTracking()
            .Where(x => x.Status == LicenseAssignmentStatus.Active);

        if (organizationId.HasValue)
            query = query.Where(x => x.OrganizationId == organizationId.Value);

        if (subscriptionOrderId.HasValue)
            query = query.Where(x => x.SubscriptionOrderId == subscriptionOrderId.Value);

        var activeIds = await query
            .Select(x => x.OrganizationUserId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return activeIds.ToHashSet();
    }
}


