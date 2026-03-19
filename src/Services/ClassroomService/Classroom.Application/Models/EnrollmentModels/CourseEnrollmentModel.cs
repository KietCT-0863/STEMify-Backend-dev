namespace Classroom.Application.Models.EnrollmentModels
{
    public class CourseEnrollmentModel
    {
        public int Id { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public string StudentName { get; set; } = string.Empty;
        public int CourseId { get; set; }
        public string CourseTitle { get; set; } = string.Empty;
        public string? CoverImageUrl { get; set; }
        public string? Description { get; set; }
        public int? Duration { get; set; }
        public string? AgeRangeLabel { get; set; }
        public DateTime? EnrolledAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string? Status { get; set; } 
        public int? FinalScore { get; set; }
        public string? VerificationCode { get; set; }
        public string? CertificateUrl { get; set; }
        public int? CertificateId { get; set; }
        public int ProgressPercentage { get; set; }
        public int? ClassroomId { get; set; }
    }
}
