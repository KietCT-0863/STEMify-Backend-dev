using FluentValidation;
using MediatR;

namespace Resource.Application.Commands.Answer
{
    public class DeleteAnswerCommand : IRequest
    {
        public int Id { get; set; }
    }

    public class DeleteAnswerCommandValidator : AbstractValidator<DeleteAnswerCommand>
    {
        public DeleteAnswerCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Answer ID must be greater than 0.");
        }
    }
}
