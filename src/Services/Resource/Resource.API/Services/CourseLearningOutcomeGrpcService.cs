using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Resource.Application.Commands.CourseLearningOutcome;
using Resource.Application.Queries.CourseLearningOutcome;
using Shared.Protos.Resource;

namespace Resource.API.Services
{
    public class CourseLearningOutcomeGrpcService : CourseLearningOutcomeService.CourseLearningOutcomeServiceBase
    {
        private readonly IMediator _mediator;

        public CourseLearningOutcomeGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<CourseLearningOutcomeResponse> CreateCourseLearningOutcome(
            CreateCourseLearningOutcomeRequest request,
            ServerCallContext context
        )
        {
            var command = new CreateCourseLearningOutcomeCommand
            {
                Name = request.Name,
                Description = request.Description,
                CourseId = request.CourseId,
                ProgramLearningOutcomeIds = request.ProgramLearningOutcomeIds.ToList(),
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<CourseLearningOutcomeResponse> GetCourseLearningOutcome(
            GetCourseLearningOutcomeRequest request,
            ServerCallContext context
        )
        {
            var query = new GetCourseLearningOutcomeByIdQuery(request.Id);
            var result = await _mediator.Send(query);

            if (result == null)
                throw new RpcException(
                    new Status(StatusCode.NotFound, $"CourseLearningOutcome with ID {request.Id} not found.")
                );

            return result;
        }

        public override async Task<CourseLearningOutcomeResponse> UpdateCourseLearningOutcome(
            UpdateCourseLearningOutcomeRequest request,
            ServerCallContext context
        )
        {
            var command = new UpdateCourseLearningOutcomeCommand
            {
                Id = request.Id,
                Name = request.Name,
                Description = request.Description,
                CourseId = request.CourseId,
                ProgramLearningOutcomeIds = request.ProgramLearningOutcomeIds.ToList(),
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<Empty> DeleteCourseLearningOutcome(
            DeleteCourseLearningOutcomeRequest request,
            ServerCallContext context
        )
        {
            var command = new DeleteCourseLearningOutcomeCommand { Id = request.Id };
            await _mediator.Send(command);

            return new Empty();
        }

        public override async Task<CourseLearningOutcomeList> ListCourseLearningOutcomes(
            Empty request,
            ServerCallContext context
        )
        {
            try
            {
                var result = await _mediator.Send(new GetCourseLearningOutcomeListQuery());
                return result;
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"ListCourseLearningOutcomes failed: {ex.Message}")
                );
            }
        }

        public override async Task<PagedCourseLearningOutcomeList> QueryCourseLearningOutcomes(
            QueryCourseLearningOutcomesRequest request,
            ServerCallContext context
        )
        {
            var query = new QueryCourseLearningOutcomesQuery
            {
                Search = request.Search,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                OrderBy = request.OrderBy,
                CourseId = request.CourseId,
            };
            var result = await _mediator.Send(query);

            return result;
        }
    }
}
