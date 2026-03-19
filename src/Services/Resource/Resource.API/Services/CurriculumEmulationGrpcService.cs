using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Resource.Application.Commands.CurriculumEmulation;
using Shared.Protos.Resource;

namespace Resource.API.Services
{
    public class CurriculumEmulationGrpcService : CurriculumEmulationService.CurriculumEmulationServiceBase
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CurriculumGrpcService> _logger;

        public CurriculumEmulationGrpcService(IMediator mediator, ILogger<CurriculumGrpcService> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        public override async Task<Empty> CreateCurriculumEmulation(
            CreateCurriculumEmulationRequest request,
            ServerCallContext context
        )
        {
            var command = new CreateCurriculumEmulationCommand
            {
                EmulationIds = request.EmulationIds.ToList(),
                CurriculumId = request.CurriculumId,
            };

            await _mediator.Send(command);
            return new Empty();
        }

        public override async Task<Empty> DeleteCurriculumEmulation(
            DeleteCurriculumEmulationRequest request,
            ServerCallContext context
        )
        {
            var command = new DeleteCurriculumEmulationCommand
            {
                EmulationIds = request.EmulationIds.ToList(),
                CurriculumId = request.CurriculumId
            };
            await _mediator.Send(command);

            return new Empty();
        }
    }
}
