using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Order.Application.Commands.Organizations.CreateOrganization;
using Order.Application.Commands.Organizations.DeleteOrganization;
using Order.Application.Commands.Organizations.UpdateOrganization;
using Order.Application.Queries.OrganizationCurriculums.GetOrganizationCurriculumDetails;
using Order.Application.Queries.OrganizationCurriculums.GetOrganizationCurriculumList;
using Order.Application.Queries.Organizations.GetOrganizationById;
using Order.Application.Queries.Organizations.GetOrganizationDashboard;
using Order.Application.Queries.Organizations.GetOrganizationForBulkProvisioning;
using Order.Application.Queries.Organizations.GetOrganizationList;
using Order.Application.Queries.Organizations.GetOrganizationsWithAccessByUserId;
using Order.Domain.Enums;
using ServiceStack;
using Shared.Enums;
using Shared.Protos.Order;

namespace Order.API.Services
{
    public class OrganizationGrpcService : GrpcOrganizationService.GrpcOrganizationServiceBase
    {
        private readonly IMediator _mediator;

        public OrganizationGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<GrpcOrganizationDetail> CreateOrganization(
            CreateOrganizationRequest request,
            ServerCallContext context
        )
        {
            var command = new CreateOrganizationCommand
            {
                Name = request.Name,
                Description = request.Description,
                ImageBytes = request.Image?.ToByteArray(),
                OrganizationTypeId = request.OrganizationTypeId
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<GrpcOrganizationDetail> GetOrganizationById(
            GetOrganizationRequest request,
            ServerCallContext context
        )
        {
            var query = new GetOrganizationByIdQuery
            {
                Id = request.Id
            };
            var result = await _mediator.Send(query);

            return result;
        }

        public override async Task<GrpcOrganizationDetail> UpdateOrganization(
            UpdateOrganizationRequest request,
            ServerCallContext context
        )
        {
            var command = new UpdateOrganizationCommand
            {
                Id = request.Id,
                Name = request.Name,
                Description = request.Description,
                ImageBytes = request.Image?.ToByteArray(),
                OrganizationTypeId = request.OrganizationTypeId,
                Status = string.IsNullOrEmpty(request.Status) ? null : request.Status.ToEnumOrDefault(Domain.Enums.OrganizationStatus.Active)
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<Empty> DeleteOrganization(
            DeleteOrganizationRequest request,
            ServerCallContext context
        )
        {
            var command = new DeleteOrganizationCommand { Id = request.Id };
            await _mediator.Send(command);

            return new Empty();
        }

        public override async Task<GrpcPagedOrganizationResponse> GetPagedOrganization(
            GetOrganizationParams request,
            ServerCallContext context
        )
        {
            var query = new GetOrganizationListQuery
            {
                Search = request.Search,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                OrderBy = request.OrderBy,
                IsDescending = request.SortDirection != null && request.SortDirection == SortDirection.Desc.ToString(),
                OrganizationTypeId = request.OrganizationTypeId,
                Status = string.IsNullOrEmpty(request.Status) ? null : request.Status.ToEnumOrDefault(Domain.Enums.OrganizationStatus.Active)
            };
            var result = await _mediator.Send(query);

            return result;
        }

        public override async Task<GetOrganizationDashboardResponse> GetOrganizationDashboard(
            GetOrganizationDashboardRequest request,
            ServerCallContext context
        )
        {
            var query = new GetOrganizationDashboardQuery
            {
                Id = request.Id,
                Period = request.Period
            };
            var result = await _mediator.Send(query);
            return result;
        }
        
        public override async Task<GrpcOrganizationBulkProvisioningInfo> GetOrganizationForBulkProvisioning(
            GetOrganizationRequest request,
            ServerCallContext context
        )
        {
            var query = new GetOrganizationForBulkProvisioningQuery
            {
                Id = request.Id
            };

            var result = await _mediator.Send(query);
            return result;
        }

        public override Task<GrpcOrganizationCurriculumList> GetOrganizationCurriculumByOrgId(GetOrganizationRequest request, ServerCallContext context)
        {
            var query = new GetOrganizationCurriculumListQuery
            {
                OrgId = request.Id,
                Status = request.Status.ToEnumOrDefault(OrganizationSubscriptionOrderStatus.Active)
            };
            var result = _mediator.Send(query);
            return result;
        }

        public override Task<GrpcOrganizationsWithAccessResponse> GetOrganizationsWithAccessByUserId(GetOrganizationsWithAccessByUserIdRequest request, ServerCallContext context)
        {
            var query = new GetOrganizationsWithAccessByUserIdQuery
            {
                UserId = request.UserId
            };
            var result = _mediator.Send(query);
            return result;
        }

        public async override Task<OrganizationCurriculumModel> GetOrganizationCurriculumById(GetOrganizationCurriculumByIdRequest request, ServerCallContext context)
        {
            var query = new GetOrganizationCurriculumByIdQuery
            {
                CurriculumId = request.CurriculumId,
                OrgId = request.OrganizationId
            };
            var result = await _mediator.Send(query);
            return result;
        }
    }
}