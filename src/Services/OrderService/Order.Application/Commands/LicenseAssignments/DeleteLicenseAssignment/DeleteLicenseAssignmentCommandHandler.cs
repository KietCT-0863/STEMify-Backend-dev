using EventBus.Messages.License;
using MassTransit;
using MediatR;
using Order.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace Order.Application.Commands.LicenseAssignments.DeleteLicenseAssignment;

public class DeleteLicenseAssignmentCommandHandler : IRequestHandler<DeleteLicenseAssignmentCommand>
{
    private readonly IOrderUnitOfWork _unitOfWork;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<DeleteLicenseAssignmentCommandHandler> _logger;

    public DeleteLicenseAssignmentCommandHandler(
        IOrderUnitOfWork unitOfWork,
        IPublishEndpoint publishEndpoint,
        ILogger<DeleteLicenseAssignmentCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Handle(DeleteLicenseAssignmentCommand request, CancellationToken cancellationToken)
    {
        var licenseAssignment = await _unitOfWork.LicenseAssignments.FindByIdAsync(request.Id, cancellationToken);

        if (licenseAssignment == null)
        {
            throw new KeyNotFoundException($"LicenseAssignment with Id {request.Id} not found.");
        }

        await _unitOfWork.LicenseAssignments.DeleteAsync(licenseAssignment, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var deletedEvent = new LicenseAssignmentDeletedEvent(licenseAssignment.Id);

        await _publishEndpoint.Publish(deletedEvent, cancellationToken);

    }
}