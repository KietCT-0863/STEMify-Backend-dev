using Classroom.Domain.Enums;
using Contracts.Domains;

namespace Classroom.Domain.Entities
{
    public class StudentLessonProgress : EntityBase<int>
    {
        public int EnrollmentId { get; set; }
        public int LessonId { get; set; }
        public ProgressStatus Status { get; set; }
        public DateTime? CompletedAt { get; set; }

        /// Navigation properties
        public virtual ICollection<StudentSectionProgress> SectionProgress { get; set; } = [];
        public virtual CourseEnrollment CourseEnrollment { get; set; } = null!;
    }
}
