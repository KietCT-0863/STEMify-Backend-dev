using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.CourseLearningOutcome
{
    public class QueryCourseLearningOutcomesQuery : IRequest<PagedCourseLearningOutcomeList>
    {
        public string Search { get; set; }
        public int? CourseId { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string OrderBy { get; set; }
    }
}
