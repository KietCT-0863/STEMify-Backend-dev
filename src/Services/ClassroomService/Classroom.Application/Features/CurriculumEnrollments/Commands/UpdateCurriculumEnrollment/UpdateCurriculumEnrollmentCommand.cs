using Classroom.Application.Models.EnrollmentModels;
using Classroom.Domain.Enums;
using MediatR;

namespace Classroom.Application.Features.CurriculumEnrollments.Commands.UpdateCurriculumEnrollment
{
    public class UpdateCurriculumEnrollmentCommand : IRequest<CurriculumEnrollmentModel>
    {
        public int Id { get; set; }
        public int CurriculumId { get; set; }
        public int? ProgressPercentage { get; set; }
        public EnrollmentStatus? Status { get; set; }
    }
}
