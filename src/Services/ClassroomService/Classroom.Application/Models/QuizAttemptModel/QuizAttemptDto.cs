using Classroom.Domain.Enums;

namespace Classroom.Application.Models.QuizAttemptModel
{
    public class StudentQuiz
    {
        public int QuizId { get; set; }
        public int StudentSectionProgressId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public StudentQuizStatus Status { get; set; }
        public decimal? FinalScore { get; set; }
        public DateTime AssignedAt { get; set; }
        public int AttemptCount { get; set; }
    }

    public class QuizAttemptDto
    {
        public int Id { get; set; }
        public int QuizId { get; set; }
        public int StudentQuizId { get; set; }
        public QuizAttemptStatus Status { get; set; }
        public decimal TotalScore { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public int AttemptNumber { get; set; }
    }

    public class QuizDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public decimal TotalMarks { get; set; }
        public decimal PassingMarks { get; set; }
    }
}
