using EventBus.Messages.License;
using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Common.Interfaces.Services;
using Identity.Domain.Entities;
using MassTransit;
using Microsoft.Extensions.Logging;  

namespace Identity.Infrastructure.BackgroundServices.Consumers;

/// <summary>
/// Syncs OrganizationUser.IsActive = false when license is revoked
/// </summary>
public class LicenseAssignmentRevokedEventConsumer : IConsumer<LicenseAssignmentRevokedEvent>
{
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IOrganizationUserLicenseProjectionService _licenseProjectionService;
    private readonly ILogger<LicenseAssignmentRevokedEventConsumer> _logger;

    public LicenseAssignmentRevokedEventConsumer(
        IOrganizationUserRepository organizationUserRepository,
        IIdentityUnitOfWork unitOfWork,
        IOrganizationUserLicenseProjectionService licenseProjectionService,
        ILogger<LicenseAssignmentRevokedEventConsumer> logger)
    {
        _organizationUserRepository = organizationUserRepository;
        _unitOfWork = unitOfWork;
        _licenseProjectionService = licenseProjectionService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<LicenseAssignmentRevokedEvent> context)
    {
        var @event = context.Message;

        try
        {
            if (!Guid.TryParse(@event.UserId, out var userId))
            {
                _logger.LogWarning(
                    "Invalid UserId format in LicenseAssignmentRevokedEvent: {UserId}, LicenseAssignmentId: {LicenseAssignmentId}",
                    @event.UserId, @event.LicenseAssignmentId);
                return;
            }

            var orgUsers = await _organizationUserRepository.GetByUserIdAsync(
                userId,
                activeOnly: false,
                context.CancellationToken);

            var orgUser = orgUsers.FirstOrDefault();

            if (orgUser == null)
            {
                _logger.LogWarning(
                    "OrganizationUser not found for LicenseAssignmentRevokedEvent - " +
                    "LicenseAssignmentId: {LicenseAssignmentId}, UserId: {UserId}, SubscriptionOrderId: {SubscriptionOrderId}",
                    @event.LicenseAssignmentId, @event.UserId, @event.OrganizationSubscriptionOrderId);
                return;
            }

            await _licenseProjectionService.ApplyLicenseRevokedAsync(
                @event.LicenseAssignmentId,
                context.CancellationToken);

            _logger.LogInformation(
                "Updated license projection for OrganizationUser {OrgUserId} from LicenseAssignmentRevokedEvent - " +
                "LicenseAssignmentId: {LicenseAssignmentId}, UserId: {UserId}, Reason: {Reason}",
                orgUser.Id, @event.LicenseAssignmentId, @event.UserId, @event.Reason);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "Operation cancelled while processing LicenseAssignmentRevokedEvent - " +
                "LicenseAssignmentId: {LicenseAssignmentId}, UserId: {UserId}. " +
                "This may occur during application shutdown.",
                @event.LicenseAssignmentId, @event.UserId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing LicenseAssignmentRevokedEvent - " +
                "LicenseAssignmentId: {LicenseAssignmentId}, UserId: {UserId}",
                @event.LicenseAssignmentId, @event.UserId);
            throw;
        }
    }
}

