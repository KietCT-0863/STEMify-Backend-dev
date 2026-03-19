using MediatR;
using Resource.Application.Commands.CurriculumEmulation;
using Resource.Application.Common.Interfaces;
using Resource.Application.Specifications.Curriculums;
using Shared.Exceptions;

namespace Resource.Application.Handlers.CurriculumEmulation
{
    public class DeleteCurriculumEmulationCommandHandler : IRequestHandler<DeleteCurriculumEmulationCommand>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public DeleteCurriculumEmulationCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task Handle(DeleteCurriculumEmulationCommand request, CancellationToken cancellationToken)
        {

            var spec = new CurriculumByIdSpecification(request.CurriculumId);
            var curriculum =
                await _unitOfWork.Curriculums.FirstOrDefaultAsync(spec, cancellationToken)
                ?? throw new NotFoundException($"Curriculum with {request.CurriculumId} not found");

            var curriculumEmulations = new List<Domain.Entities.CurriculumEmulation>();
            foreach (var emulationId in request.EmulationIds)
            {
                var curriculumEmulation = await _unitOfWork.CurriculumEmulations.FindOneAsync
                    (cc => cc.CurriculumId == request.CurriculumId && cc.EmulationId == emulationId, cancellationToken);
                if (curriculumEmulation == null)
                {
                    throw new InvalidOperationException(
                        $"Course with ID '{emulationId}' is not associated with Curriculum ID '{request.CurriculumId}'.");
                }
                curriculumEmulations.Add(curriculumEmulation);
            }

            await _unitOfWork.CurriculumEmulations.DeleteRangeAsync(curriculumEmulations, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
