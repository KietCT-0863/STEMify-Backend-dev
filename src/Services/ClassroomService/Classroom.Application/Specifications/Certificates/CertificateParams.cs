using Classroom.Domain.Enums;
using Shared.SeedWork;

namespace Classroom.Application.Specifications.Certificates
{
    public class CertificateParams : PagingRequestParam
    {
        private string? _search;

        public string? Search
        {
            get => _search;
            set => _search = value?.ToLower().Trim();
        }
        public CertificateType? Type { get; set; }
        public string? UserId { get; set; }
        public int? CourseEnrollmentId { get; set; }
        public int? CurriculumEnrollmentId { get; set; }
    }
}
