namespace Classroom.Application.Models.EnrollmentModels
{
    public class CurriculumEnrollmentModel
    {
        public int Id { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public int CurriculumId { get; set; }
        public string CurriculumTitle { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
        public string? Description { get; set; }
        public DateTime EnrolledAt { get; set; }
        public int ProgressPercentage { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? VerificationCode { get; set; }
        public string? CertificateUrl { get; set; }
        public int? CertificateId { get; set; }
        public List<CourseEnrollmentModel> CourseEnrollments { get; set; } = new();
    }
}
