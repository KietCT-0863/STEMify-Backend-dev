using Classroom.Domain.Enums;
using MediatR;

namespace Classroom.Application.Features.StudentProgress.Commands.CreateLessonProgress
{
    public class CreateLessonProgressCommand : IRequest<Unit>
    {
        public int LessonId { get; set; }
        public int CourseEnrollmentId { get; set; }
        public ProgressStatus Status { get; set; }
    }
}
