using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Standard
{
    public class QueryStandardsQuery : IRequest<PagedStandardList>
    {
        public string Search { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string OrderBy { get; set; }
    }
}
