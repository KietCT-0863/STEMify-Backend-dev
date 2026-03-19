using MediatR;
using Resource.Application.Commands.CurriculumCourse;
using Resource.Application.Common.Interfaces;
using Resource.Application.Specifications.Curriculums;
using Shared.Exceptions;

namespace Resource.Application.Handlers.CurriculumCourse
{
    public class DeleteCurriculumCourseCommandHandler : IRequestHandler<DeleteCurriculumCourseCommand>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public DeleteCurriculumCourseCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }
        public async Task Handle(DeleteCurriculumCourseCommand request, CancellationToken cancellationToken)
        {

            var spec = new CurriculumByIdSpecification(request.CurriculumId);
            var curriculum =
                await _unitOfWork.Curriculums.FirstOrDefaultAsync(spec, cancellationToken)
                ?? throw new NotFoundException($"Curriculum with {request.CurriculumId} not found");

            var curriculumCourses = new List<Domain.Entities.CurriculumCourse>();
            foreach (var courseId in request.CourseIds)
            {
                var curriculumCourse = await _unitOfWork.CurriculumCourses.FindOneAsync
                    (cc => cc.CurriculumId == request.CurriculumId && cc.CourseId == courseId, cancellationToken);
                if (curriculumCourse == null)
                {
                    throw new InvalidOperationException(
                        $"Course with ID '{courseId}' is not associated with Curriculum ID '{request.CurriculumId}'.");
                }
                curriculumCourses.Add(curriculumCourse);
            }

            await _unitOfWork.CurriculumCourses.DeleteRangeAsync(curriculumCourses, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
    }
}
