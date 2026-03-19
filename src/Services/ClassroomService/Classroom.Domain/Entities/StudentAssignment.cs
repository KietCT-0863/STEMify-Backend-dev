using Classroom.Domain.Enums;
using Contracts.Domains;

namespace Classroom.Domain.Entities
{
    public class StudentAssignment : EntityBase<int>
    {
        public string StudentId { get; set; }
        public int StudentSectionProgressId { get; set; }
        public int AssignmentId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public decimal? FinalScore { get; set; }
        public DateTime? DueDate { get; set; }
        public int AttemptCount { get; set; }
        public int? MaxAttemptAllowed { get; set; }
        public DateTime? NextAttemptAvailableAt { get; set; }
        public StudentAssignmentStatus Status { get; set; } = StudentAssignmentStatus.Assigned;

        /// Navigation properties
        public virtual StudentSectionProgress StudentSectionProgress { get; set; } = null!;
        public virtual ICollection<AssignmentAttempt> AssignmentAttempts { get; set; } = [];
    }
}
