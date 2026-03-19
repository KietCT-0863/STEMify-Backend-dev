using FluentValidation;
using MediatR;

namespace Order.Application.Commands.OrganizationTypes.DeleteOrganizationType
{
    public class DeleteOrganizationTypeCommand : IRequest
    {
        public int Id { get; set; }
    }

    public class DeleteOrganizationTypeCommandValidator : AbstractValidator<DeleteOrganizationTypeCommand>
    {
        public DeleteOrganizationTypeCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("OrganizationType ID must be greater than 0.");
        }
    }
}