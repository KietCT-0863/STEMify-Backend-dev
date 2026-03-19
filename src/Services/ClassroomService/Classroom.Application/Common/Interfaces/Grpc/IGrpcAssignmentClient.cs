using Shared.Protos.Resource;

namespace Classroom.Application.Common.Interfaces.Grpc
{
    public interface IGrpcAssignmentClient
    {
        Task<GrpcAssignmentModel?> GetAssignmentByIdAsync(int id);
    }
}
