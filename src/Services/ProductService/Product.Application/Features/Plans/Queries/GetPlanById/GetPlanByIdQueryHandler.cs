using Google.Protobuf.WellKnownTypes;
using MediatR;
using Product.Application.Common.Interfaces;
using Product.Application.Common.Interfaces.Cache;
using Product.Application.Specifications;
using Shared.Protos.Product;

namespace Product.Application.Features.Plans.Queries.GetPlanById
{
    public class GetPlanByIdQueryHandler
        : IRequestHandler<GetPlanByIdQuery, GrpcPlanDetail>
    {
        private readonly IProductUnitOfWork _unitOfWork;
        private readonly ICurriculumCacheService _curriculumCacheService;

        public GetPlanByIdQueryHandler(IProductUnitOfWork unitOfWork, ICurriculumCacheService curriculumCacheService)
        {
            _unitOfWork = unitOfWork;
            _curriculumCacheService = curriculumCacheService;
        }

        public async Task<GrpcPlanDetail> Handle(
            GetPlanByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            var spec = new PlanByIdSpecification(request.Id);
            var plan = await _unitOfWork.Plans.FirstOrDefaultAsync(
                spec,
                cancellationToken
            );

            if (plan == null)
                throw new KeyNotFoundException($"Plan with ID {request.Id} not found.");

            var curriculumModels = new List<GrpcPlanCurriculumModel>();
            if (plan.PlanCurriculums != null)
            {
                var tasks = plan.PlanCurriculums
                    .Select(async pc =>
                    {
                        var cur = await _curriculumCacheService.GetCurriculumByIdAsync(pc.CurriculumId, cancellationToken);
                        return new GrpcPlanCurriculumModel
                        {
                            Id = pc.CurriculumId,
                            Title = cur?.Title ?? string.Empty,
                            ImageUrl = cur?.ImageUrl ?? string.Empty
                        };
                    })
                    .ToList();

                var results = await Task.WhenAll(tasks);
                curriculumModels.AddRange(results);
            }

            var billingCycleDetails = new List<GrpcPlanBillingCycleDetail>();
            if (plan.PlanBillingCycles != null)
            {
                billingCycleDetails.AddRange(plan.PlanBillingCycles.Select(pbc => new GrpcPlanBillingCycleDetail
                {
                    Id = pbc.Id,
                    PlanId = pbc.PlanId,
                    BillingCycle = ((Shared.Protos.Product.BillingCycle)(int)pbc.BillingCycle).ToString(),
                    Price = (double)pbc.Price,
                    MaxTeacherSeats = pbc.MaxTeacherSeats,
                    MaxStudentSeats = pbc.MaxStudentSeats,
                    IsAddOn = pbc.IsAddOn,
                    ParentPlanBillingCycleId = pbc.ParentPlanBillingCycleId
                }));
            }

            var response = new GrpcPlanDetail
            {
                Id = plan.Id,
                Name = plan.Name ?? string.Empty,
                Status = plan.Status.ToString(),
                Description = plan.Description,
                AccessSupportDetail = plan.AccessSupportDetail,
                CurriculumCount = plan.CurriculumCount,
                MaxTeacherSeats = plan.MaxTeacherSeats,
                MaxStudentSeats = plan.MaxStudentSeats,
                CreatedAt = Timestamp.FromDateTimeOffset(plan.CreatedDate),
                UpdatedAt = plan.LastModifiedDate != null
                    ? Timestamp.FromDateTimeOffset(plan.LastModifiedDate.Value)
                    : null,
            };

            response.Curriculums.AddRange(curriculumModels);
            response.PlanBillingCycles.AddRange(billingCycleDetails);

            return response;
        }
    }
}