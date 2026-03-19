using MediatR;
using Resource.Application.Commands.CurriculumEmulation;
using Resource.Application.Common.Interfaces;
using Resource.Application.Common.Interfaces.Grpc;
using Shared.Exceptions;

namespace Resource.Application.Handlers.CurriculumEmulation
{
    public class CreateCurriculumEmulationCommandHandler : IRequestHandler<CreateCurriculumEmulationCommand>
    {
        private readonly IResourceUnitOfWork _unitOfWork;
        private readonly IGrpcEmulationClient _emulationClient;

        public CreateCurriculumEmulationCommandHandler(
            IGrpcEmulationClient emulationClient,
            IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
            _emulationClient = emulationClient;
        }

        public async Task Handle(CreateCurriculumEmulationCommand request, CancellationToken cancellationToken)
        {
            var curriculumEmulations = new List<Domain.Entities.CurriculumEmulation>();
            if ((await _unitOfWork.Curriculums.AnyAsync(c => c.Id == request.CurriculumId, cancellationToken)) == false)
            {
                throw new NotFoundException($"Curriculum with ID '{request.CurriculumId}' does not exist.");
            }

            foreach (var emulationId in request.EmulationIds)
            {
                var emulationExists = await _emulationClient.GetEmulationByIdAsync(emulationId) ?? throw new NotFoundException($"Emulation with ID '{emulationId}' does not exist.");
                
                if ((await _unitOfWork.CurriculumEmulations.AnyAsync(cc => cc.CurriculumId == request.CurriculumId && cc.EmulationId == emulationId, cancellationToken)))
                {
                    throw new InvalidOperationException($"Course with ID '{emulationId}' is already associated with Curriculum ID '{request.CurriculumId}'.");
                }

                curriculumEmulations.Add(new Domain.Entities.CurriculumEmulation
                {
                    CurriculumId = request.CurriculumId,
                    EmulationId = emulationId,
                });
            }
            await _unitOfWork.CurriculumEmulations.AddRangeAsync(curriculumEmulations, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
