using Classroom.Domain.Enums;
using Contracts.Domains;
using System.ComponentModel.DataAnnotations;

namespace Classroom.Domain.Entities
{
    public class Certificate : EntityBase<int>
    {
        [Required]
        public Guid UserId { get; set; }

        public int? CourseEnrollmentId { get; set; }

        public int? CurriculumEnrollmentId { get; set; }

        [Required]
        public CertificateType CertificateType { get; set; }

        [Required]
        public DateTime IssueDate { get; set; } = DateTime.UtcNow;

        [Required]
        public string VerificationCode { get; set; } = string.Empty;

        [Required]
        public string CertificateUrl { get; set; } = string.Empty;

        [Required]
        public string UserName { get; set; } = string.Empty;

        [Required]
        public string Title { get; set; } = string.Empty;

        // Navigation
        public virtual CourseEnrollment? CourseEnrollment { get; set; }
        public virtual CurriculumEnrollment? CurriculumEnrollment { get; set; }
    }
}
