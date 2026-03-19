using FluentValidation;
using MediatR;

namespace Product.Application.Features.Component.Commands
{
    public class DeleteComponentCommand : IRequest
    {
        public int Id { get; set; }
    }

    public class DeleteComponentCommandValidator : AbstractValidator<DeleteComponentCommand>
    {
        public DeleteComponentCommandValidator()
        {
            RuleFor(x => x.Id).GreaterThan(0).WithMessage("Component ID must be greater than 0.");
        }
    }
}
