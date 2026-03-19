using MediatR;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.StudentQuiz.Commands.UpdateStudentQuizAttempt
{
    public class UpdateStudentQuizAttemptCommand : IRequest<GrpcQuizAttemptResponse>
    {
        public int Id { get; set; }
        public List<QuestionAttemptCommand> QuestionAttempts { get; set; } = [];

    }
    public class QuestionAttemptCommand
    {
        public int QuestionId { get; set; }
        public List<int> AnswerIds { get; set; } = [];

    }
}
