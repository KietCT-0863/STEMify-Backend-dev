using Contracts.Domains;

namespace Classroom.Domain.Entities
{
    public class QuizQuestionAttempt : EntityBase<int>
    {
        public int QuizAttemptId { get; set; }
        public int QuestionId { get; set; }
        public decimal? Score { get; set; }
        public bool IsCorrect { get; set; }
        // Navigation properties
        public virtual QuizAttempt QuizAttempt { get; set; } = null!;
        public virtual ICollection<AnswerAttempt> AnswerAttempts { get; set; } = [];
    }
}
