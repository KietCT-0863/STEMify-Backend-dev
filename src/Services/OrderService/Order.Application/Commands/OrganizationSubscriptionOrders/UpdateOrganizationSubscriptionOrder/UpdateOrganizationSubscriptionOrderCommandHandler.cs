using MediatR;
using Order.Application.Commands.OrganizationSubscriptionOrders.UpdateOrganizationSubscriptionOrder;
using Order.Application.Common.Interfaces;
using Order.Application.Common.Interfaces.Cache;
using Order.Application.Common.Interfaces.Grpc;
using Order.Application.Queries.OrganizationSubscriptionOrders.GetOrganizationSubscriptionOrderById;
using Order.Application.Specifications;
using Order.Domain.Entities;
using Shared.Protos.Order;

namespace Order.Application.Features.OrganizationSubscriptionOrders.Commands.UpdateOrganizationSubscriptionOrder
{
    public class UpdateOrganizationSubscriptionOrderCommandHandler : IRequestHandler<UpdateOrganizationSubscriptionOrderCommand, GrpcOrganizationSubscriptionOrderDetail>
    {
        private readonly IOrderUnitOfWork _unitOfWork;
        private readonly IGrpcCurriculumClient _curriculumClient;
        private readonly IPlanBillingCycleCacheService _planBillingCycleCacheService;
        private readonly IMediator _mediator;

        public UpdateOrganizationSubscriptionOrderCommandHandler(IOrderUnitOfWork unitOfWork, IGrpcCurriculumClient curriculumClient, IPlanBillingCycleCacheService planBillingCycleCacheService, IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _curriculumClient = curriculumClient;
            _planBillingCycleCacheService = planBillingCycleCacheService;
            _mediator = mediator;
        }

