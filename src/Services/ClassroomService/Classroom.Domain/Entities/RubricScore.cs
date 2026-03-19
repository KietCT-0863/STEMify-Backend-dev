using Contracts.Domains;

namespace Classroom.Domain.Entities
{
    public class RubricScore : EntityBase<int>
    {
        public int AssignmentQuestionAttemptId { get; set; }
        public int RubricCriterionId { get; set; }
        public decimal Points { get; set; }

        // Navigation properties
        public virtual AssignmentQuestionAttempt? AssignmentQuestionAttempt { get; set; }
    }
}
