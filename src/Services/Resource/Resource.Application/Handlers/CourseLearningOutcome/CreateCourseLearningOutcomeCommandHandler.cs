using MediatR;
using Resource.Application.Commands.CourseLearningOutcome;
using Resource.Application.Common.Interfaces;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.CourseLearningOutcome
{
    public class CreateCourseLearningOutcomeCommandHandler
        : IRequestHandler<CreateCourseLearningOutcomeCommand, CourseLearningOutcomeResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public CreateCourseLearningOutcomeCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CourseLearningOutcomeResponse> Handle(
            CreateCourseLearningOutcomeCommand request,
            CancellationToken cancellationToken
        )
        {
            var courseLearningOutcome = new Domain.Entities.CourseLearningOutcome
            {
                Name = request.Name,
                Description = request.Description,
                CourseId = request.CourseId,
                LearningOutcomeMappings = request.ProgramLearningOutcomeIds.Select(cid => new Domain.Entities.LearningOutcomeMapping
                {
                    PLOId = cid
                }).ToList(),
            };

            await _unitOfWork.CourseLearningOutcomes.AddAsync(courseLearningOutcome, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new CourseLearningOutcomeResponse()
            {
                Id = courseLearningOutcome.Id,
                Name = courseLearningOutcome.Name,
                Description = courseLearningOutcome.Description,
                CourseId = courseLearningOutcome.CourseId
            };
        }
    }
}
