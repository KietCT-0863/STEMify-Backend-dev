using Classroom.Application.Models.EnrollmentModels;
using Classroom.Domain.Enums;
using MediatR;

namespace Classroom.Application.Features.CourseEnrollments.Commands.UpdateCourseEnrollment
{
    public class UpdateCourseEnrollmentCommand : IRequest<CourseEnrollmentModel>
    {
        public int Id { get; set; }
        public EnrollmentStatus? Status { get; set; }
        public int? ProgressPercentage { get; set; }
    }
}
