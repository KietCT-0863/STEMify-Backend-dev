using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.AgeRange
{
    public class QueryAgeRangesQuery : IRequest<PagedAgeRangeList>
    {
        public string Search { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string OrderBy { get; set; }
        public int? Age { get; set; }
    }
}
