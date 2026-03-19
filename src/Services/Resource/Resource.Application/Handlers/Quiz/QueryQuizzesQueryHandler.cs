using Infrastructure.Common.Paging;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Resource.Application.Common.Interfaces;
using Resource.Application.Extensions.Mapping;
using Resource.Application.Queries.Quiz;
using Shared.Protos.Resource;
using System.Linq.Expressions;

namespace Resource.Application.Handlers.Quiz
{
    public class QueryQuizzesQueryHandler : IRequestHandler<QueryQuizzesQuery, PagedQuizList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public QueryQuizzesQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<PagedQuizList> Handle(
            QueryQuizzesQuery request,
            CancellationToken cancellationToken
        )
        {
            var pageRequest = new PageRequest
            {
                PageNumber = request.PageNumber,
                PageSize = request.PageSize,
            };

            Expression<Func<Domain.Entities.Quiz, bool>> predicate = c =>
                (
                    string.IsNullOrEmpty(request.Search)
                    || c.Title.ToLower().Contains(request.Search)
                )
                && (!request.SectionId.HasValue || c.Content.SectionId == request.SectionId.Value);

            Expression<Func<Domain.Entities.Quiz, object>>? sortExpression =
                request.OrderBy?.ToLower() switch
                {
                    "title" => c => c.Title,
                    _ => c => c.Id,
                };

            var paged = await _unitOfWork.Quizzes.GetByPageFilter(
                pageRequest,
                projectionFunc: c => c.Include(c => c.Content),
                sortExpression: sortExpression,
                predicate: predicate,
                cancellationToken: cancellationToken
            );

            var response = new PagedQuizList
            {
                TotalCount = paged.TotalCount,
                PageNumber = paged.PageNumber,
                PageSize = paged.PageSize,
                TotalPages = paged.TotalPages,
            };

            foreach (var quiz in paged.Items)
            {
                var quizResponse = new QuizResponse
                {
                    Id = quiz.Id,
                    TotalMarks = quiz.TotalMarks,
                    PassingMarks = quiz.PassingMarks,
                    DurationDays = quiz.DurationDays,
                    TimeLimitMinutes = quiz.TimeLimitInMinutes,
                    Status = quiz.Content.Status.ToString(),
                    Title = quiz.Title,
                    Description = quiz.Description,
                    ContentId = quiz.ContentId,
                    CooldownHours = quiz.CooldownHours,
                    MaxAttemptAllowed = quiz.MaxAttemptAllowed,
                    TotalQuestions = quiz.Questions?.Count ?? 0
                };
                quizResponse.Questions.AddRange(
                    quiz.Questions?.Select(x => x.ToGrpcQuestionResponse()) ?? Enumerable.Empty<QuestionResponse>()
                );

                response.Items.Add(quizResponse);
            }

            return response;
        }
    }
}
