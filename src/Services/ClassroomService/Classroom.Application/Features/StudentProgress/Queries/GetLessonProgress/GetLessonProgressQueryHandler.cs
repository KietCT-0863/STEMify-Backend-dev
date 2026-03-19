using Classroom.Application.Common.Interfaces;
using Classroom.Application.Models.ProgressModels;
using Infrastructure.Abstractions.Paging;
using MediatR;

namespace Classroom.Application.Features.StudentProgress.Queries.GetLessonProgress
{
    public class GetLessonProgressQueryHandler
        : IRequestHandler<GetLessonProgressQuery, PageList<StudentLessonProgressModel>>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;

        public GetLessonProgressQueryHandler(IClassroomUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<PageList<StudentLessonProgressModel>> Handle(
            GetLessonProgressQuery request,
            CancellationToken cancellationToken
        )
        {
            var lessonProgress = await _unitOfWork.LessonProgress.FindAsync(
                predicate: lp => lp.EnrollmentId == request.EnrollmentId,
                cancellationToken: cancellationToken
            );
            return new PageList<StudentLessonProgressModel>(
                Items: lessonProgress
                    .Select(lp => new StudentLessonProgressModel
                    {
                        Id = lp.Id,
                        LessonId = lp.LessonId,
                        Status = lp.Status.ToString(),
                        CompletedAt = lp.CompletedAt,
                    })
                    .ToList(),
                TotalCount: lessonProgress.Count,
                PageNumber: 1, // Assuming single page for simplicity
                PageSize: lessonProgress.Count
            );
        }
    }
}
