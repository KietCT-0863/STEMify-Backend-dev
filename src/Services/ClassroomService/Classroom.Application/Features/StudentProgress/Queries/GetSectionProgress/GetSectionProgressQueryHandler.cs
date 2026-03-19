using Classroom.Application.Common.Interfaces;
using Classroom.Application.Models.ProgressModels;
using Infrastructure.Abstractions.Paging;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Classroom.Application.Features.StudentProgress.Queries.GetSectionProgress
{
    public class GetSectionProgressQueryHandler
        : IRequestHandler<GetSectionProgressQuery, PageList<StudentSectionProgressModel>>
    {
        private readonly IClassroomUnitOfWork _unitOfWork;

        public GetSectionProgressQueryHandler(IClassroomUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        public async Task<PageList<StudentSectionProgressModel>> Handle(
            GetSectionProgressQuery request,
            CancellationToken cancellationToken
        )
        {
            var pagedSectionProgress = await _unitOfWork.SectionProgress.GetByPageFilter(
                pageRequest: new Infrastructure.Common.Paging.PageRequest
                {
                    PageNumber = 1,
                    PageSize = int.MaxValue // Fetch all for simplicity
                },
                sortExpression: sp => sp.Id,
                projectionFunc: sp => sp
                    .Include(sp => sp.StudentQuiz)
                    .Include(sp => sp.StudentAssignment)
                    .Select(sp => new StudentSectionProgressModel
                    {
                        Id = sp.Id,
                        SectionId = sp.SectionId,
                        Status = sp.Status.ToString(),
                        CompletedAt = sp.CompletedAt,
                        StudentQuizId = sp.StudentQuiz != null ? sp.StudentQuiz.Id : null,
                        StudentAssignmentId =
                            sp.StudentAssignment != null ? sp.StudentAssignment.Id : null
                    }),
                predicate: sp =>
                    sp.LessonProgress.EnrollmentId == request.EnrollmentId &&
                    sp.LessonProgress.LessonId == request.LessonId,
                cancellationToken: cancellationToken
            );

            return new PageList<StudentSectionProgressModel>(
                pagedSectionProgress.Items,
                pagedSectionProgress.PageNumber,
                pagedSectionProgress.PageSize,
                pagedSectionProgress.TotalCount
            );
        }
    }
}
