using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Curriculum
{
    public class QueryCurriculumsQuery : IRequest<PagedCurriculumList>
    {
        public string Search { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string OrderBy { get; set; }
        public string Code { get; set; }
        public Shared.Enums.SortDirection? SortDirection { get; set; }
        public string? CreatedByUserId { get; set; }
        public Resource.Domain.Enums.CurriculumStatus? Status { get; set; }
    }
}
