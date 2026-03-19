using FluentValidation;
using MediatR;
using Shared.Protos.Cart;

namespace Cart.Application.Features.Carts.Commands
{
    public class UpdateCartItemCommand : IRequest<CartResponse>
    {
        public string? UserId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; } = 1;
    }

    public class UpdateCartItemCommandValidator : AbstractValidator<UpdateCartItemCommand>
    {
        public UpdateCartItemCommandValidator()
        {
            RuleFor(x => x.ProductId)
                .GreaterThan(0).WithMessage("ProductId must be a positive integer.");
            RuleFor(x => x.Quantity)
                .NotEmpty().WithMessage("Quantity is required.");
        }
    }
}
