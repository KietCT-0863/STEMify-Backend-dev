using MediatR;
using Resource.Application.Commands.CourseLearningOutcome;
using Resource.Application.Common.Interfaces;

namespace Resource.Application.Handlers.CourseLearningOutcome
{
    public class DeleteCourseLearningOutcomeCommandHandler : IRequestHandler<DeleteCourseLearningOutcomeCommand>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public DeleteCourseLearningOutcomeCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteCourseLearningOutcomeCommand request, CancellationToken cancellationToken)
        {
            var courseLearningOutcome = await _unitOfWork.CourseLearningOutcomes.FindByIdForUpdateAsync(
                request.Id,
                cancellationToken
            );
            if (courseLearningOutcome == null)
                throw new KeyNotFoundException($"CourseLearningOutcome with ID {request.Id} not found.");

            await _unitOfWork.CourseLearningOutcomes.DeleteAsync(courseLearningOutcome, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
