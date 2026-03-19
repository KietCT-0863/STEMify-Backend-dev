using MediatR;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.StudentAssignment.Queries.GetStudentAssignmentById
{
    public class GetStudentAssignmentByIdQuery : IRequest<GrpcStudentAssignmentResponse>
    {
        public int Id { get; set; }
    }
}
