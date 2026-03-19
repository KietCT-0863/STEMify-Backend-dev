using Infrastructure.Common.Paging;
using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.Answer;
using Resource.Application.Specifications.Answers;
using Shared.Protos.Resource;
using System.Linq.Expressions;

namespace Resource.Application.Handlers.Answer
{
    public class QueryAnswersQueryHandler : IRequestHandler<QueryAnswersQuery, PagedAnswerList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public QueryAnswersQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedAnswerList> Handle(
            QueryAnswersQuery request,
            CancellationToken cancellationToken
        )
        {
            try
            {
                var filter = new AnswerParams
                {
                    Search = request.Search,
                    PageNumber = request.PageNumber < 1 ? 1 : request.PageNumber,
                    PageSize = request.PageSize < 1 ? 10 : request.PageSize,
                    OrderBy = request.OrderBy,
                    QuestionId = request.QuestionId,
                };

                var pageRequest = filter.ToPageRequest();

                Expression<Func<Domain.Entities.Answer, bool>> predicate = c =>
                    (
                        string.IsNullOrEmpty(filter.Search)
                        || c.Content.ToLower().Contains(filter.Search)
                    ) && (!filter.QuestionId.HasValue || c.QuestionId == filter.QuestionId.Value);

                Expression<Func<Domain.Entities.Answer, object>>? sortExpression =
                    request.OrderBy?.ToLower() switch
                    {
                        "content" => c => c.Content,
                        _ => c => c.Id,
                    };

                var paged = await _unitOfWork.Answers.GetByPageFilter(
                    pageRequest,
                    sortExpression: sortExpression,
                    predicate: predicate,
                    cancellationToken: cancellationToken
                );

                var response = new PagedAnswerList
                {
                    TotalCount = paged.TotalCount,
                    PageNumber = paged.PageNumber,
                    PageSize = paged.PageSize,
                    TotalPages = paged.TotalPages,
                };

                foreach (var answer in paged.Items)
                {
                    var answerResponse = new AnswerResponse
                    {
                        Id = answer.Id,
                        Content = answer.Content,
                        IsCorrect = answer.IsCorrect,
                        QuestionId = answer.QuestionId,
                    };

                    response.Items.Add(answerResponse);
                }

                return response;
            }
            catch (Exception ex)
            {
                throw new ApplicationException(
                    $"An error occurred while retrieving the answer list: {ex.Message}",
                    ex
                );
            }
        }
    }
}
