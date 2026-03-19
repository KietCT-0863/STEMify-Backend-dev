using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.CourseLearningOutcome;
using Resource.Application.Specifications.CourseLearningOutcomes;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.CourseLearningOutcome
{
    public class GetCourseLearningOutcomeListQueryHandler : IRequestHandler<GetCourseLearningOutcomeListQuery, CourseLearningOutcomeList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetCourseLearningOutcomeListQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CourseLearningOutcomeList> Handle(
            GetCourseLearningOutcomeListQuery request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var spec = new CourseLearningOutcomeWithIncludesSpecification();
                var courseLearningOutcomes = await _unitOfWork.CourseLearningOutcomes.GetAllAsync(spec, cancellationToken);

                var courseLearningOutcomeList = new CourseLearningOutcomeList();
                foreach (var courseLearningOutcome in courseLearningOutcomes)
                {
                    var response = new CourseLearningOutcomeResponse
                    {
                        Id = courseLearningOutcome.Id,
                        Name = courseLearningOutcome.Name,
                        Description = courseLearningOutcome.Description,
                        CourseId = courseLearningOutcome.CourseId,
                    };
                    courseLearningOutcomeList.CourseLearningOutcomes.Add(response);
                }

                return courseLearningOutcomeList;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"An error occurred while retrieving the CourseLearningOutcome list: {ex.Message}",
                    ex
                );
            }
        }
    }
}
