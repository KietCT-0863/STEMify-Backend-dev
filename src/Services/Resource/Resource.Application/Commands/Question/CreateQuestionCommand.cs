using FluentValidation;
using MediatR;
using Resource.Domain.Enums;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.Question
{
    public class CreateQuestionCommand : IRequest<QuestionList>
    {
        public int QuizId { get; set; }
        public List<CreateQuestionModel> Questions { get; set; } = [];
    }
    public class CreateQuestionModel
    {
        public QuestionType QuestionType { get; set; }
        public string Content { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public string? FileUrl { get; set; }
        public string? AnswerExplanation { get; set; }
        public int Points { get; set; }
        public List<CreateAnswerModel> Answers { get; set; } = [];
    }

    public class CreateAnswerModel
    {
        public string Content { get; set; } = String.Empty;
        public bool IsCorrect { get; set; }
    }
    public class CreateQuestionCommandValidator : AbstractValidator<CreateQuestionCommand>
    {
        public CreateQuestionCommandValidator()
        {
            RuleFor(x => x.QuizId)
                .GreaterThan(0)
                .WithMessage("QuizId must be greater than 0.");

            RuleForEach(x => x.Questions)
                .SetValidator(new CreateQuestionModelValidator());

            RuleFor(x => x.Questions)
                .NotEmpty()
                .WithMessage("At least one question must be provided.");
        }
    }

    public class CreateQuestionModelValidator : AbstractValidator<CreateQuestionModel>
    {
        public CreateQuestionModelValidator()
        {
            RuleFor(x => x.QuestionType)
                .IsInEnum()
                .WithMessage("QuestionType must be a valid enum value.");

            RuleFor(x => x.Content)
                .NotEmpty()
                .WithMessage("Content is required.");

            RuleFor(x => x.OrderIndex)
                .GreaterThanOrEqualTo(0)
                .WithMessage("OrderIndex must be 0 or greater.");

            RuleFor(x => x.Points)
                .GreaterThan(0)
                .WithMessage("Points must be greater than 0.");

            RuleForEach(x => x.Answers)
                .SetValidator(new CreateAnswerModelValidator());
        }
    }
    public class CreateAnswerModelValidator : AbstractValidator<CreateAnswerModel>
    {
        public CreateAnswerModelValidator()
        {
            RuleFor(x => x.Content)
                .NotEmpty()
                .WithMessage("Answer content is required.");
        }
    }
}
