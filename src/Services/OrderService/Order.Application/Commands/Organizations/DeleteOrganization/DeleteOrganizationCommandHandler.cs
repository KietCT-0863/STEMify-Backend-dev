using MediatR;
using Order.Application.Common.Interfaces;

namespace Order.Application.Commands.Organizations.DeleteOrganization
{
    public class DeleteOrganizationCommandHandler : IRequestHandler<DeleteOrganizationCommand>
    {
        private readonly IOrderUnitOfWork _unitOfWork;

        public DeleteOrganizationCommandHandler(IOrderUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteOrganizationCommand request, CancellationToken cancellationToken)
        {
            var organization = await _unitOfWork.Organizations.FindByIdAsync(request.Id, cancellationToken);

            if (organization == null)
            {
                throw new KeyNotFoundException($"Organization with Id {request.Id} not found.");
            }

            organization.Status = Domain.Enums.OrganizationStatus.Archived;
            organization.LastModifiedDate = DateTimeOffset.UtcNow;

            await _unitOfWork.Organizations.UpdateAsync(organization, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}