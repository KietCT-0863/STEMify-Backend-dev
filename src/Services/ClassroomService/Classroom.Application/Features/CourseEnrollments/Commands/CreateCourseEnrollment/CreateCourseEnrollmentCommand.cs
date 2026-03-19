using Classroom.Application.Models.EnrollmentModels;
using Classroom.Domain.Enums;
using MediatR;

namespace Classroom.Application.Features.CourseEnrollments.Commands.CreateCourseEnrollment
{
    public class CreateCourseEnrollmentCommand : IRequest<CourseEnrollmentModel>
    {
        public Guid StudentId { get; set; }
        public int? CurriculumEnrollmentId { get; set; }
        public int CourseId { get; set; }
        public int? ClassroomId { get; set; }
        public EnrollmentStatus Status { get; set; }
    }
}
