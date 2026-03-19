using MediatR;
using Resource.Application.Commands.ProgramLearningOutcome;
using Resource.Application.Common.Interfaces;
using Resource.Application.Specifications.ProgramLearningOutcomes;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.ProgramLearningOutcome
{
    public class UpdateProgramLearningOutcomeCommandHandler
        : IRequestHandler<UpdateProgramLearningOutcomeCommand, ProgramLearningOutcomeResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public UpdateProgramLearningOutcomeCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ProgramLearningOutcomeResponse> Handle(
            UpdateProgramLearningOutcomeCommand request,
            CancellationToken cancellationToken
        )
        {
            var spec = new ProgramLearningOutcomeByIdSpecification(request.Id);
            var programLearningOutcome = await _unitOfWork.ProgramLearningOutcomes.FirstOrDefaultAsync(
                spec,
                cancellationToken
            );
            if (programLearningOutcome == null)
                throw new KeyNotFoundException($"ProgramLearningOutcome with ID {request.Id} not found.");

            if (!string.IsNullOrEmpty(request.Name))
                programLearningOutcome.Name = request.Name;
            if (!string.IsNullOrEmpty(request.Description))
                programLearningOutcome.Description = request.Description;
            if (request.CurriculumId.HasValue)
                programLearningOutcome.CurriculumId = request.CurriculumId.Value;

            await _unitOfWork.ProgramLearningOutcomes.UpdateAsync(programLearningOutcome, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new ProgramLearningOutcomeResponse()
            {
                Id = programLearningOutcome.Id,
                Name = programLearningOutcome.Name,
                Description = programLearningOutcome.Description,
                CurriculumId = programLearningOutcome.CurriculumId,
            };

            return response;
        }
    }
}
