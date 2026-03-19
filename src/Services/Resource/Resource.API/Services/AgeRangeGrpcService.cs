using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Resource.Application.Commands.AgeRange;
using Resource.Application.Queries.AgeRange;
using Shared.Protos.Resource;

namespace Resource.API.Services
{
    public class AgeRangeGrpcService : AgeRangeService.AgeRangeServiceBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<AgeRangeGrpcService> _logger;

        public AgeRangeGrpcService(IMediator mediator, ILogger<AgeRangeGrpcService> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public override async Task<AgeRangeResponse> CreateAgeRange(
            CreateAgeRangeRequest request,
            ServerCallContext context
        )
        {
            var command = new CreateAgeRangeCommand
            {
                AgeRangeLabel = request.AgeRangeLabel,
                MinAge = request.MinAge,
                MaxAge = request.MaxAge,
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<AgeRangeResponse> GetAgeRange(
            GetAgeRangeRequest request,
            ServerCallContext context
        )
        {
            var query = new GetAgeRangeByIdQuery(request.Id);
            var result = await _mediator.Send(query);

            if (result == null)
                throw new RpcException(
                    new Status(StatusCode.NotFound, $"AgeRange with ID {request.Id} not found.")
                );

            return result;
        }

        public override async Task<AgeRangeResponse> UpdateAgeRange(
            UpdateAgeRangeRequest request,
            ServerCallContext context
        )
        {
            var command = new UpdateAgeRangeCommand
            {
                Id = request.Id,
                AgeRangeLabel = request.AgeRangeLabel,
                MinAge = request.MinAge,
                MaxAge = request.MaxAge,
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<Empty> DeleteAgeRange(
            DeleteAgeRangeRequest request,
            ServerCallContext context
        )
        {
            var command = new DeleteAgeRangeCommand { Id = request.Id };
            await _mediator.Send(command);

            return new Empty();
        }

        public override async Task<AgeRangeList> ListAgeRanges(
            Empty request,
            ServerCallContext context
        )
        {
            try
            {
                var result = await _mediator.Send(new GetAgeRangeListQuery());
                return result;
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"ListAgeRanges failed: {ex.Message}")
                );
            }
        }

        public override async Task<PagedAgeRangeList> QueryAgeRanges(
            QueryAgeRangesRequest request,
            ServerCallContext context
        )
        {
            try
            {
                _logger.LogInformation("🎯 gRPC QueryAgeRanges received: {@Request}", request);
                _logger.LogInformation(
                    " Request metadata: {Headers}",
                    string.Join(", ", context.RequestHeaders.Select(h => $"{h.Key}={h.Value}"))
                );

                var query = new QueryAgeRangesQuery
                {
                    Search = request.Search,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize,
                    OrderBy = request.OrderBy,
                    Age = request.Age,
                };

                _logger.LogInformation("📋 QueryAgeRanges query created: {@Query}", query);

                var result = await _mediator.Send(query);

                _logger.LogInformation(
                    " QueryAgeRanges completed successfully. Result count: {Count}",
                    result?.Items?.Count ?? 0
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "gRPC QueryAgeRanges failed: {Message} | StackTrace: {StackTrace} | InnerException: {Inner}",
                    ex.Message,
                    ex.StackTrace,
                    ex.InnerException?.Message
                );
                throw new RpcException(
                    new Status(StatusCode.Internal, $"QueryAgeRanges failed: {ex.Message}")
                );
            }
        }
    }
}
