using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Resource.Application.Commands.RubricCriterion;
using Resource.Application.Queries.RubricCriterion;
using Shared.Protos.Resource;

namespace Resource.API.Services
{
    public class RubricCriterionGrpcService : RubricCriterionService.RubricCriterionServiceBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<RubricCriterionGrpcService> _logger;

        public RubricCriterionGrpcService(IMediator mediator, ILogger<RubricCriterionGrpcService> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public override async Task<RubricCriterionResponse> CreateRubricCriterion(
            CreateRubricCriterionRequest request,
            ServerCallContext context
        )
        {
            try
            {
                var command = new CreateRubricCriterionCommand
                {
                    AssignmentQuestionId = request.AssignmentQuestionId,
                    CriterionName = request.CriterionName,
                    Description = request.Description,
                    MaxPoints = (decimal)request.MaxPoints,
                };

                var result = await _mediator.Send(command);
                return result;
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"CreateRubricCriterion failed: {ex.Message}")
                );
            }
        }

        public override async Task<RubricCriterionResponse> GetRubricCriterion(
            GetRubricCriterionRequest request,
            ServerCallContext context
        )
        {
            var query = new GetRubricCriterionByIdQuery(request.Id);
            var result = await _mediator.Send(query);

            if (result == null)
                throw new RpcException(
                    new Status(StatusCode.NotFound, $"RubricCriterion with ID {request.Id} not found.")
                );

            return result;
        }

        public override async Task<RubricCriterionResponse> UpdateRubricCriterion(
            UpdateRubricCriterionRequest request,
            ServerCallContext context
        )
        {
            try
            {
                var command = new UpdateRubricCriterionCommand
                {
                    Id = request.Id,
                    CriterionName = request.CriterionName,
                    Description = request.Description,
                    MaxPoints = (decimal)request.MaxPoints,
                };

                var result = await _mediator.Send(command);
                return result;
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"UpdateRubricCriterion failed: {ex.Message}")
                );
            }
        }

        public override async Task<Empty> DeleteRubricCriterion(
            DeleteRubricCriterionRequest request,
            ServerCallContext context
        )
        {
            try
            {
                var command = new DeleteRubricCriterionCommand { Id = request.Id };
                await _mediator.Send(command);

                return new Empty();
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"DeleteRubricCriterion failed: {ex.Message}")
                );
            }
        }

        public override async Task<PagedRubricCriterionList> QueryRubricCriterions(
            QueryRubricCriterionsRequest request,
            ServerCallContext context
        )
        {
            try
            {
                _logger.LogInformation("🎯 gRPC QueryRubricCriterions received: {@Request}", request);
                _logger.LogInformation(
                    " Request metadata: {Headers}",
                    string.Join(", ", context.RequestHeaders.Select(h => $"{h.Key}={h.Value}"))
                );

                var query = new QueryRubricCriterionsQuery
                {
                    Search = request.Search,
                    PageNumber = request.PageNumber ?? 1,
                    PageSize = request.PageSize ?? 10,
                    OrderBy = request.OrderBy,
                    AssignmentQuestionId = request.AssignmentQuestionId,
                };

                _logger.LogInformation("📋 QueryRubricCriterions query created: {@Query}", query);

                var result = await _mediator.Send(query);

                _logger.LogInformation(
                    " QueryRubricCriterions completed successfully. Result count: {Count}",
                    result?.Items?.Count ?? 0
                );

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "gRPC QueryRubricCriterions failed: {Message} | StackTrace: {StackTrace} | InnerException: {Inner}",
                    ex.Message,
                    ex.StackTrace,
                    ex.InnerException?.Message
                );
                throw new RpcException(
                    new Status(StatusCode.Internal, $"QueryRubricCriterions failed: {ex.Message}")
                );
            }
        }
    }
}
