using MediatR;
using Resource.Application.Commands.ProgramLearningOutcome;
using Resource.Application.Common.Interfaces;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.ProgramLearningOutcome
{
    public class CreateProgramLearningOutcomeCommandHandler
        : IRequestHandler<CreateProgramLearningOutcomeCommand, ProgramLearningOutcomeResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public CreateProgramLearningOutcomeCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ProgramLearningOutcomeResponse> Handle(
            CreateProgramLearningOutcomeCommand request,
            CancellationToken cancellationToken
        )
        {
            var programLearningOutcome = new Domain.Entities.ProgramLearningOutcome
            {
                Name = request.Name,
                Description = request.Description,
                CurriculumId = request.CurriculumId
            };

            await _unitOfWork.ProgramLearningOutcomes.AddAsync(programLearningOutcome, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new ProgramLearningOutcomeResponse()
            {
                Id = programLearningOutcome.Id,
                Name = programLearningOutcome.Name,
                Description = programLearningOutcome.Description,
                CurriculumId = programLearningOutcome.CurriculumId
            };
        }
    }
}
