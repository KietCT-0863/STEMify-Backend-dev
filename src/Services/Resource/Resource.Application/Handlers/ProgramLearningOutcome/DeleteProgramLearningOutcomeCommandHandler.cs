using MediatR;
using Resource.Application.Commands.ProgramLearningOutcome;
using Resource.Application.Common.Interfaces;

namespace Resource.Application.Handlers.ProgramLearningOutcome
{
    public class DeleteProgramLearningOutcomeCommandHandler : IRequestHandler<DeleteProgramLearningOutcomeCommand>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public DeleteProgramLearningOutcomeCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteProgramLearningOutcomeCommand request, CancellationToken cancellationToken)
        {
            var programLearningOutcome = await _unitOfWork.ProgramLearningOutcomes.FindByIdForUpdateAsync(
                request.Id,
                cancellationToken
            );
            if (programLearningOutcome == null)
                throw new KeyNotFoundException($"ProgramLearningOutcome with ID {request.Id} not found.");

            await _unitOfWork.ProgramLearningOutcomes.DeleteAsync(programLearningOutcome, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
