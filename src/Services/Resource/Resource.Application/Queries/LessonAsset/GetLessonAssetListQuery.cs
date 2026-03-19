using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.LessonAsset
{
    public class GetLessonAssetListQuery : IRequest<PagedLessonAssetList>
    {
        public string? Search { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int LessonId { get; set; }
        public int? TagId { get; set; }
        public string? Type { get; set; }
    }

}
