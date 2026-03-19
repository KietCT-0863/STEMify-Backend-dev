using Classroom.Domain.Enums;
using Contracts.Domains;

namespace Classroom.Domain.Entities
{
    public class StudentSectionProgress : EntityBase<int>
    {
        public int StudentLessonProgressId { get; set; }
        public int SectionId { get; set; }
        public DateTime? CompletedAt { get; set; }
        public ProgressStatus Status { get; set; }

        /// Navigation properties
        public virtual StudentLessonProgress LessonProgress { get; set; } = null!;
        public StudentQuiz? StudentQuiz { get; set; }
        public StudentAssignment? StudentAssignment { get; set; }
    }
}
