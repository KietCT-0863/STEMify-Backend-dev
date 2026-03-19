using MediatR;
using Resource.Application.Commands.Course;
using Resource.Application.Common.Interfaces;

namespace Resource.Application.Handlers.Course
{
    public class DeleteCourseCommandHandler : IRequestHandler<DeleteCourseCommand>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public DeleteCourseCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task Handle(DeleteCourseCommand request, CancellationToken cancellationToken)
        {

                var course = await _unitOfWork.Courses.FindByIdAsync(request.Id, cancellationToken);
                if (course == null)
                    throw new KeyNotFoundException($"Course with ID {request.Id} not found.");

                course.Status = Domain.Enums.CourseStatus.Deleted;

                await _unitOfWork.Courses.UpdateAsync(course, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
