using Resource.Domain.Entities;
using Shared.Protos.Resource;

namespace Resource.Application.Extensions.Mapping
{
    public static class QuestionMappingExtension
    {
        // Map Domain QuestionResponse to gRPC QuestionResponse
        public static QuestionResponse ToGrpcQuestionResponse(
            this Question question
        )
        {
            var grpcQuestion = new QuestionResponse
            {
                Id = question.Id,
                Content = question.Content,
                QuestionType = question.QuestionType.ToString(),
                OrderIndex = question.OrderIndex,
                FileUrl = question.FileUrl ?? string.Empty,
                AnswerExplanation = question.AnswerExplanation ?? string.Empty,
                Points = question.Points,
            };

            // Add Answers separately
            if (question.Answers != null)
            {
                grpcQuestion.Answers.AddRange(
                    question.Answers
                    .OrderBy(q => q.Id)
                    .Select(a => new AnswerDTO
                    {
                        Id = a.Id,
                        Content = a.Content,
                        IsCorrect = a.IsCorrect
                    })
                );
            }

            return grpcQuestion;
        }
    }
}
