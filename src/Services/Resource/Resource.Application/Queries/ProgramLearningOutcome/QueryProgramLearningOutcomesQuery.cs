using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.ProgramLearningOutcome
{
    public class QueryProgramLearningOutcomesQuery : IRequest<PagedProgramLearningOutcomeList>
    {
        public string Search { get; set; }
        public int? CurriculumId { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string OrderBy { get; set; }
    }
}
