using MediatR;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.StudentAssignment.Queries.GetStudentAssignmentsByAssignmentId
{
    public class GetStudentAssignmentsByAssignmentIdQuery : IRequest<GrpcAssignmentStatisticResponse>
    {
        public int AssignmentId { get; set; }
        public int ClassroomId { get; set; }
    }
}
