using Classroom.Domain.Enums;
using Infrastructure.Common.Paging;
using MediatR;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.StudentQuiz.Queries.GetStudentQuizList
{
    public class GetStudentQuizByClassroomQuery : IRequest<GrpcPagedStudentQuizzesResponse>
    {
        public int ClassroomId { get; set; }
        public StudentQuizStatus? Status { get; set; }
        public PageRequest PageRequest { get; set; } = new();
    }
}
