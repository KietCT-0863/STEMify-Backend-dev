using MediatR;
using Resource.Application.Commands.CurriculumCourse;
using Resource.Application.Common.Interfaces;
using Shared.Exceptions;

namespace Resource.Application.Handlers.CurriculumCourse
{
    public class CreateCurriculumCourseCommandHandler : IRequestHandler<CreateCurriculumCourseCommand>
    {
        private readonly IResourceUnitOfWork _unitOfWork;
        public CreateCurriculumCourseCommandHandler(
            IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task Handle(CreateCurriculumCourseCommand request, CancellationToken cancellationToken)
        {
            int orderIndex = 0;
            var curriculumCourses = new List<Domain.Entities.CurriculumCourse>();
            if ((await _unitOfWork.Curriculums.AnyAsync(c => c.Id == request.CurriculumId, cancellationToken)) == false)
            {
                throw new NotFoundException($"Curriculum with ID '{request.CurriculumId}' does not exist.");
            }
            // calculate the number of courses in this curriculum
            orderIndex = (await _unitOfWork.CurriculumCourses.FindAsync(c => c.CurriculumId == request.CurriculumId, cancellationToken)).Count;

            foreach (var courseId in request.CourseIds)
            {
                if ((await _unitOfWork.Courses.AnyAsync(c => c.Id == courseId, cancellationToken)) == false)
                {
                    throw new NotFoundException($"Course with ID '{courseId}' does not exist.");
                }
                if ((await _unitOfWork.CurriculumCourses.AnyAsync(cc => cc.CurriculumId == request.CurriculumId && cc.CourseId == courseId, cancellationToken)))
                {
                    throw new InvalidOperationException($"Course with ID '{courseId}' is already associated with Curriculum ID '{request.CurriculumId}'.");
                }

                curriculumCourses.Add(new Domain.Entities.CurriculumCourse
                {
                    CurriculumId = request.CurriculumId,
                    CourseId = courseId,
                    CourseOrderIndex = orderIndex,
                });
                orderIndex++;
            }
            await _unitOfWork.CurriculumCourses.AddRangeAsync(curriculumCourses, cancellationToken);

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