        public async Task<GrpcOrganizationSubscriptionOrderDetail> Handle(UpdateOrganizationSubscriptionOrderCommand request, CancellationToken cancellationToken)
        {
            var spec = new OrganizationSubscriptionOrderByIdSpecification(request.Id);
            var order = await _unitOfWork.OrganizationSubscriptionOrders.FirstOrDefaultAsync(spec, cancellationToken);
            if (order == null)
                throw new KeyNotFoundException($"OrganizationSubscriptionOrder with ID {request.Id} not found.");

            // validate curricula exist if provided
            if (request.CurriculumIds != null && request.CurriculumIds.Count > 0)
            {
                foreach (var curriculumId in request.CurriculumIds)
                {
                    var curriculumExists = await _curriculumClient.GetCurriculumRelations(curriculumId);
                    if (curriculumExists == null)
                        throw new KeyNotFoundException($"Curriculum with ID {curriculumId} not found.");
                }
            }

            // Apply scalar updates only when provided
            //if (request.OrganizationId.HasValue)
            //    order.OrganizationId = request.OrganizationId.Value;

            //if (request.PlanBillingCycleId.HasValue)
            //    order.PlanBillingCycleId = request.PlanBillingCycleId.Value;

            //if (request.ContractId.HasValue)
            //    order.ContractId = request.ContractId.Value;

            //if (request.ParentSubscriptionId.HasValue)
            //    order.ParentSubscriptionId = request.ParentSubscriptionId;

            //if (!string.IsNullOrEmpty(request.PlanName))
            //    order.PlanName = request.PlanName;

            //if (request.GrossAmount.HasValue)
            //    order.GrossAmount = request.GrossAmount.Value;

            //if (request.NetAmount.HasValue)
            //    order.NetAmount = request.NetAmount.Value;

            if (request.Status.HasValue)
                order.Status = request.Status.Value;

            var planBillingCycle = await _planBillingCycleCacheService.GetPlanBillingCycleByIdAsync(order.PlanBillingCycleId, cancellationToken);
            if (planBillingCycle == null)
            {
                throw new KeyNotFoundException($"PlanBillingCycle with ID {order.PlanBillingCycleId} not found.");
            }

            if (request.DiscountPercent.HasValue)
            {
                order.DiscountPercent = request.DiscountPercent.Value;
                var grossAmount = (decimal)planBillingCycle.Price;
                order.GrossAmount = grossAmount;
                var discountAmount = grossAmount * (order.DiscountPercent / 100);
                order.NetAmount = (grossAmount - discountAmount);
            }

            if (request.StartDate.HasValue)
            {
                order.StartDate = request.StartDate.Value;
                var months = (int)planBillingCycle.BillingCycle;
                if (months <= 0)
                    months = 12;

                order.EndDate = order.StartDate.AddMonths(months);
                order.Status = DateOnly.FromDateTime(request.StartDate.Value) == DateOnly.FromDateTime(DateTime.Today)
                        ? Domain.Enums.OrganizationSubscriptionOrderStatus.Active
                        : Domain.Enums.OrganizationSubscriptionOrderStatus.Pending;
            }

            if (request.MaxStudentSeats.HasValue)
                order.MaxStudentSeats = request.MaxStudentSeats.Value;

            if (request.MaxTeacherSeats.HasValue)
                order.MaxTeacherSeats = request.MaxTeacherSeats.Value;

            // ===== update curricula association (add/remove) =====
            if (request.CurriculumIds != null)
            {
                var newCurriculumIds = request.CurriculumIds.Distinct().Where(id => id > 0).ToList();
                var existingIds = order.SubscriptionOrderCurriculums?.Select(x => x.CurriculumId).ToList() ?? new List<int>();

                var toRemove = order.SubscriptionOrderCurriculums?
                    .Where(pc => !newCurriculumIds.Contains(pc.CurriculumId))
                    .ToList() ?? new List<SubscriptionOrderCurriculum>();

                var idsToAdd = newCurriculumIds.Except(existingIds).ToList();

                if (toRemove.Any())
                {
                    foreach (var pc in toRemove)
                    {
                        order.SubscriptionOrderCurriculums?.Remove(pc);
                        await _unitOfWork.SubscriptionOrderCurriculums.DeleteAsync(pc, cancellationToken);
                    }
                }

                if (idsToAdd.Any())
                {
                    foreach (var cid in idsToAdd)
                    {
                        var curriculumRelations = await _curriculumClient.GetCurriculumRelations(cid);
                        if (curriculumRelations == null)
                        {
                            throw new KeyNotFoundException($"Curriculum with ID {cid} not found.");
                        }

                        var courses = curriculumRelations.Courses.Select(c => new CourseSnapshot
                        {
                            Id = c.CourseId,
                            Title = c.Title,
                            ImageUrl = c.ImageUrl,
                            Description = c.Description,
                            Level = c.Level,
                            Code = c.Code,
                            KitId = c.KitId
                        });

                        var emulators = curriculumRelations.Emulators.Select(e => new EmulatorSnapshot
                        {
                            EmulationId = e.EmulationId,
                            Name = e.Name,
                            Description = e.Description,
                            ThumbnailUrl = e.ThumbnailUrl
                        });

                        var newItem = new SubscriptionOrderCurriculum
                        {
                            OrganizationSubscriptionOrderId = order.Id,
                            CurriculumId = cid,
                            CoursesSnapshot = courses.ToList(),
                            EmulatorsSnapshot = emulators.ToList()
                        };

                        order.SubscriptionOrderCurriculums ??= new List<SubscriptionOrderCurriculum>();
                        order.SubscriptionOrderCurriculums.Add(newItem);
                        await _unitOfWork.SubscriptionOrderCurriculums.AddAsync(newItem, cancellationToken);
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);

                order.CurriculumCount = order.SubscriptionOrderCurriculums?.Count ?? 0;
            }
            order.LastModifiedDate = DateTimeOffset.UtcNow;

            // persist changes
            await _unitOfWork.OrganizationSubscriptionOrders.UpdateAsync(order, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            var query = new GetOrganizationSubscriptionOrderByIdQuery
            {
                Id = request.Id
            };
            var result = await _mediator.Send(query, cancellationToken);

            return result;
        }
    }
}