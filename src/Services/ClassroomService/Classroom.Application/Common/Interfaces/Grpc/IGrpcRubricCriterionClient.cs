using Shared.Protos.Resource;

namespace Classroom.Application.Common.Interfaces.Grpc
{
    public interface IGrpcRubricCriterionClient
    {
        Task<RubricCriterionResponse?> GetRubricCriterionByIdAsync(int id);
        Task<PagedRubricCriterionList?> GetQueryRubricCriterions(QueryRubricCriterionsRequest request);
    }
}
