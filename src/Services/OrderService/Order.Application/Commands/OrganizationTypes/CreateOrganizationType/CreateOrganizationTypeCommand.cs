using FluentValidation;
using MediatR;

namespace Order.Application.Commands.OrganizationTypes.CreateOrganizationType
{
    public class CreateOrganizationTypeCommand : IRequest<Shared.Protos.Order.GrpcOrganizationTypeModel>
    {
        public string Name { get; set; }
    }

    public class CreateOrganizationTypeCommandValidator : AbstractValidator<CreateOrganizationTypeCommand>
    {
        public CreateOrganizationTypeCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("OrganizationType name is required.")
                .Must(name => !string.IsNullOrWhiteSpace(name))
                .WithMessage("OrganizationType name must not be whitespace.")
                .MaximumLength(255)
                .WithMessage("OrganizationType name must not exceed 255 characters.");
        }
    }
}