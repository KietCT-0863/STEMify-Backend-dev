using MediatR;
using Resource.Application.Commands.Lesson;
using Resource.Application.Common.Interfaces;
using Resource.Application.Specifications.Courses;
using Shared.Exceptions;

namespace Resource.Application.Handlers.Lesson
{
    public class UpdateLessonsOrderCommandHandler : IRequestHandler<UpdateLessonsOrderCommand, Unit>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public UpdateLessonsOrderCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(
            UpdateLessonsOrderCommand request,
            CancellationToken cancellationToken
        )
        {
            var spec = new CourseByIdSpecification(request.CourseId);
            var course =
                await _unitOfWork.Courses.FirstOrDefaultAsync(spec, cancellationToken)
                ?? throw new NotFoundException($"Course with {request.CourseId} not found");

            var lessonIds = course.Lessons.Select(s => s.Id).ToHashSet();
            if (!lessonIds.SetEquals(request.OrderedLessonIds))
                throw new DomainException("ordered ids must match lessons of the course");

            for (int i = 0; i < request.OrderedLessonIds.Count; i++)
            {
                var id = request.OrderedLessonIds[i];
                var s = await _unitOfWork.Lessons.FindByIdForUpdateAsync(id, cancellationToken);
                if (s != null && s.OrderIndex != i)
                {
                    s.OrderIndex = i;
                    s.LastModifiedDate = DateTime.UtcNow;
                    // ?? PERFORMANCE: Explicitly call UpdateAsync to attach entity
                    // Required because global NoTracking is enabled
                    await _unitOfWork.Lessons.UpdateAsync(s, cancellationToken);
                }

                course.LastModifiedDate = DateTime.UtcNow;
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
