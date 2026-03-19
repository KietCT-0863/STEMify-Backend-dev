using MediatR;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.StudentQuiz.Commands.CreateStudentQuizAttempt
{
    public class CreateStudentQuizAttemptCommand : IRequest<GrpcQuizAttemptResponse>
    {
        public int StudentQuizId { get; set; }
    }
}
