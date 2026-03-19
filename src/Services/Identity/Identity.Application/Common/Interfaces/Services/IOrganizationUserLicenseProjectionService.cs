using Identity.Domain.Entities;
using Shared.Enums;

namespace Identity.Application.Common.Interfaces.Services;

public interface IOrganizationUserLicenseProjectionService
{
    Task ApplyLicenseCreatedOrUpdatedAsync(
        OrganizationUser organizationUser,
        int licenseAssignmentId,
        string licenseType,
        int subscriptionOrderId,
        LicenseAssignmentStatus status,
        DateTime assignedAt,
        CancellationToken cancellationToken = default);

    Task ApplyLicenseRevokedAsync(
        int licenseAssignmentId,
        CancellationToken cancellationToken = default);

    Task HardDeleteLicenseAssignmentAsync(
        int licenseAssignmentId,
        CancellationToken cancellationToken = default);
}


