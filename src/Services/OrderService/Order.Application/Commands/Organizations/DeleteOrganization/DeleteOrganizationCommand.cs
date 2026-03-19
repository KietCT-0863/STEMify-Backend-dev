using FluentValidation;
using MediatR;

namespace Order.Application.Commands.Organizations.DeleteOrganization
{
    public class DeleteOrganizationCommand : IRequest
    {
        public int Id { get; set; }
    }

    public class DeleteOrganizationCommandValidator : AbstractValidator<DeleteOrganizationCommand>
    {
        public DeleteOrganizationCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Organization ID must be greater than 0.");
        }
    }
}