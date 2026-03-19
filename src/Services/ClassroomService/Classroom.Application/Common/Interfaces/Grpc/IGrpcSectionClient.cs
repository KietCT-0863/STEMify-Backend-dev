using Shared.Protos.Resource;

namespace Classroom.Application.Common.Interfaces.Grpc
{
    public interface IGrpcSectionClient
    {
        Task<PagedSectionList> GetSectionsAsync(QuerySectionsRequest request);
        Task<SectionResponse> GetSectionByIdAsync(int id);
    }
}
