using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.CourseLearningOutcome;
using Resource.Application.Specifications.CourseLearningOutcomes;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.CourseLearningOutcome
{
    public class GetCourseLearningOutcomeByIdQueryHandler
        : IRequestHandler<GetCourseLearningOutcomeByIdQuery, CourseLearningOutcomeResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetCourseLearningOutcomeByIdQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CourseLearningOutcomeResponse> Handle(
            GetCourseLearningOutcomeByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            var spec = new CourseLearningOutcomeByIdSpecification(request.Id);
            var courseLearningOutcome = await _unitOfWork.CourseLearningOutcomes.FirstOrDefaultAsync(
                spec,
                cancellationToken
            );

            if (courseLearningOutcome == null)
                throw new KeyNotFoundException($"CourseLearningOutcome with ID {request.Id} not found.");

            var response = new CourseLearningOutcomeResponse()
            {
                Id = courseLearningOutcome.Id,
                Description = courseLearningOutcome.Description,
                Name = courseLearningOutcome.Name,
                CourseId = courseLearningOutcome.CourseId,
            };

            response.ProgramLearningOutcomeIds.AddRange(
                courseLearningOutcome.LearningOutcomeMappings?.Select(cc => cc.PLOId) ?? Enumerable.Empty<int>()
            );

            return response;
        }
    }
}
