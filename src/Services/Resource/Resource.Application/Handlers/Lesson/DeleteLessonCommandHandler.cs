using MediatR;
using Resource.Application.Commands.Lesson;
using Resource.Application.Common.Interfaces;

namespace Resource.Application.Handlers.Lesson
{
    public class DeleteLessonCommandHandler : IRequestHandler<DeleteLessonCommand>
    {
        private readonly IResourceUnitOfWork _unitOfWork;
        private readonly IMediator _mediator;

        public DeleteLessonCommandHandler(IResourceUnitOfWork unitOfWork, IMediator mediator)
        {
            _unitOfWork = unitOfWork;
            _mediator = mediator;
        }

        public async Task Handle(DeleteLessonCommand request, CancellationToken cancellationToken)
        {
            var lesson = await _unitOfWork.Lessons.FindByIdForUpdateAsync(request.Id, cancellationToken);
            if (lesson == null)
                throw new KeyNotFoundException($"Lesson with ID {request.Id} not found.");

            // Soft delete: mark as Deleted
            lesson.Status = Domain.Enums.LessonStatus.Deleted;
            lesson.LastModifiedDate = DateTimeOffset.UtcNow;
            
            await _unitOfWork.Lessons.UpdateAsync(lesson, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
