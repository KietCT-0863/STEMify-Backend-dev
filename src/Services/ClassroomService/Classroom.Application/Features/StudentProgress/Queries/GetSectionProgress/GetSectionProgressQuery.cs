using Classroom.Application.Models.ProgressModels;
using Infrastructure.Abstractions.Paging;
using MediatR;

namespace Classroom.Application.Features.StudentProgress.Queries.GetSectionProgress
{
    public class GetSectionProgressQuery : IRequest<PageList<StudentSectionProgressModel>>
    {
        public int EnrollmentId { get; set; }
        public int LessonId { get; set; }

        public GetSectionProgressQuery(int enrollmentId, int lessonId)
        {
            EnrollmentId = enrollmentId;
            LessonId = lessonId;
        }
    }
}
