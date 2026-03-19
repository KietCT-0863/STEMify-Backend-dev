using Classroom.Application.Models.EnrollmentModels;
using Classroom.Domain.Enums;
using MediatR;

namespace Classroom.Application.Features.CurriculumEnrollments.Commands.CreateCurriculumEnrollment
{
    public class CreateCurriculumEnrollmentCommand : IRequest<CurriculumEnrollmentModel>
    {
        public Guid StudentId { get; set; }
        public int CurriculumId { get; set; }
        public EnrollmentStatus Status { get; set; }
        public int? ClassroomId { get; set; }
    }
}
