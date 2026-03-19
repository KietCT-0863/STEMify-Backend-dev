using MediatR;
using Order.Application.Common.Interfaces;

namespace Order.Application.Commands.OrganizationSubscriptionOrders.DeleteOrganizationSubscriptionOrder
{
    public class DeleteOrganizationSubscriptionOrderCommandHandler : IRequestHandler<DeleteOrganizationSubscriptionOrderCommand, bool>
    {
        private readonly IOrderUnitOfWork _unitOfWork;

        public DeleteOrganizationSubscriptionOrderCommandHandler(IOrderUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeleteOrganizationSubscriptionOrderCommand request, CancellationToken cancellationToken)
        {
            var organizationSubscriptionOrder = await _unitOfWork.OrganizationSubscriptionOrders.FindByIdAsync(
                request.Id,
                cancellationToken
            );
            if (organizationSubscriptionOrder == null)
                throw new KeyNotFoundException($"OrganizationSubscriptionOrder with ID {request.Id} not found.");

            organizationSubscriptionOrder.Status = Domain.Enums.OrganizationSubscriptionOrderStatus.Archived;
            organizationSubscriptionOrder.LastModifiedDate = DateTimeOffset.UtcNow;

            await _unitOfWork.OrganizationSubscriptionOrders.UpdateAsync(organizationSubscriptionOrder, cancellationToken);

            return (await _unitOfWork.SaveChangesAsync(cancellationToken)) > 0;
        }
    }
}