using Shared.Protos.Classroom;

namespace Order.Application.Common.Interfaces.Grpc
{
    public interface IGrpcClassroomClient
    {
        Task<GrpcPagedClassroomsResponse> GetPagedClassrooms(GetClassroomsRequest request);
        Task<GrpcClassroomResponse> GetClassroomById(GetClassroomRequest request);
    }
}
