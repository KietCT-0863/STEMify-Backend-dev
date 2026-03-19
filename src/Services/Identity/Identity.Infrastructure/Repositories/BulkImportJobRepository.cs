using Identity.Application.Common.Interfaces.Repositories;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Data;
using Infrastructure.Abstractions.Persistence.EfCore;
using Microsoft.EntityFrameworkCore;
using Sieve.Services;

namespace Identity.Infrastructure.Repositories;
public class BulkImportJobRepository : EfRepositoryBase<ApplicationDbContext, BulkImportJob, Guid>, IBulkImportJobRepository
{
    public BulkImportJobRepository(ApplicationDbContext dbContext, ISieveProcessor sieveProcessor)
        : base(dbContext, sieveProcessor)
    {
    }

    public async Task<List<BulkImportJob>> GetByOrganizationAsync(
        int organizationId,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(j => j.OrganizationId == organizationId)
            .OrderByDescending(j => j.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> CountByOrganizationAsync(
        int organizationId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .CountAsync(j => j.OrganizationId == organizationId, cancellationToken);
    }

    public async Task<List<BulkImportJob>> GetPendingJobsAsync(
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(j => j.Status == BulkImportStatus.Pending)
            .OrderBy(j => j.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<BulkImportJob>> GetByStatusAsync(
        BulkImportStatus status,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(j => j.Status == status)
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<BulkImportJob>> GetRecentJobsWithFailuresAsync(
        int organizationId,
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .Where(j => j.OrganizationId == organizationId
                && j.FailedCount > 0
                && (j.Status == BulkImportStatus.Completed || j.Status == BulkImportStatus.Failed))
            .OrderByDescending(j => j.CompletedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> HasActiveJobAsync(
        int organizationId,
        CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(j => j.OrganizationId == organizationId
                && (j.Status == BulkImportStatus.Pending || j.Status == BulkImportStatus.Processing),
                cancellationToken);
    }
}
