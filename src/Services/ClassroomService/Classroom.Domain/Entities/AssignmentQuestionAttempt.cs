using Contracts.Domains;

namespace Classroom.Domain.Entities
{
    public class AssignmentQuestionAttempt : EntityBase<int>
    {
        public int AssignmentAttemptId { get; set; }
        public int AssignmentQuestionId { get; set; }
        public string? AnswerText { get; set; }
        public string? AnswerFileUrl { get; set; }
        public decimal Points { get; set; }

        // Navigation properties
        public virtual AssignmentAttempt? AssignmentAttempt { get; set; }
        public virtual ICollection<RubricScore> RubricScores { get; set; } = new List<RubricScore>();
    }
}
