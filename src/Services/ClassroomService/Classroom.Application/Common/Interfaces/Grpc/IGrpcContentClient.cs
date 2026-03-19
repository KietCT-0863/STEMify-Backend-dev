using Shared.Protos.Resource;

namespace Classroom.Application.Common.Interfaces.Grpc
{
    public interface IGrpcContentClient
    {
        Task<ContentResponse?> GetContentBySectionIdAsync(int sectionId);
    }
}
