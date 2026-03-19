using MediatR;
using Product.Application.Common.Interfaces;

namespace Product.Application.Features.Plans.Commands.DeletePlan
{
    public class DeletePlanCommandHandler : IRequestHandler<DeletePlanCommand, bool>
    {
        private readonly IProductUnitOfWork _unitOfWork;

        public DeletePlanCommandHandler(IProductUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<bool> Handle(DeletePlanCommand request, CancellationToken cancellationToken)
        {
            var plan = await _unitOfWork.Plans.FindByIdAsync(
                request.Id,
                cancellationToken
            );
            if (plan == null)
                throw new KeyNotFoundException($"Plan with ID {request.Id} not found.");

            if(plan.Status == Domain.Enums.PlanStatus.Draft)
               await _unitOfWork.Plans.DeleteAsync(plan,cancellationToken);
            else
                plan.Status = Domain.Enums.PlanStatus.Archived;

            return (await _unitOfWork.SaveChangesAsync(cancellationToken)) > 0;
        }
    }
}