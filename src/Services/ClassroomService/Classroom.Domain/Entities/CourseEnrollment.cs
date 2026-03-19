using Classroom.Domain.Enums;
using Contracts.Domains;

namespace Classroom.Domain.Entities
{
    public class CourseEnrollment : EntityBase<int>
    {
        public Guid StudentId { get; set; }
        public int CourseId { get; set; }
        public int? CurriculumEnrollmentId { get; set; }
        public int? ClassroomId { get; set; }

        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public EnrollmentStatus Status { get; set; }
        public int? FinalScore { get; set; }
        public int ProgressPercentage { get; set; } = 0;

        public virtual Certificate? Certificate { get; set; }
        public Classroom? Classroom { get; set; }
        public virtual CurriculumEnrollment? CurriculumEnrollment { get; set; } 
        public virtual ICollection<StudentLessonProgress> LessonProgress { get; set; } = [];
    }
}
