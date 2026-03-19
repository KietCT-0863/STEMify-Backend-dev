using Google.Protobuf.WellKnownTypes;
using MediatR;
using Product.Application.Common.Interfaces;
using Product.Application.Common.Interfaces.Cache;
using Product.Application.Specifications;
using Product.Domain.Entities;
using Shared.Protos.Product;

namespace Product.Application.Features.Plans.Commands.UpdatePlan
{
    public class UpdatePlanCommandHandler : IRequestHandler<UpdatePlanCommand, GrpcPlanModel>
    {
        private readonly IProductUnitOfWork _unitOfWork;
        private readonly ICurriculumCacheService _curriculumCacheService;

        public UpdatePlanCommandHandler(IProductUnitOfWork unitOfWork, ICurriculumCacheService curriculumCacheService)
        {
            _unitOfWork = unitOfWork;
            _curriculumCacheService = curriculumCacheService;
        }

        public async Task<GrpcPlanModel> Handle(UpdatePlanCommand request, CancellationToken cancellationToken)
        {
            var planByIdSpecification = new PlanByIdSpecification(request.Id);
            var plan = await _unitOfWork.Plans.FirstOrDefaultAsync(planByIdSpecification, cancellationToken);
            if (plan == null)
                throw new KeyNotFoundException($"Plan with ID {request.Id} not found.");

            if (request.CurriculumIds != null && request.CurriculumIds.Count > 0)
            {
                foreach (var curriculumId in request.CurriculumIds)
                {
                    var curriculumExists = await _curriculumCacheService.GetCurriculumByIdAsync(
                        curriculumId,
                        cancellationToken);
                    if (curriculumExists == null)
                    {
                        throw new KeyNotFoundException($"Curriculum with ID {curriculumId} not found.");
                    }
                }
            }

            if (request.IsAddOn == true && request.PlanBillingCycleId.HasValue)
            {
                var spec = new PlanBillingCycleByIdSpecification(request.PlanBillingCycleId.Value);
                var parentPbc = await _unitOfWork.PlanBillingCycles.FirstOrDefaultAsync(spec, cancellationToken);
                if (parentPbc == null)
                {
                    throw new KeyNotFoundException($"Parent PlanBillingCycle with ID {request.PlanBillingCycleId.Value} not found.");
                }
            }

            if (!string.IsNullOrEmpty(request.Name))
                plan.Name = request.Name;

            if (request.Description != null)
                plan.Description = request.Description;

            if (request.AccessSupportDetail != null)
                plan.AccessSupportDetail = request.AccessSupportDetail;

            if (request.Status.HasValue)
                plan.Status = request.Status.Value;

            if (request.CurriculumCount.HasValue)
                plan.CurriculumCount = request.CurriculumCount.Value;

            bool isChangingAddonStatus = request.IsAddOn.HasValue;
            bool newIsAddOn = request.IsAddOn ?? (await _unitOfWork.PlanBillingCycles.AnyAsync(
                pb => pb.PlanId == plan.Id && pb.IsAddOn,
                cancellationToken));

            if (!newIsAddOn)
            {
                if (request.MaxStudentSeats.HasValue)
                    plan.MaxStudentSeats = request.MaxStudentSeats.Value;

                if (request.MaxTeacherSeats.HasValue)
                    plan.MaxTeacherSeats = request.MaxTeacherSeats.Value;
            }
            else
            {
                plan.MaxStudentSeats = null;
                plan.MaxTeacherSeats = null;
            }

            if (request.CurriculumIds != null && request.CurriculumIds.Count > 0)
            {
                var newCurriculumIds = request.CurriculumIds.Distinct().Where(id => id > 0).ToList();
                var existingCurriculumIds = plan.PlanCurriculums?.Select(pc => pc.CurriculumId).ToList() ?? new List<int>();

                var curriculumsToRemove = plan.PlanCurriculums?
                    .Where(pc => !newCurriculumIds.Contains(pc.CurriculumId))
                    .ToList() ?? new List<PlanCurriculum>();

                var curriculumIdsToAdd = newCurriculumIds
                    .Except(existingCurriculumIds)
                    .ToList();

                if (curriculumsToRemove.Any())
                {
                    foreach (var pc in curriculumsToRemove)
                    {
                        plan.PlanCurriculums?.Remove(pc);
                        await _unitOfWork.PlanCurriculums.DeleteAsync(pc, cancellationToken);
                    }
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                if (curriculumIdsToAdd.Any())
                {
                    foreach (var curriculumId in curriculumIdsToAdd)
                    {
                        var newPlanCurriculum = new PlanCurriculum
                        {
                            PlanId = plan.Id,
                            CurriculumId = curriculumId
                        };
                        plan.PlanCurriculums?.Add(newPlanCurriculum);
                        await _unitOfWork.PlanCurriculums.AddAsync(newPlanCurriculum, cancellationToken);
                    }
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            if (isChangingAddonStatus || (request.BillingCycles != null && request.BillingCycles.Count > 0))
            {
                // Get existing billing cycles
                var existingBillingCycles = await _unitOfWork.PlanBillingCycles
                    .FindAsync(pbc => pbc.PlanId == plan.Id, cancellationToken);

                if (existingBillingCycles != null && existingBillingCycles.Any())
                {
                    foreach (var bc in existingBillingCycles)
                    {
                        await _unitOfWork.PlanBillingCycles.DeleteAsync(bc, cancellationToken);
                    }
                    await _unitOfWork.SaveChangesAsync(cancellationToken);
                }

                var billingCycles = new List<PlanBillingCycle>();

                if (newIsAddOn)
                {
                    // Create single addon billing cycle
                    if (request.BillingCycles != null && request.BillingCycles.Count > 0)
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
                }
                else
                {
                    if (request.BillingCycles != null && request.BillingCycles.Count > 0)
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
                }

                // Add new billing cycles
                foreach (var bc in billingCycles)
                {
                    await _unitOfWork.PlanBillingCycles.AddAsync(bc, cancellationToken);
                }
            }

            // Save all changes
            await _unitOfWork.Plans.UpdateAsync(plan, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Build response
            var responseCurriculumIds = plan.PlanCurriculums?
                .Select(pc => pc.CurriculumId)
                .ToList() ?? new List<int>();

            var response = new GrpcPlanModel
            {
                Id = plan.Id,
                Name = plan.Name,
                Description = plan.Description ?? string.Empty,
                AccessSupportDetail = plan.AccessSupportDetail ?? string.Empty,
                CurriculumCount = plan.CurriculumCount,
                MaxTeacherSeats = plan.MaxTeacherSeats ?? 0,
                MaxStudentSeats = plan.MaxStudentSeats ?? 0,
                CreatedAt = Timestamp.FromDateTimeOffset(plan.CreatedDate),
                UpdatedAt = plan.LastModifiedDate.HasValue
                    ? Timestamp.FromDateTimeOffset(plan.LastModifiedDate.Value)
                    : null,
                CurriculumIds = { responseCurriculumIds }
            };

            return response;
        }
    }
}