using Classroom.Domain.Enums;
using Contracts.Domains;

namespace Classroom.Domain.Entities
{
    public class QuizAttempt : EntityBase<int>
    {
        public int StudentQuizId { get; set; }
        public QuizAttemptStatus Status { get; set; }
        public decimal TotalScore { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int AttemptNumber { get; set; }

        // Navigation properties
        public virtual StudentQuiz StudentQuiz { get; set; } = null!;
        public virtual ICollection<QuizQuestionAttempt> QuestionAttempts { get; set; } = [];
    }
}
