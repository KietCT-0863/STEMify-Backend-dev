using Grpc.Core;
using MediatR;
using Resource.Application.Commands.Question;
using Resource.Domain.Enums;
using ServiceStack;
using Shared.Protos.Resource;

namespace Resource.API.Services
{
    public class QuestionGrpcService : QuestionService.QuestionServiceBase
    {
        private readonly IMediator _mediator;

        public QuestionGrpcService(IMediator mediator)
        {
            _mediator = mediator;
        }

        public override async Task<QuestionList> CreateQuestion(
            CreateQuestionRequest request,
            ServerCallContext context
        )
        {
            var command = new CreateQuestionCommand
            {
                QuizId = request.QuizId,
                Questions = request.Questions.Select(q => new CreateQuestionModel
                {
                    QuestionType = q.QuestionType.ToEnumOrDefault(QuestionType.MultipleChoice),
                    Content = q.Content,
                    OrderIndex = q.OrderIndex,
                    FileUrl = string.IsNullOrWhiteSpace(q.FileUrl) ? null : q.FileUrl,
                    AnswerExplanation = string.IsNullOrWhiteSpace(q.AnswerExplanation) ? null : q.AnswerExplanation,
                    Points = q.Points,
                    Answers = q.Answers.Select(a => new CreateAnswerModel
                    {
                        Content = a.Content,
                        IsCorrect = a.IsCorrect
                    }).ToList()
                }).ToList()
            };

            var result = await _mediator.Send(command);
            return result;
        }

        public override async Task<QuestionList> UpdateQuestion(
            UpdateQuestionRequest request,
            ServerCallContext context
        )
        {
            var command = new UpdateQuestionsCommand
            {
                QuizId = request.QuizId,
                Questions = request.Questions.Select(q => new UpdateQuestionModel
                {
                    Id = q.Id == 0 ? null : q.Id,
                    QuestionType = q.QuestionType.ToEnumOrDefault(QuestionType.MultipleChoice),
                    Content = q.Content,
                    OrderIndex = q.OrderIndex,
                    FileUrl = string.IsNullOrWhiteSpace(q.FileUrl) ? null : q.FileUrl,
                    AnswerExplanation = string.IsNullOrWhiteSpace(q.AnswerExplanation) ? null : q.AnswerExplanation,
                    Points = q.Points,
                    Answers = q.Answers.Select(a => new UpdateAnswerModel
                    {
                        Id = a.Id == 0 ? null : a.Id,
                        Content = a.Content,
                        IsCorrect = a.IsCorrect
                    }).ToList()
                }).ToList()
            };

            var result = await _mediator.Send(command);
            return result;
        }
    }
}
