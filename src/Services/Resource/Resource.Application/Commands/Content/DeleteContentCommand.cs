using FluentValidation;
using MediatR;

namespace Resource.Application.Commands.Content
{
    public class DeleteContentCommand : IRequest
    {
        public int Id { get; set; }
    }

    public class DeleteContentCommandValidator : AbstractValidator<DeleteContentCommand>
    {
        public DeleteContentCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Content ID must be greater than 0.");
        }
    }
}
