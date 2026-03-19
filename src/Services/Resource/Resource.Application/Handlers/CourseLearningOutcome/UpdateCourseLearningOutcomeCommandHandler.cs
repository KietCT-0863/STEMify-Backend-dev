using MediatR;
using Resource.Application.Commands.CourseLearningOutcome;
using Resource.Application.Common.Interfaces;
using Resource.Application.Specifications.CourseLearningOutcomes;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.CourseLearningOutcome
{
    public class UpdateCourseLearningOutcomeCommandHandler
        : IRequestHandler<UpdateCourseLearningOutcomeCommand, CourseLearningOutcomeResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public UpdateCourseLearningOutcomeCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<CourseLearningOutcomeResponse> Handle(
            UpdateCourseLearningOutcomeCommand request,
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

            if (!string.IsNullOrEmpty(request.Name))
                courseLearningOutcome.Name = request.Name;
            if (!string.IsNullOrEmpty(request.Description))
                courseLearningOutcome.Description = request.Description;
            if (request.CourseId.HasValue)
                courseLearningOutcome.CourseId = request.CourseId.Value;

            if (request.ProgramLearningOutcomeIds != null && request.ProgramLearningOutcomeIds.Any())
            {
                var programLearningOutcomeToRemove = courseLearningOutcome.LearningOutcomeMappings
                    .Where(cs => !request.ProgramLearningOutcomeIds.Contains(cs.PLOId))
                    .ToList();

                foreach (var programLearningOutcome in programLearningOutcomeToRemove)
                    courseLearningOutcome.LearningOutcomeMappings.Remove(programLearningOutcome);

                foreach (var programLearningOutcomeId in request.ProgramLearningOutcomeIds)
                {
                    if (!courseLearningOutcome.LearningOutcomeMappings.Any(cs => cs.PLOId == programLearningOutcomeId))
                        courseLearningOutcome.LearningOutcomeMappings.Add(
                            new Domain.Entities.LearningOutcomeMapping { PLOId = programLearningOutcomeId }
                        );
                }
            }

            await _unitOfWork.CourseLearningOutcomes.UpdateAsync(courseLearningOutcome, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            var response = new CourseLearningOutcomeResponse()
            {
                Id = courseLearningOutcome.Id,
                Name = courseLearningOutcome.Name,
                Description = courseLearningOutcome.Description,
                CourseId = courseLearningOutcome.CourseId,
            };

            return response;
        }
    }
}
