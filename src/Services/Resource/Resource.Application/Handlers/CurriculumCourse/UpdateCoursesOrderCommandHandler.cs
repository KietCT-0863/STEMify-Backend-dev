using MediatR;
using Resource.Application.Commands.Course;
using Resource.Application.Common.Interfaces;
using Resource.Application.Specifications.Curriculums;
using Shared.Exceptions;

namespace Resource.Application.Handlers.CurriculumCourse
{
    public class UpdateCoursesOrderCommandHandler : IRequestHandler<UpdateCoursesOrderCommand, Unit>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public UpdateCoursesOrderCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<Unit> Handle(
            UpdateCoursesOrderCommand request,
            CancellationToken cancellationToken
        )
        {
            var spec = new CurriculumByIdSpecification(request.CurriculumId);
            var curriculum =
                await _unitOfWork.Curriculums.FirstOrDefaultAsync(spec, cancellationToken)
                ?? throw new NotFoundException($"Curriculum with {request.CurriculumId} not found");

            var courseIds = curriculum.CurriculumCourses.Select(s => s.CourseId).ToHashSet();
            if (!courseIds.SetEquals(request.OrderedCourseIds))
                throw new DomainException("ordered ids must match courses of the curriculum");

            for (int i = 0; i < request.OrderedCourseIds.Count; i++)
            {
                var id = request.OrderedCourseIds[i];
                var s = await _unitOfWork.CurriculumCourses.
                    FindOneAsync(c => c.CourseId == id && c.CurriculumId == request.CurriculumId, cancellationToken);
                if (s != null && s.CourseOrderIndex != i)
                {
                    s.CourseOrderIndex = i;
                }
                curriculum.LastModifiedDate = DateTime.UtcNow.AddHours(7);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return Unit.Value;
        }
    }
}
