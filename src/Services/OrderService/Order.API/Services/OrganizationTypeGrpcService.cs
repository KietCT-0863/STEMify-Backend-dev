using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Order.Application.Commands.OrganizationTypes.CreateOrganizationType;
using Order.Application.Commands.OrganizationTypes.DeleteOrganizationType;
using Order.Application.Commands.OrganizationTypes.UpdateOrganizationType;
using Order.Application.Queries.OrganizationTypes.GetOrganizationTypeById;
using Order.Application.Queries.OrganizationTypes.GetOrganizationTypeList;
using Shared.Enums;
using Shared.Protos.Order;

namespace Order.API.Services
{
    public class OrganizationTypeGrpcService : GrpcOrganizationTypeService.GrpcOrganizationTypeServiceBase
    {
        private readonly IMediator _mediator;

        public OrganizationTypeGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<GrpcOrganizationTypeModel> CreateOrganizationType(
            CreateOrganizationTypeRequest request,
            ServerCallContext context
        )
        {
            var command = new CreateOrganizationTypeCommand
            {
                Name = request.Name,
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<GrpcOrganizationTypeModel> GetOrganizationTypeById(
            GetOrganizationTypeRequest request,
            ServerCallContext context
        )
        {
            var query = new GetOrganizationTypeByIdQuery
            {
                Id = request.Id
            };
            var result = await _mediator.Send(query);

            return result;
        }

        public override async Task<GrpcOrganizationTypeModel> UpdateOrganizationType(
            UpdateOrganizationTypeRequest request,
            ServerCallContext context
        )
        {
            var command = new UpdateOrganizationTypeCommand
            {
                Id = request.Id,
                Name = request.Name,
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<Empty> DeleteOrganizationType(
            DeleteOrganizationTypeRequest request,
            ServerCallContext context
        )
        {
            var command = new DeleteOrganizationTypeCommand { Id = request.Id };
            await _mediator.Send(command);

            return new Empty();
        }

        public override async Task<GrpcPagedOrganizationTypeResponse> GetPagedOrganizationType(
            GetOrganizationTypeParams request,
            ServerCallContext context
        )
        {
            var query = new GetOrganizationTypeListQuery
            {
                Search = request.Search,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                OrderBy = request.OrderBy,
                IsDescending = request.SortDirection != null && request.SortDirection == SortDirection.Desc.ToString(),
            };
            var result = await _mediator.Send(query);

            return result;
        }
    }
}