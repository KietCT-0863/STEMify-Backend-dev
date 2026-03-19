using Identity.Application.Dtos.Grpc;

namespace Identity.Application.Common.Interfaces.Grpc;

public interface IOrderLicenseService
{
    /// <summary>
    /// Check if organization has sufficient licenses available
    /// </summary>
    Task<LicenseAvailabilityDto> CheckLicenseAvailabilityAsync(
        int organizationId,
        string licenseType,
        int requestedCount,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Bulk check multiple license types at once (optimized for bulk import validation)
    /// </summary>
    Task<BulkLicenseCheckDto> BulkCheckLicensesAsync(
        int organizationId,
        Dictionary<string, int> licenseRequests,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Assign license to user by email
    /// </summary>
    Task<LicenseAssignmentResultDto> AssignLicenseAsync(
        int organizationId,
        string userEmail,
        string licenseType,
        int subscriptionOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get organization details with email domain and license information
    /// </summary>
    Task<OrganizationDto> GetOrganizationAsync(
        int organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get organization info specifically for bulk provisioning (includes license counts)
    /// </summary>
    Task<OrganizationBulkProvisioningDto> GetOrganizationForBulkProvisioningAsync(
        int organizationId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Reserve license with Pending status (for bulk invitation)
    /// This reserves seats before users accept invitations
    /// </summary>
    Task<LicenseAssignmentResultDto> ReserveLicenseAsync(
        int organizationId,
        string userId,
        string licenseType,
        int subscriptionOrderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Activate reserved license (Pending -> Active)
    /// Called when user accepts invitation
    /// </summary>
    Task<LicenseAssignmentResultDto> ActivateReservedLicenseAsync(
        int organizationId,
        string userEmail,
        string licenseType,
        int subscriptionOrderId,
        CancellationToken cancellationToken = default);
}
