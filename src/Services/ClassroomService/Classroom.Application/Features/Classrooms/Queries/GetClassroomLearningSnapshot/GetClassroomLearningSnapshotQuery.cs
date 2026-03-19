using MediatR;
using Shared.Protos.Classroom;

namespace Classroom.Application.Features.Classrooms.Queries.GetClassroomLearningSnapshot
{
    public class GetClassroomLearningSnapshotQuery : IRequest<GrpcClassroomLearningSnapshotResponse>
    {
        public int ClassroomId { get; set; }
        public string? StudentId { get; set; }
        public int? DaysBack { get; set; }
    }
}

