using MediatR;
using Product.Application.Common.Interfaces;
using Product.Application.Specifications;
using Shared.Protos.Product;

namespace Product.Application.Features.PlanBillingCycles.Queries.GetPlanBillingCycleById
{
    public class GetPlanBillingCycleByIdQueryHandler
        : IRequestHandler<GetPlanBillingCycleByIdQuery, GrpcPlanBillingCycleModel>
    {
        private readonly IProductUnitOfWork _unitOfWork;

        public GetPlanBillingCycleByIdQueryHandler(IProductUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<GrpcPlanBillingCycleModel> Handle(
            GetPlanBillingCycleByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            var spec = new PlanBillingCycleByIdSpecification(request.Id);
            var planBillingCycle = await _unitOfWork.PlanBillingCycles.FirstOrDefaultAsync(
                spec,
                cancellationToken
            );

            if (planBillingCycle == null)
                throw new KeyNotFoundException($"PlanBillingCycle with ID {request.Id} not found.");

            var response = new GrpcPlanBillingCycleModel
            {
                Id = planBillingCycle.Id,
                AccessSupportDetail = planBillingCycle.Plan.AccessSupportDetail,
                BillingCycle = (BillingCycle)((int)planBillingCycle.BillingCycle),
                Description = planBillingCycle.Plan.Description,
                IsAddOn = planBillingCycle.IsAddOn,
                Name = planBillingCycle.Plan.Name,
                CurriculumCount = planBillingCycle.Plan.CurriculumCount,
                MaxTeacherSeats = planBillingCycle.MaxTeacherSeats,
                MaxStudentSeats = planBillingCycle.MaxStudentSeats,
                Price = (double)planBillingCycle.Price,
            };

            return response;
        }
    }
}