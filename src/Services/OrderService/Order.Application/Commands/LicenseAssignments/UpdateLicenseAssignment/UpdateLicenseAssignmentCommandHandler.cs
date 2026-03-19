using Google.Protobuf.WellKnownTypes;
using MediatR;
using Order.Application.Common.Interfaces;
using Shared.Protos.Order;
using EventBus.Messages.License;
using MassTransit;

namespace Order.Application.Commands.LicenseAssignments.UpdateLicenseAssignment
{
    public class UpdateLicenseAssignmentCommandHandler : IRequestHandler<UpdateLicenseAssignmentCommand, GrpcLicenseAssignmentModel>
    {
        private readonly IOrderUnitOfWork _unitOfWork;
        private readonly IPublishEndpoint _publishEndpoint;

        public UpdateLicenseAssignmentCommandHandler(
            IOrderUnitOfWork unitOfWork,
            IPublishEndpoint publishEndpoint)
        {
            _unitOfWork = unitOfWork;
            _publishEndpoint = publishEndpoint;
        }

        public async Task<GrpcLicenseAssignmentModel> Handle(UpdateLicenseAssignmentCommand request, CancellationToken cancellationToken)
        {
            var licenseAssignment = await _unitOfWork.LicenseAssignments.FindByIdAsync(request.Id, cancellationToken);
            if (licenseAssignment == null)
                throw new KeyNotFoundException($"LicenseAssignment with ID {request.Id} not found.");

            var previousStatus = licenseAssignment.Status;

            if (request.Status.HasValue)
            {
                licenseAssignment.Status = request.Status.Value;
            }

            if (request.Status.HasValue && request.Status.Value == Domain.Enums.LicenseAssignmentStatus.Revoked && !licenseAssignment.RevokedAt.HasValue)
            {
                licenseAssignment.RevokedAt = DateTime.UtcNow;
            }

            await _unitOfWork.LicenseAssignments.UpdateAsync(licenseAssignment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Publish integration events when status changes so Identity can sync read models
            if (request.Status.HasValue && previousStatus != request.Status.Value)
            {
                var newStatus = request.Status.Value;

                if (newStatus == Order.Domain.Enums.LicenseAssignmentStatus.Active)
                {
                    var activatedEvent = new LicenseAssignmentActivatedEvent(
                        licenseAssignmentId: licenseAssignment.Id,
                        userId: licenseAssignment.OrganizationUserId,
                        organizationSubscriptionOrderId: licenseAssignment.OrganizationSubscriptionOrderId,
                        licenseType: licenseAssignment.LicenseType.ToString(),
                        activatedAt: DateTime.UtcNow);

                    await _publishEndpoint.Publish(activatedEvent, cancellationToken);
                }
                else if (newStatus == Order.Domain.Enums.LicenseAssignmentStatus.Revoked)
                {
                    var revokedAt = licenseAssignment.RevokedAt ?? DateTime.UtcNow;

                    var revokedEvent = new LicenseAssignmentRevokedEvent(
                        licenseAssignmentId: licenseAssignment.Id,
                        userId: licenseAssignment.OrganizationUserId,
                        organizationSubscriptionOrderId: licenseAssignment.OrganizationSubscriptionOrderId,
                        licenseType: licenseAssignment.LicenseType.ToString(),
                        revokedAt: revokedAt);

                    await _publishEndpoint.Publish(revokedEvent, cancellationToken);
                }
            }

            var response = new GrpcLicenseAssignmentModel
            {
                Id = licenseAssignment.Id,
                OrganizationSubscriptionOrderId = licenseAssignment.OrganizationSubscriptionOrderId,
                UserId = licenseAssignment.OrganizationUserId,
                Status = licenseAssignment.Status.ToString(),
                Type = licenseAssignment.LicenseType.ToString(),
                AssignedAt = Timestamp.FromDateTimeOffset(new DateTimeOffset(licenseAssignment.AssignedAt)),
                RevokedAt = licenseAssignment.RevokedAt.HasValue
                    ? Timestamp.FromDateTimeOffset(new DateTimeOffset(licenseAssignment.RevokedAt.Value))
                    : null
            };

            return response;
        }
    }
}