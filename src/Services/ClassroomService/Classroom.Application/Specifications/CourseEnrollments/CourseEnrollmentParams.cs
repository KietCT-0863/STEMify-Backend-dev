using Classroom.Domain.Enums;
using Shared.SeedWork;

namespace Classroom.Application.Specifications.CourseEnrollments
{
    public class CourseEnrollmentParams : PagingRequestParam
    {
        private string? _search;

        public string? Search
        {
            get => _search;
            set => _search = value?.ToLower().Trim();
        }
        public Guid? StudentId { get; set; }
        public int? CourseId { get; set; }
        public int? ClassroomId { get; set; }
        public EnrollmentStatus? Status { get; set; }
        public string? VerificationCode { get; set; }
    }
}
