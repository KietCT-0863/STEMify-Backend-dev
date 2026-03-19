using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Tags
{
    public class GetTagListQuery : IRequest<PagedTagList>
    {
        public string? Search { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
    }
}
