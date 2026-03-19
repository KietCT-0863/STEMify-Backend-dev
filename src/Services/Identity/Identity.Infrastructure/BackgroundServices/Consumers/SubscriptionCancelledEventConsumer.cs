using EventBus.Messages.Subscription;
using Identity.Application.Common.Interfaces.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.BackgroundServices.Consumers;

/// <summary>
/// Consumer that handles subscription cancelled events
/// Marks related license projections as revoked
/// </summary>
public class SubscriptionCancelledEventConsumer : IConsumer<SubscriptionCancelledEvent>
{
    private readonly ILogger<SubscriptionCancelledEventConsumer> _logger;
    private readonly IOrganizationUserLicenseProjectionService _licenseProjectionService;

    public SubscriptionCancelledEventConsumer(
        IOrganizationUserLicenseProjectionService licenseProjectionService,
        ILogger<SubscriptionCancelledEventConsumer> logger)
    {
        _logger = logger;
        _licenseProjectionService = licenseProjectionService;
    }

    public async Task Consume(ConsumeContext<SubscriptionCancelledEvent> context)
    {
        var @event = context.Message;

        _logger.LogInformation(
            "Processing SubscriptionCancelledEvent for SubscriptionId: LicenseAssignmentCount: {Count}",
            @event.LicenseAssignmentIds?.Count ?? 0);

        if (@event.LicenseAssignmentIds == null || !@event.LicenseAssignmentIds.Any())
        {
            _logger.LogInformation("No license assignments found in SubscriptionCancelledEvent for Subscription");
            return;
        }

        var distinctIds = @event.LicenseAssignmentIds
            .Where(id => id > 0)
            .Distinct()
            .ToList();

        foreach (var licenseAssignmentId in distinctIds)
        {
            await _licenseProjectionService.ApplyLicenseRevokedAsync(
                licenseAssignmentId,
                context.CancellationToken);

            _logger.LogDebug(
                "Applied revocation to projection for LicenseAssignmentId {LicenseAssignmentId}",
                licenseAssignmentId);
        }

        _logger.LogInformation(
            "Completed processing SubscriptionCancelledEvent for Subscription: revoked {Count} license assignments",
            distinctIds.Count);
    }
}