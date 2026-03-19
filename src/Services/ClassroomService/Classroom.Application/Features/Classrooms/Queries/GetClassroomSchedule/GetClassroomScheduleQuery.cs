using MediatR;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.Classrooms.Queries.GetClassroomSchedule
{
    public class GetClassroomScheduleQuery : IRequest<GrpcClassroomScheduleResponse>
    {
        public int ClassroomId { get; set; }
    }
}
