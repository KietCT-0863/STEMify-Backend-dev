using EventBus.Messages.Subscription;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Order.Application.Common.Interfaces;
using Order.Domain.Enums;

namespace Order.Application.Commands.OrganizationSubscriptionOrders.CancelOrganizationSubscriptionOrder
{
    public class CancelOrganizationSubscriptionOrderCommandHandler : IRequestHandler<CancelOrganizationSubscriptionOrderCommand, bool>
    {
        private readonly IOrderUnitOfWork _unitOfWork;
        private readonly IPublishEndpoint _publishEndpoint;
        private readonly ILogger<CancelOrganizationSubscriptionOrderCommandHandler> _logger;

        public CancelOrganizationSubscriptionOrderCommandHandler(
            IOrderUnitOfWork unitOfWork,
            IPublishEndpoint publishEndpoint,
            ILogger<CancelOrganizationSubscriptionOrderCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _publishEndpoint = publishEndpoint;
            _logger = logger;
        }

        public async Task<bool> Handle(CancelOrganizationSubscriptionOrderCommand request, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Starting cancellation for OrganizationSubscriptionOrder {OrderId}", request.Id);

            var organizationSubscriptionOrder = await _unitOfWork.OrganizationSubscriptionOrders.FindByIdAsync(
                request.Id,
                cancellationToken
            );

            if (organizationSubscriptionOrder == null)
            {
                _logger.LogWarning("OrganizationSubscriptionOrder with ID {OrderId} not found", request.Id);
                throw new KeyNotFoundException($"OrganizationSubscriptionOrder with ID {request.Id} not found.");
            }

            // Update in-memory; rely on unit of work SaveChanges for a single DB round-trip.
            organizationSubscriptionOrder.Status = OrganizationSubscriptionOrderStatus.Cancelled;
            organizationSubscriptionOrder.LastModifiedDate = DateTimeOffset.UtcNow;

            // Load active license assignments for the subscription.
            var licenseAssignments = (await _unitOfWork.LicenseAssignments.FindAsync(
                    predicate: la => la.OrganizationSubscriptionOrderId == organizationSubscriptionOrder.Id
                                     && la.Status != LicenseAssignmentStatus.Revoked
                                     && la.Status != LicenseAssignmentStatus.Expired,
                    cancellationToken: cancellationToken
                )).ToList();

            if (licenseAssignments.Count == 0)
            {
                _logger.LogInformation("No active license assignments found for subscription {OrderId}", organizationSubscriptionOrder.Id);
            }
            else
            {
                foreach (var la in licenseAssignments)
                {
                    la.Status = LicenseAssignmentStatus.Revoked;
                    la.RevokedAt = DateTime.UtcNow;
                }

                _logger.LogInformation("Marked {Count} license assignments as Revoked for subscription {OrderId}",
                    licenseAssignments.Count, organizationSubscriptionOrder.Id);
            }

            // Persist all changes in one SaveChanges call to reduce round-trips.
            var saved = await _unitOfWork.SaveChangesAsync(cancellationToken);
            if (saved <= 0)
            {
                _logger.LogWarning("No changes were saved when cancelling subscription {OrderId}", organizationSubscriptionOrder.Id);
                return false;
            }

            // Publish event after persistence to avoid publishing before DB commit.
            var @event = new SubscriptionCancelledEvent
            {
                LicenseAssignmentIds = licenseAssignments.Select(la => la.Id).ToList(),
            };

            await _publishEndpoint.Publish(@event, cancellationToken);

            _logger.LogInformation("Published SubscriptionCancelledEvent for subscription {OrderId} with {Count} license assignments",
                organizationSubscriptionOrder.Id, @event.LicenseAssignmentIds.Count);

            return true;
        }
    }
}