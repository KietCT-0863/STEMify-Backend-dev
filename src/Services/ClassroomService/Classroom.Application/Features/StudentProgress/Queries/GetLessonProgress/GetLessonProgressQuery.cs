using Classroom.Application.Models.ProgressModels;
using Infrastructure.Abstractions.Paging;
using MediatR;

namespace Classroom.Application.Features.StudentProgress.Queries.GetLessonProgress
{
    public class GetLessonProgressQuery : IRequest<PageList<StudentLessonProgressModel>>
    {
        public int EnrollmentId { get; set; }

        public GetLessonProgressQuery(int enrollmentId)
        {
            EnrollmentId = enrollmentId;
        }
    }
}
