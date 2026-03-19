using FluentValidation;
using MediatR;

namespace Resource.Application.Commands.Quiz
{
    public class DeleteQuizCommand : IRequest
    {
        public int Id { get; set; }
    }

    public class DeleteQuizCommandValidator : AbstractValidator<DeleteQuizCommand>
    {
        public DeleteQuizCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Quiz ID must be greater than 0.");
        }
    }
}
