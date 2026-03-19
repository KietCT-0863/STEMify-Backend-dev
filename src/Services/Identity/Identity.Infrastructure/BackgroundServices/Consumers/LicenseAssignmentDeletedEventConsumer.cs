using EventBus.Messages.License;
using Identity.Application.Common.Interfaces.Services;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace Identity.Infrastructure.BackgroundServices.Consumers;

public class LicenseAssignmentDeletedEventConsumer : IConsumer<LicenseAssignmentDeletedEvent>
{
    private readonly IOrganizationUserLicenseProjectionService _licenseProjectionService;
    private readonly ILogger<LicenseAssignmentDeletedEventConsumer> _logger;

    public LicenseAssignmentDeletedEventConsumer(
        IOrganizationUserLicenseProjectionService licenseProjectionService,
        ILogger<LicenseAssignmentDeletedEventConsumer> logger)
    {
        _licenseProjectionService = licenseProjectionService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<LicenseAssignmentDeletedEvent> context)
    {
        var @event = context.Message;

        try
        {
            await _licenseProjectionService.HardDeleteLicenseAssignmentAsync(
                @event.LicenseAssignmentId,
                context.CancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing LicenseAssignmentDeletedEvent for LicenseAssignmentId {LicenseAssignmentId}",
                @event.LicenseAssignmentId);
            throw;
        }
    }
}


