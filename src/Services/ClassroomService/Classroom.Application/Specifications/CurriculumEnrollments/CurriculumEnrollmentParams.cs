using Classroom.Domain.Enums;
using Shared.SeedWork;

namespace Classroom.Application.Specifications.CurriculumEnrollments
{
    public class CurriculumEnrollmentParams : PagingRequestParam
    {
        private string? _search;

        public string? Search
        {
            get => _search;
            set => _search = value?.ToLower().Trim();
        }
        public Guid? StudentId { get; set; }
        public int? CurriculumId { get; set; }
        public int? CertificateId { get; set; }
        public string? VerificationCode { get; set; }
        public EnrollmentStatus? Status { get; set; }
        public int? OrganizationSubscriptionOrderId { get; set; }
    }
}
