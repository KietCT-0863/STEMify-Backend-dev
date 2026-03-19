using FluentValidation;
using MediatR;

namespace Resource.Application.Commands.Standard
{
    public class DeleteStandardCommand : IRequest
    {
        public int Id { get; set; }
    }

    public class DeleteStandardCommandValidator : AbstractValidator<DeleteStandardCommand>
    {
        public DeleteStandardCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Standard ID must be greater than 0.");
        }
    }
}
