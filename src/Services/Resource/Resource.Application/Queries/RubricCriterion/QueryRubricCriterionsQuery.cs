using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.RubricCriterion
{
    public class QueryRubricCriterionsQuery : IRequest<PagedRubricCriterionList>
    {
        public string Search { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string OrderBy { get; set; }
        public int? AssignmentQuestionId { get; set; }
    }
}
