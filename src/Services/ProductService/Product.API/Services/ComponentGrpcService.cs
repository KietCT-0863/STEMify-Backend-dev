using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Product.Application.Features.Component.Commands;
using Product.Application.Features.Component.Queries;
using Shared.Protos.Product;

namespace Product.API.Services
{
    public class ComponentGrpcService : GrpcComponentService.GrpcComponentServiceBase
    {
        private readonly IMediator _mediator;

        public ComponentGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<ComponentResponse> CreateComponent(
            CreateComponentRequest request,
            ServerCallContext context
        )
        {
            var command = new CreateComponentCommand
            {
                ImageBytes = request.Image?.ToByteArray(),
                Name = request.Name,
                Description = request.Description,
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<ComponentResponse> UpdateComponent(
            UpdateComponentRequest request,
            ServerCallContext context
        )
        {
            var command = new UpdateComponentCommand
            {
                Id = request.Id,
                Name = request.Name,
                Description = request.Description,
                ImageBytes = request.Image?.ToByteArray(),
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<Empty> DeleteComponent(
            DeleteComponentRequest request,
            ServerCallContext context
        )
        {
            var command = new DeleteComponentCommand { Id = request.Id };
            await _mediator.Send(command);

            return new Empty();
        }

        public override async Task<PagedComponentList> QueryComponents(
            QueryComponentsRequest request,
            ServerCallContext context
        )
        {
            var query = new GetComponentListQuery
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                Search = request.Search,
            };

            var result = await _mediator.Send(query);
            return result;
        }

        public override async Task<ComponentResponse> GetComponentById(
            GetComponentRequest request,
            ServerCallContext context
        )
        {
            var query = new GetComponentByIdQuery { Id = request.Id };
            var result = await _mediator.Send(query);
            return result;
        }
    }
}
