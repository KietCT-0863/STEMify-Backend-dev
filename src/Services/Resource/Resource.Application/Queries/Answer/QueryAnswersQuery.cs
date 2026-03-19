using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Queries.Answer
{
    public class QueryAnswersQuery : IRequest<PagedAnswerList>
    {
        public string Search { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public string OrderBy { get; set; }
        public int? QuestionId { get; set; }
    }
}
