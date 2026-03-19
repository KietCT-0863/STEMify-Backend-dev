using MediatR;
using Resource.Application.Common.Interfaces;
using Resource.Application.Extensions.Mapping;
using Resource.Application.Queries.Quiz;
using Resource.Application.Specifications.Quizzes;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Quiz
{
    public class GetQuizByIdQueryHandler : IRequestHandler<GetQuizByIdQuery, QuizResponse>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public GetQuizByIdQueryHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<QuizResponse> Handle(
            GetQuizByIdQuery request,
            CancellationToken cancellationToken
        )
        {
            var spec = new QuizByIdSpecification(request.Id);
            var quiz = await _unitOfWork.Quizzes.FirstOrDefaultAsync(spec, cancellationToken);

            if (quiz == null)
                throw new KeyNotFoundException($"Quiz with ID {request.Id} not found.");

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
                CooldownHours = quiz.CooldownHours,
                MaxAttemptAllowed = quiz.MaxAttemptAllowed,
                ContentId = quiz.ContentId,
                TotalQuestions = quiz.Questions?.Count ?? 0
            };
            response.Questions.AddRange(
                quiz.Questions?
                .OrderBy(q => q.OrderIndex)
                .Select(x => x.ToGrpcQuestionResponse()) ?? []
            );

            return response;

        }
    }
}
