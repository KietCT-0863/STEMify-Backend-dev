using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Product.Application.Features.Plans.Commands.CreatePlan;
using Product.Application.Features.Plans.Commands.DeletePlan;
using Product.Application.Features.Plans.Commands.UpdatePlan;
using Product.Application.Features.Plans.Queries.GetPlanById;
using Product.Application.Features.Plans.Queries.GetPlanList;
using Product.Application.Models;
using Shared.Enums;
using Shared.Extensions;
using Shared.Protos.Product;

namespace Product.API.Services
{
    public class PlanGrpcService : GrpcPlanService.GrpcPlanServiceBase
    {
        private readonly IMediator _mediator;

        public PlanGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<GrpcPlanResponse> CreatePlan(
            CreatePlanRequest request,
            ServerCallContext context
        )
        {
            var billingCycles = request.BillingCycles?.Select(bcReq =>
            {
                Domain.Enums.BillingCycle parsed = Domain.Enums.BillingCycle.Annual;
                if (!string.IsNullOrWhiteSpace(bcReq.BillingCycle))
                {
                    System.Enum.TryParse<Domain.Enums.BillingCycle>(bcReq.BillingCycle, true, out parsed);
                }

                return new BillingCycleDto
                {
                    BillingCycle = parsed,
                    Price = (decimal)bcReq.Price
                };
            }).ToList() ?? new List<BillingCycleDto>();

            var command = new CreatePlanCommand
            {
                Name = request.Name,
                Description = request.Description,
                CurriculumCount = request.CurriculumCount,
                AccessSupportDetail = request.AccessSupportDetail,
                MaxTeacherSeats = request.MaxTeacherSeats,
                MaxStudentSeats = request.MaxStudentSeats,
                CurriculumIds = request.CurriculumIds?.ToList() ?? new List<int>(),
                BillingCycles = billingCycles,
                IsAddOn = request.IsAddOn,
                PlanBillingCycleId = request.ParentPlanBillingCycleId
            };

            var result = await _mediator.Send(command);
            return new GrpcPlanResponse { Plan = result };
        }

        public override async Task<GrpcPlanDetail> GetPlanById(
            GetPlanRequest request,
            ServerCallContext context
        )
        {
            var query = new GetPlanByIdQuery
            {
                Id = request.Id
            };
            var result = await _mediator.Send(query);

            return result;
        }

        public override async Task<GrpcPlanResponse> UpdatePlan(
            UpdatePlanRequest request,
            ServerCallContext context
        )
        {
            var billingCycles = request.BillingCycles?.Select(bcReq =>
            {
                Domain.Enums.BillingCycle parsed = Domain.Enums.BillingCycle.Annual;
                if (!string.IsNullOrWhiteSpace(bcReq.BillingCycle))
                {
                    System.Enum.TryParse<Domain.Enums.BillingCycle>(bcReq.BillingCycle, true, out parsed);
                }

                return new BillingCycleDto
                {
                    BillingCycle = parsed,
                    Price = (decimal)bcReq.Price
                };
            }).ToList();

            var command = new UpdatePlanCommand
            {
                Id = request.Id,
                Name = request.Name,
                CurriculumCount = request.CurriculumCount,
                Status = request.Status.ToEnumOrNull<Domain.Enums.PlanStatus>(),
                Description = request.Description,
                AccessSupportDetail = request.AccessSupportDetail,
                MaxTeacherSeats = request.MaxTeacherSeats,
                MaxStudentSeats = request.MaxStudentSeats,
                CurriculumIds = request.CurriculumIds?.ToList(),
                BillingCycles = billingCycles,
                IsAddOn = request.IsAddOn,
                PlanBillingCycleId = request.ParentPlanBillingCycleId
            };

            var result = await _mediator.Send(command);
            return new GrpcPlanResponse { Plan = result };
        }

        public override async Task<Empty> DeletePlan(
            DeletePlanRequest request,
            ServerCallContext context
        )
        {
            var command = new DeletePlanCommand { Id = request.Id };
            await _mediator.Send(command);

            return new Empty();
        }

        public override async Task<GrpcPagedPlanResponse> GetPagedPlan(
            GetPlanParams request,
            ServerCallContext context
        )
        {
            Domain.Enums.BillingCycle? billingCycle = null;
            if (!string.IsNullOrWhiteSpace(request.BillingCycle))
            {
                if (
                    System.Enum.TryParse<Domain.Enums.BillingCycle>(
                        request.BillingCycle,
                        true,
                        out var parsedBillingCycle
                    )
                )
                {
                    billingCycle = parsedBillingCycle;
                }
            }
            var query = new GetPlanListQuery
            {
                Search = request.Search,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                OrderBy = request.OrderBy,
                Status = request.Status.ToEnumOrNull<Domain.Enums.PlanStatus>(),
                IsDescending = request.SortDirection != null && request.SortDirection == SortDirection.Desc.ToString(),
                BillingCycle = billingCycle,
                IsAddOn = request.IsAddOn,
            };
            var result = await _mediator.Send(query);

            return result;
        }
    }
}