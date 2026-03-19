using MediatR;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.StudentQuiz.Queries.GetStudentQuizById
{
    public class GetStudentQuizByIdQuery : IRequest<GrpcStudentQuizResponse>
    {
        public int Id { get; set; }
    }
}
