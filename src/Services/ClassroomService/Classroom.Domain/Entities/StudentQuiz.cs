using Classroom.Domain.Enums;
using Contracts.Domains;

namespace Classroom.Domain.Entities
{
    public class StudentQuiz : EntityBase<int>
    {
        public int QuizId { get; set; }
        public int StudentSectionProgressId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public StudentQuizStatus Status { get; set; }
        public decimal? FinalScore { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? DueDate { get; set; }
        public int? MaxAttemptAllowed { get; set; }
        public int? TimeLimitMinutes { get; set; }
        public int AttemptCount { get; set; }
        public DateTime? NextAttemptAvailableAt { get; set; }

        // Navigation properties
        public virtual ICollection<QuizAttempt> QuizAttempts { get; set; } = [];
    }
}
