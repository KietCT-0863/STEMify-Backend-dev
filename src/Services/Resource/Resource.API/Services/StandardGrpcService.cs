using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Resource.Application.Commands.Standard;
using Resource.Application.Queries.Standard;
using Shared.Protos.Resource;

namespace Resource.API.Services
{
    public class StandardGrpcService : StandardService.StandardServiceBase
    {
        private readonly IMediator _mediator;

        public StandardGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<StandardResponse> CreateStandard(
            CreateStandardRequest request,
            ServerCallContext context
        )
        {
            var command = new CreateStandardCommand
            {
                StandardName = request.StandardName,
                Description = request.Description
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<StandardResponse> GetStandard(
            GetStandardRequest request,
            ServerCallContext context
        )
        {
            var query = new GetStandardByIdQuery(request.Id);
            var result = await _mediator.Send(query);

            if (result == null)
                throw new RpcException(
                    new Status(StatusCode.NotFound, $"Standard with ID {request.Id} not found.")
                );

            return result;
        }

        public override async Task<StandardResponse> UpdateStandard(
            UpdateStandardRequest request,
            ServerCallContext context
        )
        {
            var command = new UpdateStandardCommand
            {
                Id = request.Id,
                StandardName = request.StandardName,
                Description = request.Description
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<Empty> DeleteStandard(
            DeleteStandardRequest request,
            ServerCallContext context
        )
        {
            var command = new DeleteStandardCommand { Id = request.Id };
            await _mediator.Send(command);

            return new Empty();
        }

        public override async Task<StandardList> ListStandards(
            Empty request,
            ServerCallContext context
        )
        {
            try
            {
                var result = await _mediator.Send(new GetStandardListQuery());
                return result;
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"ListStandards failed: {ex.Message}")
                );
            }
        }

        public override async Task<PagedStandardList> QueryStandards(
            QueryStandardsRequest request,
            ServerCallContext context
        )
        {
            try
            {
                var query = new QueryStandardsQuery
                {
                    Search = request.Search,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    OrderBy = request.OrderBy,
                };
                var result = await _mediator.Send(query);

                return result;
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"QueryStandards failed: {ex.Message}")
                );
            }
        }
    }
}
