using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.Question
{
    public class UpdateQuestionsCommand : IRequest<QuestionList>
    {
        public int QuizId { get; set; }
        public List<UpdateQuestionModel> Questions { get; set; } = [];
    }

    public class UpdateQuestionModel
    {
        public int? Id { get; set; }
        public Domain.Enums.QuestionType QuestionType { get; set; }
        public string Content { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public string? FileUrl { get; set; }
        public string? AnswerExplanation { get; set; }
        public int Points { get; set; }
        public List<UpdateAnswerModel> Answers { get; set; } = [];
    }

    public class UpdateAnswerModel
    {
        public int? Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}
