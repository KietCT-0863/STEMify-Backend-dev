using Classroom.Domain.Enums;
using Contracts.Domains;

namespace Classroom.Domain.Entities
{
    public class AssignmentAttempt : EntityBase<int>
    {
        public int StudentAssignmentId { get; set; }
        public string TeacherId { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public decimal TotalScore { get; set; }
        public string? Feedback { get; set; }
        public int AttemptNumber { get; set; }
        public AssignmentAttemptStatus Status { get; set; }

        // Navigation properties
        public virtual StudentAssignment? StudentAssignment { get; set; }
        public virtual ICollection<AssignmentQuestionAttempt> AssignmentQuestionAttempts { get; set; } = [];
    }
}
