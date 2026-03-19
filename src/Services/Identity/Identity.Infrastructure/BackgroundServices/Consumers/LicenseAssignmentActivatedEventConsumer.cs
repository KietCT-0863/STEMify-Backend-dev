using EventBus.Messages.License;
using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Common.Interfaces.Services;
using Identity.Domain.Entities;
using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Enums;

namespace Identity.Infrastructure.BackgroundServices.Consumers;

/// <summary>
/// Syncs OrganizationUser.IsActive and license information when license is activated
/// </summary>
public class LicenseAssignmentActivatedEventConsumer : IConsumer<LicenseAssignmentActivatedEvent>
{
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IOrganizationUserLicenseProjectionService _licenseProjectionService;
    private readonly IOrganizationUserLicenseReadRepository _licenseReadRepository;
    private readonly ILogger<LicenseAssignmentActivatedEventConsumer> _logger;

    public LicenseAssignmentActivatedEventConsumer(
        IOrganizationUserRepository organizationUserRepository,
        IIdentityUnitOfWork unitOfWork,
        IOrganizationUserLicenseProjectionService licenseProjectionService,
        IOrganizationUserLicenseReadRepository licenseReadRepository,
        ILogger<LicenseAssignmentActivatedEventConsumer> logger)
    {
        _organizationUserRepository = organizationUserRepository;
        _unitOfWork = unitOfWork;
        _licenseProjectionService = licenseProjectionService;
        _licenseReadRepository = licenseReadRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<LicenseAssignmentActivatedEvent> context)
    {
        var @event = context.Message;

        try
        {
            // Note: @event.UserId is actually ApplicationUser.Id, not OrganizationUserId
            // We need to find the correct OrganizationUser by matching subscriptionOrderId
            if (!Guid.TryParse(@event.UserId, out var userId))
            {
                _logger.LogWarning(
                    "Invalid UserId format in LicenseAssignmentActivatedEvent: {UserId}, LicenseAssignmentId: {LicenseAssignmentId}",
                    @event.UserId, @event.LicenseAssignmentId);
                return;
            }

            OrganizationUser? orgUser = null;
            var existingProjection = await _licenseReadRepository.GetByLicenseAssignmentIdAsync(
                @event.LicenseAssignmentId,
                context.CancellationToken);

            if (existingProjection != null)
            {
                var orgUsers = await _organizationUserRepository.GetByIdsAsync(
                    new List<Guid> { existingProjection.OrganizationUserId },
                    context.CancellationToken);
                orgUser = orgUsers.FirstOrDefault();
            }

            if (orgUser == null)
            {
                var orgUsers = await _organizationUserRepository.GetByUserIdAsync(
                    userId,
                    activeOnly: false,
                    context.CancellationToken);

                foreach (var ou in orgUsers)
                {
                    var projections = await _licenseReadRepository.GetByOrganizationUserIdAsync(
                        ou.Id,
                        context.CancellationToken);
                    
                    if (projections.Any(p => p.SubscriptionOrderId == @event.OrganizationSubscriptionOrderId))
                    {
                        orgUser = ou;
                        break;
                    }
                }

                if (orgUser == null)
                {
                    orgUser = orgUsers.FirstOrDefault();
                }
            }

            if (orgUser == null)
            {
                _logger.LogWarning(
                    "OrganizationUser not found for LicenseAssignmentActivatedEvent - " +
                    "LicenseAssignmentId: {LicenseAssignmentId}, UserId: {UserId}, SubscriptionOrderId: {SubscriptionOrderId}",
                    @event.LicenseAssignmentId, @event.UserId, @event.OrganizationSubscriptionOrderId);
                return;
            }

            await _licenseProjectionService.ApplyLicenseCreatedOrUpdatedAsync(
                orgUser,
                @event.LicenseAssignmentId,
                @event.LicenseType,
                @event.OrganizationSubscriptionOrderId,
                status: LicenseAssignmentStatus.Active,
                assignedAt: @event.ActivatedAt,
                context.CancellationToken);

            _logger.LogInformation(
                "Updated license projection for OrganizationUser {OrgUserId} from LicenseAssignmentActivatedEvent - " +
                "LicenseAssignmentId: {LicenseAssignmentId}, UserId: {UserId}",
                orgUser.Id, @event.LicenseAssignmentId, @event.UserId);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "Operation cancelled while processing LicenseAssignmentActivatedEvent - " +
                "LicenseAssignmentId: {LicenseAssignmentId}, UserId: {UserId}. " +
                "This may occur during application shutdown.",
                @event.LicenseAssignmentId, @event.UserId);
            throw; 
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing LicenseAssignmentActivatedEvent - " +
                "LicenseAssignmentId: {LicenseAssignmentId}, UserId: {UserId}",
                @event.LicenseAssignmentId, @event.UserId);
            throw;
        }
    }
}

