using MediatR;
using Resource.Application.Commands.Question;
using Resource.Application.Common.Interfaces;
using Resource.Application.Extensions.Mapping;
using Shared.Protos.Resource;

namespace Resource.Application.Handlers.Question
{
    public class CreateQuestionCommandHandler
    : IRequestHandler<CreateQuestionCommand, QuestionList>
    {
        private readonly IResourceUnitOfWork _unitOfWork;

        public CreateQuestionCommandHandler(IResourceUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        public async Task<QuestionList> Handle(CreateQuestionCommand request, CancellationToken cancellationToken)
        {
            var quiz = await _unitOfWork.Quizzes.FindByIdAsync(request.QuizId, cancellationToken);
            if (quiz == null)
            {
                throw new KeyNotFoundException($"Quiz with ID {request.QuizId} not found.");
            }

            var responses = new QuestionList();
            var createdQuestions = new List<Domain.Entities.Question>();

            foreach (var q in request.Questions)
            {
                var question = new Domain.Entities.Question
                {
                    QuizId = request.QuizId,
                    QuestionType = q.QuestionType,
                    Content = q.Content,
                    OrderIndex = q.OrderIndex,
                    FileUrl = q.FileUrl,
                    AnswerExplanation = q.AnswerExplanation,
                    Points = q.Points,
                    Answers = q.Answers.Select(a => new Domain.Entities.Answer
                    {
                        Content = a.Content,
                        IsCorrect = a.IsCorrect
                    }).ToList()
                };

                createdQuestions.Add(question);

            }
            await _unitOfWork.Questions.AddRangeAsync(createdQuestions, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            foreach (var createdQuestion in createdQuestions)
            {
                var questionResponse = createdQuestion.ToGrpcQuestionResponse();
                responses.Questions.Add(questionResponse);
            }
            return responses;
        }
    }
}
