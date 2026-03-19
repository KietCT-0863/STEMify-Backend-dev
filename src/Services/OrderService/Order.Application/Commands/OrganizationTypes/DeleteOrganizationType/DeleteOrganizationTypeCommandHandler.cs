using MediatR;
using Order.Application.Common.Interfaces;

namespace Order.Application.Commands.OrganizationTypes.DeleteOrganizationType
{
    public class DeleteOrganizationTypeCommandHandler : IRequestHandler<DeleteOrganizationTypeCommand>
    {
        private readonly IOrderUnitOfWork _unitOfWork;

        public DeleteOrganizationTypeCommandHandler(IOrderUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteOrganizationTypeCommand request, CancellationToken cancellationToken)
        {
            var organization = await _unitOfWork.OrganizationTypes.FindByIdAsync(request.Id, cancellationToken);

            if (organization == null)
            {
                throw new KeyNotFoundException($"OrganizationType with Id {request.Id} not found.");
            }

            await _unitOfWork.OrganizationTypes.DeleteAsync(organization, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}