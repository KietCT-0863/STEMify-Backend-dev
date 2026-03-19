using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Section
{
    public class QuerySectionsQuery : IRequest<PagedSectionList>
    {
        public string Search { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string OrderBy { get; set; }
        public int? LessonId { get; set; }
        public Resource.Domain.Enums.SectionStatus? Status { get; set; }
    }
}
