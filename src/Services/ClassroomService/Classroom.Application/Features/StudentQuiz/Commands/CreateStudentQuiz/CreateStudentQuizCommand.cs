using MediatR;

namespace Classroom.Application.Features.StudentQuiz.Commands.CreateStudentQuiz
{
    public class CreateStudentQuizCommand : IRequest<Unit>
    {
        public int QuizId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public int StudentSectionProgressId { get; set; }
        public DateTime? DueDate { get; set; }
        public int? MaxAttemptAllowed { get; set; }
        public int? TimeLimitMinutes { get; set; }
    }
}
