using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.Answer
{
    public class CreateAnswerCommand : IRequest<AnswerResponse>
    {
        public string Content { get; set; }
        public bool IsCorrect { get; set; }
        public int QuestionId { get; set; }
    }

    public class CreateAnswerCommandValidator : AbstractValidator<CreateAnswerCommand>
    {
        public CreateAnswerCommandValidator()
        {
            RuleFor(x => x.Content).NotEmpty().WithMessage("Content is required.");

            RuleFor(x => x.IsCorrect).NotNull().WithMessage("IsCorrect must be specified.");

            RuleFor(x => x.QuestionId)
                .GreaterThan(0)
                .WithMessage("QuestionId must be a positive integer.");
        }
    }
}
