using MediatR;

namespace Classroom.Application.Features.CurriculumEnrollments.Commands.DeleteCurriculumEnrollment
{
    public class DeleteCurriculumEnrollmentCommand : IRequest<bool>
    {
        public int EnrollmentId { get; set; }

        public DeleteCurriculumEnrollmentCommand(int enrollmentId)
        {
            EnrollmentId = enrollmentId;
        }
    }
}
