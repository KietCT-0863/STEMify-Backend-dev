using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Queries.Quiz;
using Resource.Application.Specifications.Quizzes;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Quiz
{
    public class GetQuizListQueryHandler : IRequestHandler<GetQuizListQuery, QuizList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetQuizListQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<QuizList> Handle(
            GetQuizListQuery request,
            CancellationToken cancellationToken
        )
        {
            var spec = new QuizWithIncludesSpecification();
            var quizzes = await _unitOfWork.Quizzes.GetAllAsync(spec, cancellationToken);

            var list = new QuizList();
            foreach (var quiz in quizzes)
            {
                var response = new QuizResponse
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
                    TotalQuestions = quiz.Questions?.Count ?? 0
                };

                list.Quizzes.Add(response);
            }

            return list;
        }
    }
}
