using FluentValidation;
using MediatR;
using Shared.Helper;

namespace Order.Application.Commands.Organizations.CreateOrganization
{
    public class CreateOrganizationCommand : IRequest<Shared.Protos.Order.GrpcOrganizationDetail>
    {
        public string Name { get; set; }
        public int OrganizationTypeId { get; set; }
        public string? Description { get; set; }
        public byte[]? ImageBytes { get; set; }
    }

    public class CreateOrganizationCommandValidator : AbstractValidator<CreateOrganizationCommand>
    {
        private const int MaxImageBytes = 5 * 1024 * 1024;

        public CreateOrganizationCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Organization name is required.")
                .Must(name => !string.IsNullOrWhiteSpace(name))
                .WithMessage("Organization name must not be whitespace.")
                .MaximumLength(255)
                .WithMessage("Organization name must not exceed 255 characters.");

            RuleFor(x => x.OrganizationTypeId)
                .GreaterThan(0)
                .WithMessage("OrganizationTypeId must be greater than 0.");

            RuleFor(x => x.Description)
                .MaximumLength(2000)
                .WithMessage("Description must not exceed 2000 characters.")
                .When(x => x.Description != null);

            When(x => x.ImageBytes != null, () =>
            {
                RuleFor(x => x.ImageBytes)
                    .Must(bytes => bytes != null && bytes.Length > 0)
                    .WithMessage("Image is required.")
                    .Must(bytes => bytes != null && bytes.Length <= MaxImageBytes)
                    .WithMessage($"Image must not exceed {MaxImageBytes / 1024 / 1024} MB.")
                    .Must(bytes => FileTypeHelper.IsImage(bytes!))
                    .WithMessage("Invalid image file format.");
            });
        }
    }
}