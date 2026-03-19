using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Content
{
    public class QueryContentsQuery : IRequest<PagedContentList>
    {
        public string Search { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string OrderBy { get; set; }
        public int? SectionId { get; set; }
        public Resource.Domain.Enums.ContentStatus? Status { get; set; }
        public Resource.Domain.Enums.ContentType? ContentType { get; set; }
    }
}
