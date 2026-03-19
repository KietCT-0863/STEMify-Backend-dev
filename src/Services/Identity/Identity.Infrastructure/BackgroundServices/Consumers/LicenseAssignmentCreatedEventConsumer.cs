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
/// Syncs OrganizationUser license information when license is created/reserved
/// </summary>
public class LicenseAssignmentCreatedEventConsumer : IConsumer<LicenseAssignmentCreatedEvent>
{
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly IOrganizationUserLicenseProjectionService _licenseProjectionService;
    private readonly ILogger<LicenseAssignmentCreatedEventConsumer> _logger;

    public LicenseAssignmentCreatedEventConsumer(
        IOrganizationUserRepository organizationUserRepository,
        IIdentityUnitOfWork unitOfWork,
        IOrganizationUserLicenseProjectionService licenseProjectionService,
        ILogger<LicenseAssignmentCreatedEventConsumer> logger)
    {
        _organizationUserRepository = organizationUserRepository;
        _unitOfWork = unitOfWork;
        _licenseProjectionService = licenseProjectionService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<LicenseAssignmentCreatedEvent> context)
    {
        var @event = context.Message;

        try
        {
            
            if (!Guid.TryParse(@event.UserId, out var organizationUserId))
            {
                _logger.LogWarning(
                    "Invalid OrganizationUserId format in LicenseAssignmentCreatedEvent: {UserId}, LicenseAssignmentId: {LicenseAssignmentId}",
                    @event.UserId, @event.LicenseAssignmentId);
                return;
            }

            var orgUsers = await _organizationUserRepository.GetByIdsAsync(
                new List<Guid> { organizationUserId },
                context.CancellationToken);

            var orgUser = orgUsers.FirstOrDefault();
            if (orgUser == null)
            {
                _logger.LogDebug(
                    "OrganizationUser not found for LicenseAssignmentCreatedEvent - " +
                    "LicenseAssignmentId: {LicenseAssignmentId}, OrganizationUserId: {OrganizationUserId}. " +
                    "This may occur if the membership was not yet created or has been removed.",
                    @event.LicenseAssignmentId, @event.UserId);
                return;
            }

            var status = LicenseAssignmentStatus.Pending;
            if (Enum.TryParse<LicenseAssignmentStatus>(@event.Status, true, out var parsedStatus))
            {
                status = parsedStatus;
            }
            else if (@event.Status == "Active")
            {
                status = LicenseAssignmentStatus.Active;
            }
            else if (@event.Status == "Revoked")
            {
                status = LicenseAssignmentStatus.Revoked;
            }
            else if (@event.Status == "Expired")
            {
                status = LicenseAssignmentStatus.Expired;
            }

            await _licenseProjectionService.ApplyLicenseCreatedOrUpdatedAsync(
                orgUser,
                @event.LicenseAssignmentId,
                @event.LicenseType,
                @event.OrganizationSubscriptionOrderId,
                status: status,
                assignedAt: @event.AssignedAt,
                context.CancellationToken);

            _logger.LogInformation(
                "Updated license projection for OrganizationUser {OrgUserId} from LicenseAssignmentCreatedEvent - " +
                "LicenseAssignmentId: {LicenseAssignmentId}, OrganizationUserId: {OrganizationUserId}, Status: {Status}",
                orgUser.Id, @event.LicenseAssignmentId, @event.UserId, @event.Status);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation(
                "Operation cancelled while processing LicenseAssignmentCreatedEvent - " +
                "LicenseAssignmentId: {LicenseAssignmentId}, OrganizationUserId: {OrganizationUserId}. " +
                "This may occur during application shutdown.",
                @event.LicenseAssignmentId, @event.UserId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error processing LicenseAssignmentCreatedEvent - " +
                "LicenseAssignmentId: {LicenseAssignmentId}, OrganizationUserId: {OrganizationUserId}",
                @event.LicenseAssignmentId, @event.UserId);
            throw;
        }
    }
}

