using Identity.Application.Common.Interfaces.Services;
using Identity.Application.ReadModels;
using Identity.Domain.Entities;
using Identity.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shared.Enums;

namespace Identity.Infrastructure.Services;

public class OrganizationUserLicenseProjectionService : IOrganizationUserLicenseProjectionService
{
    private readonly ApplicationDbContext _dbContext;

    public OrganizationUserLicenseProjectionService(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task ApplyLicenseCreatedOrUpdatedAsync(
        OrganizationUser organizationUser,
        int licenseAssignmentId,
        string licenseType,
        int subscriptionOrderId,
        LicenseAssignmentStatus status,
        DateTime assignedAt,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var projection = await _dbContext.OrganizationUserLicenseReadModels
            .SingleOrDefaultAsync(x => x.LicenseAssignmentId == licenseAssignmentId, cancellationToken);

        if (projection == null)
        {
            projection = new OrganizationUserLicenseReadModel
            {
                LicenseAssignmentId = licenseAssignmentId,
                OrganizationUserId = organizationUser.Id,
                OrganizationId = organizationUser.OrganizationId,
                UserId = organizationUser.UserId,
                SubscriptionOrderId = subscriptionOrderId,
                LicenseType = licenseType,
                Status = status,
                AssignedAt = assignedAt,
                LastUpdatedAt = DateTime.UtcNow
            };

            await _dbContext.OrganizationUserLicenseReadModels.AddAsync(projection, cancellationToken);
        }
        else
        {
            projection.Status = status;
            projection.SubscriptionOrderId = subscriptionOrderId;
            projection.LicenseType = licenseType;
            projection.LastUpdatedAt = DateTime.UtcNow;
            
            
            projection.OrganizationUserId = organizationUser.Id;
            projection.OrganizationId = organizationUser.OrganizationId;
            projection.UserId = organizationUser.UserId;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ApplyLicenseRevokedAsync(
        int licenseAssignmentId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var projection = await _dbContext.OrganizationUserLicenseReadModels
            .SingleOrDefaultAsync(x => x.LicenseAssignmentId == licenseAssignmentId, cancellationToken);

        if (projection == null)
        {
           return;
        }

        projection.Status = LicenseAssignmentStatus.Revoked;
        projection.LastUpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task HardDeleteLicenseAssignmentAsync(
        int licenseAssignmentId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var projection = await _dbContext.OrganizationUserLicenseReadModels
            .SingleOrDefaultAsync(x => x.LicenseAssignmentId == licenseAssignmentId, cancellationToken);

        if (projection == null)
        {
            return;
        }

        _dbContext.OrganizationUserLicenseReadModels.Remove(projection);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}


