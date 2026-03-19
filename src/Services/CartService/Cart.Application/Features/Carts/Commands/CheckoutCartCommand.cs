using FluentValidation;
using MediatR;

namespace Cart.Application.Features.Carts.Commands
{
    public class CheckoutCartCommand : IRequest
    {
        public string UserId { get; set; }
    }

    public class CheckoutCartCommandValidator : AbstractValidator<CheckoutCartCommand>
    {
        public CheckoutCartCommandValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("User ID is required.");
        }
    }
}
