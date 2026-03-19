using FluentValidation;
using MediatR;
using Shared.Protos.Cart;

namespace Cart.Application.Features.Carts.Commands
{
    public class RemoveFromCartCommand : IRequest<CartResponse>
    {
        public int ProductId { get; set; }
        public string? UserId { get; set; }
    }

    public class RemoveFromCartCommandValidator : AbstractValidator<RemoveFromCartCommand>
    {
        public RemoveFromCartCommandValidator()
        {
            RuleFor(x => x.ProductId)
                .GreaterThan(0).WithMessage("ProductId must be a positive integer.");
        }
    }
}
