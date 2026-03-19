using MediatR;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.Classrooms.Queries.GetClassroomStatistic
{
    public class GetClassroomStatisticQuery : IRequest<GrpcClassroomStatisticResponse>
    {
        public int ClassroomId { get; set; }
    }
}
