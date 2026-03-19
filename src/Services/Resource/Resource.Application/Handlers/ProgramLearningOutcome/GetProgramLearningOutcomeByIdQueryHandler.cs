using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.ProgramLearningOutcome;
using Resource.Application.Specifications.ProgramLearningOutcomes;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.ProgramLearningOutcome
{
    public class GetProgramLearningOutcomeByIdQueryHandler
        : IRequestHandler<GetProgramLearningOutcomeByIdQuery, ProgramLearningOutcomeResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetProgramLearningOutcomeByIdQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ProgramLearningOutcomeResponse> Handle(
            GetProgramLearningOutcomeByIdQuery request,
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

            var response = new ProgramLearningOutcomeResponse()
            {
                Id = programLearningOutcome.Id,
                Description = programLearningOutcome.Description,
                Name = programLearningOutcome.Name,
                CurriculumId = programLearningOutcome.CurriculumId,
            };

            return response;
        }
    }
}
