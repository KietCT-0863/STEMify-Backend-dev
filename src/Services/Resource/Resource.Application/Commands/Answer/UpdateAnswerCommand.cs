using FluentValidation;
using MediatR;
using Shared.Protos.Resource;

namespace Resource.Application.Commands.Answer
{
    public class UpdateAnswerCommand : IRequest<AnswerResponse>
    {
        public int Id { get; set; }
        public string Content { get; set; }
        public bool IsCorrect { get; set; }
    }

    public class UpdateAnswerCommandValidator : AbstractValidator<UpdateAnswerCommand>
    {
        public UpdateAnswerCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Answer ID must be greater than 0.");

            RuleFor(x => x.Content).NotEmpty().WithMessage("Content is required.");

            RuleFor(x => x.IsCorrect).NotNull().WithMessage("IsCorrect must be specified.");
        }
    }
}
