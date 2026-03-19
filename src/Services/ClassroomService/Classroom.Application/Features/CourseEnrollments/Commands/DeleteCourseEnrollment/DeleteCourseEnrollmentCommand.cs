using MediatR;

namespace Classroom.Application.Features.CourseEnrollments.Commands.DeleteCourseEnrollment
{
    public class DeleteCourseEnrollmentCommand : IRequest<bool>
    {
        public int EnrollmentId { get; set; }

        public DeleteCourseEnrollmentCommand(int enrollmentId)
        {
            EnrollmentId = enrollmentId;
        }
    }
}
