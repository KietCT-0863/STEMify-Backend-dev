using FluentValidation;
using MediatR;

namespace Order.Application.Commands.OrganizationSubscriptionOrders.CancelOrganizationSubscriptionOrder
{
    public class CancelOrganizationSubscriptionOrderCommand : IRequest<bool>
    {
        public int Id { get; set; }
    }


    public class CancelOrganizationSubscriptionOrderCommandValidator : AbstractValidator<CancelOrganizationSubscriptionOrderCommand>
    {
        public CancelOrganizationSubscriptionOrderCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Subscription ID must be greater than 0.");
        }
    }
}
