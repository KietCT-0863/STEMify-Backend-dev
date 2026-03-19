using FluentValidation;
using MediatR;

namespace Order.Application.Commands.OrganizationTypes.UpdateOrganizationType
{
    public class UpdateOrganizationTypeCommand : IRequest<Shared.Protos.Order.GrpcOrganizationTypeModel>
    {
        public int Id { get; set; }
        public string? Name { get; set; }
    }

    public class UpdateOrganizationTypeCommandValidator : AbstractValidator<UpdateOrganizationTypeCommand>
    {
        public UpdateOrganizationTypeCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("OrganizationType ID must be greater than 0.");

            RuleFor(x => x)
                .Must(cmd => cmd.Name != null)
                .WithMessage("At least one field must be provided to update.");

            When(x => !string.IsNullOrEmpty(x.Name), () =>
            {
                RuleFor(x => x.Name)
                    .Must(name => !string.IsNullOrWhiteSpace(name))
                    .WithMessage("OrganizationType name must not be whitespace.")
                    .MaximumLength(255)
                    .WithMessage("OrganizationType name must not exceed 255 characters.");
            });
        }
    }
}