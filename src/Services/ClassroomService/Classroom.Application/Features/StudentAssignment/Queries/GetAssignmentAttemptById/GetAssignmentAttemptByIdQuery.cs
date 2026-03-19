using MediatR;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.StudentAssignment.Queries.GetAssignmentAttemptById
{
    public class GetAssignmentAttemptByIdQuery : IRequest<GrpcAssignmentAttemptResponse>
    {
        public int Id { get; set; }
    }
}
