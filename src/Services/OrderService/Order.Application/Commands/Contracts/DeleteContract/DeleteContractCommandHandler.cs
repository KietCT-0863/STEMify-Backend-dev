using MediatR;
using Order.Application.Common.Interfaces;

namespace Order.Application.Commands.Contracts.DeleteContract
{
    public class DeleteContractCommandHandler : IRequestHandler<DeleteContractCommand>
    {
        private readonly IOrderUnitOfWork _unitOfWork;

        public DeleteContractCommandHandler(IOrderUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteContractCommand request, CancellationToken cancellationToken)
        {
            var contract = await _unitOfWork.Contracts.FindByIdAsync(request.Id, cancellationToken);

            if (contract == null)
            {
                throw new KeyNotFoundException($"Contract with Id {request.Id} not found.");
            }

            contract.Status = Domain.Enums.ContractStatus.Archived;
            contract.LastModifiedDate = DateTimeOffset.UtcNow;

            await _unitOfWork.Contracts.UpdateAsync(contract, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}