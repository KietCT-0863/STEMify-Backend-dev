using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Resource.Application.Commands.ProgramLearningOutcome;
using Resource.Application.Queries.ProgramLearningOutcome;
using Shared.Protos.Resource;

namespace Resource.API.Services
{
    public class ProgramLearningOutcomeGrpcService : ProgramLearningOutcomeService.ProgramLearningOutcomeServiceBase
    {
        private readonly IMediator _mediator;

        public ProgramLearningOutcomeGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<ProgramLearningOutcomeResponse> CreateProgramLearningOutcome(
            CreateProgramLearningOutcomeRequest request,
            ServerCallContext context
        )
        {
            var command = new CreateProgramLearningOutcomeCommand
            {
                Name = request.Name,
                Description = request.Description,
                CurriculumId = request.CurriculumId
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<ProgramLearningOutcomeResponse> GetProgramLearningOutcome(
            GetProgramLearningOutcomeRequest request,
            ServerCallContext context
        )
        {
            var query = new GetProgramLearningOutcomeByIdQuery(request.Id);
            var result = await _mediator.Send(query);

            if (result == null)
                throw new RpcException(
                    new Status(StatusCode.NotFound, $"ProgramLearningOutcome with ID {request.Id} not found.")
                );

            return result;
        }

        public override async Task<ProgramLearningOutcomeResponse> UpdateProgramLearningOutcome(
            UpdateProgramLearningOutcomeRequest request,
            ServerCallContext context
        )
        {
            var command = new UpdateProgramLearningOutcomeCommand
            {
                Id = request.Id,
                Name = request.Name,
                Description = request.Description,
                CurriculumId = request.CurriculumId
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<Empty> DeleteProgramLearningOutcome(
            DeleteProgramLearningOutcomeRequest request,
            ServerCallContext context
        )
        {
            var command = new DeleteProgramLearningOutcomeCommand { Id = request.Id };
            await _mediator.Send(command);

            return new Empty();
        }

        public override async Task<ProgramLearningOutcomeList> ListProgramLearningOutcomes(
            Empty request,
            ServerCallContext context
        )
        {
            try
            {
                var result = await _mediator.Send(new GetProgramLearningOutcomeListQuery());
                return result;
            }
            catch (Exception ex)
            {
                throw new RpcException(
                    new Status(StatusCode.Internal, $"ListProgramLearningOutcomes failed: {ex.Message}")
                );
            }
        }

        public override async Task<PagedProgramLearningOutcomeList> QueryProgramLearningOutcomes(
            QueryProgramLearningOutcomesRequest request,
            ServerCallContext context
        )
        {
            var query = new QueryProgramLearningOutcomesQuery
            {
                Search = request.Search,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
                OrderBy = request.OrderBy,
                CurriculumId = request.CurriculumId,
            };
            var result = await _mediator.Send(query);

            return result;
        }
    }
}
