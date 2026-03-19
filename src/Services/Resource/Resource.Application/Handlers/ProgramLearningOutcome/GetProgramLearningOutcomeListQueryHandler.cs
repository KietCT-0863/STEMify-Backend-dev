using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.ProgramLearningOutcome;
using Resource.Application.Specifications.ProgramLearningOutcomes;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.ProgramLearningOutcome
{
    public class GetProgramLearningOutcomeListQueryHandler : IRequestHandler<GetProgramLearningOutcomeListQuery, ProgramLearningOutcomeList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetProgramLearningOutcomeListQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<ProgramLearningOutcomeList> Handle(
            GetProgramLearningOutcomeListQuery request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var spec = new ProgramLearningOutcomeWithIncludesSpecification();
                var programLearningOutcomes = await _unitOfWork.ProgramLearningOutcomes.GetAllAsync(spec, cancellationToken);

                var programLearningOutcomeList = new ProgramLearningOutcomeList();
                foreach (var programLearningOutcome in programLearningOutcomes)
                {
                    var response = new ProgramLearningOutcomeResponse
                    {
                        Id = programLearningOutcome.Id,
                        Name = programLearningOutcome.Name,
                        Description = programLearningOutcome.Description,
                        CurriculumId = programLearningOutcome.CurriculumId,
                    };
                    programLearningOutcomeList.ProgramLearningOutcomes.Add(response);
                }

                return programLearningOutcomeList;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"An error occurred while retrieving the ProgramLearningOutcome list: {ex.Message}",
                    ex
                );
            }
        }
    }
}
