using Google.Protobuf.WellKnownTypes;
using MediatR;
using Product.Application.Common.Interfaces;
using Product.Application.Common.Interfaces.Cache;
using Product.Application.Specifications;
using Product.Domain.Entities;
using Shared.Protos.Product;

namespace Product.Application.Features.Plans.Commands.CreatePlan
{
    public class CreatePlanCommandHandler : IRequestHandler<CreatePlanCommand, GrpcPlanModel>
    {
        private readonly IProductUnitOfWork _unitOfWork;
        private readonly ICurriculumCacheService _curriculumCacheService;

        public CreatePlanCommandHandler(IProductUnitOfWork unitOfWork, ICurriculumCacheService curriculumCacheService)
        {
            _unitOfWork = unitOfWork;
            _curriculumCacheService = curriculumCacheService;
        }

        public async Task<GrpcPlanModel> Handle(CreatePlanCommand request, CancellationToken cancellationToken)
        {
            if (request.CurriculumIds != null && request.CurriculumIds.Count > 0)
            {
                foreach (var curriculumId in request.CurriculumIds)
                {
                    var curriculumExists = await _curriculumCacheService.GetCurriculumByIdAsync(curriculumId, cancellationToken);
                    if (curriculumExists == null)
                    {
                        throw new KeyNotFoundException($"Curriculum with ID {curriculumId} not found.");
                    }
                }
            }
            if (request.IsAddOn && request.PlanBillingCycleId.HasValue)
            {
                var spec = new PlanBillingCycleByIdSpecification(request.PlanBillingCycleId.Value);
                var parentPbc = await _unitOfWork.PlanBillingCycles.FirstOrDefaultAsync(spec, cancellationToken);
                if (parentPbc == null)
                {
                    throw new KeyNotFoundException($"Parent PlanBillingCycle with ID {request.PlanBillingCycleId.Value} not found.");
                }
            }
            if (request.BillingCycles == null || request.BillingCycles.Count == 0)
            {
                throw new ArgumentException("At least one billing cycle must be provided.");
            }

            // Create Plan entity
            var plan = new Plan
            {
                Name = request.Name,
                Description = request.Description,
                Status = Domain.Enums.PlanStatus.Draft,
                AccessSupportDetail = request.AccessSupportDetail,
                CurriculumCount = request.CurriculumCount,  
                MaxStudentSeats = !request.IsAddOn ? request.MaxStudentSeats : null,
                MaxTeacherSeats = !request.IsAddOn ? request.MaxTeacherSeats : null,
                PlanCurriculums = request.CurriculumIds?.Select(cid => new PlanCurriculum
                {
                    CurriculumId = cid
                }).ToList() ?? new List<PlanCurriculum>()
            };

            await _unitOfWork.Plans.AddAsync(plan, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var billingCycles = new List<PlanBillingCycle>();

            if (request.IsAddOn)
            {
                var bcDto = request.BillingCycles.First();
                billingCycles.Add(new PlanBillingCycle
                {
                    PlanId = plan.Id,
                    BillingCycle = bcDto.BillingCycle,
                    Price = bcDto.Price,
                    MaxStudentSeats = request.MaxStudentSeats,
                    MaxTeacherSeats = request.MaxTeacherSeats,
                    IsAddOn = true,
                    ParentPlanBillingCycleId = request.PlanBillingCycleId
                });
            }
            else
            {
                foreach (var bcDto in request.BillingCycles)
                {
                    billingCycles.Add(new PlanBillingCycle
                    {
                        PlanId = plan.Id,
                        BillingCycle = bcDto.BillingCycle,
                        Price = bcDto.Price,
                        MaxStudentSeats = null,
                        MaxTeacherSeats = null,
                        IsAddOn = false,
                        ParentPlanBillingCycleId = null
                    });
                }
            }

            foreach (var bc in billingCycles)
            {
                await _unitOfWork.PlanBillingCycles.AddAsync(bc, cancellationToken);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new GrpcPlanModel
            {
                Id = plan.Id,
                Name = plan.Name,
                Description = plan.Description ?? string.Empty,
                AccessSupportDetail = plan.AccessSupportDetail ?? string.Empty,
                CurriculumCount = plan.CurriculumCount,
                MaxTeacherSeats = request.MaxTeacherSeats,
                MaxStudentSeats = request.MaxStudentSeats,
                CreatedAt = Timestamp.FromDateTimeOffset(plan.CreatedDate),
                UpdatedAt = plan.LastModifiedDate.HasValue
                    ? Timestamp.FromDateTimeOffset(plan.LastModifiedDate.Value)
                    : null,
                CurriculumIds = { plan.PlanCurriculums.Select(pc => pc.CurriculumId) }
            };

            return response;
        }
    }
}