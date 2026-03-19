using Classroom.Domain.Enums;
using Contracts.Domains;
using System.ComponentModel.DataAnnotations;

namespace Classroom.Domain.Entities
{
    public class CurriculumEnrollment : EntityBase<int>
    {
        [Required]
        public Guid StudentId { get; set; }

        [Required]
        public int CurriculumId { get; set; }

        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;

        [Required]
        public EnrollmentStatus Status { get; set; } = EnrollmentStatus.InProgress;

        public DateTime? CompletedAt { get; set; }

        public int ProgressPercentage { get; set; } = 0;

        // Navigation
        public Certificate? Certificate { get; set; }
        public virtual ICollection<CourseEnrollment> CourseEnrollments { get; set; } = [];
    }
}
