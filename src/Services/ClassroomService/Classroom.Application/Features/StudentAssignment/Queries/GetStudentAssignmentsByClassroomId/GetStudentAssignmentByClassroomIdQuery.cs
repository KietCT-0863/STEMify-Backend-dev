using Classroom.Domain.Enums;
using Infrastructure.Common.Paging;
using MediatR;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.StudentAssignment.Queries.GetStudentAssignmentsByClassroomId
{
    public class GetStudentAssignmentByClassroomIdQuery : IRequest<GrpcPagedStudentAssignmentsResponse>
    {
        public int ClassroomId { get; set; }
        public StudentAssignmentStatus? Status { get; set; }
        public PageRequest PageRequest { get; set; } = new();
    }
}
