using Contracts.Domains;

namespace Classroom.Domain.Entities
{
    public class AnswerAttempt : EntityBase<int>
    {
        public int QuestionAttemptId { get; set; }
        public int AnswerId { get; set; }
        public bool IsCorrect { get; set; }
        public bool IsSelected { get; set; }

        // Navigation properties
        public virtual QuizQuestionAttempt QuestionAttempt { get; set; } = null!;
    }
}
