using FluentValidation;
using MediatR;
using Order.Domain.Enums;
using Shared.Helper;

namespace Order.Application.Commands.Organizations.UpdateOrganization
{
    public class UpdateOrganizationCommand : IRequest<Shared.Protos.Order.GrpcOrganizationDetail>
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int? OrganizationTypeId { get; set; }
        public string? Description { get; set; }
        public byte[]? ImageBytes { get; set; }
        public Domain.Enums.OrganizationStatus? Status { get; set; }
    }

    public class UpdateOrganizationCommandValidator : AbstractValidator<UpdateOrganizationCommand>
    {
        private const int MaxImageBytes = 5 * 1024 * 1024;

        public UpdateOrganizationCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Organization ID must be greater than 0.");

            // At least one updatable field must be provided
            RuleFor(x => x)
                .Must(cmd => cmd.Name != null
                             || cmd.OrganizationTypeId.HasValue
                             || cmd.Description != null
                             || cmd.ImageBytes != null
                             || cmd.Status.HasValue)
                .WithMessage("At least one field must be provided to update.");

            When(x => !string.IsNullOrEmpty(x.Name), () =>
            {
                RuleFor(x => x.Name)
                    .Must(name => !string.IsNullOrWhiteSpace(name))
                    .WithMessage("Organization name must not be whitespace.")
                    .MaximumLength(255)
                    .WithMessage("Organization name must not exceed 255 characters.");
            });

            When(x => x.OrganizationTypeId.HasValue, () =>
            {
                RuleFor(x => x.OrganizationTypeId.Value)
                    .GreaterThan(0)
                    .WithMessage("OrganizationTypeId must be greater than 0.");
            });

            When(x => x.Description != null, () =>
            {
                RuleFor(x => x.Description)
                    .MaximumLength(2000)
                    .WithMessage("Description must not exceed 2000 characters.");
            });

            When(x => x.ImageBytes != null, () =>
            {
                RuleFor(x => x.ImageBytes!)
                    .Must(bytes => bytes.Length > 0)
                    .WithMessage("Image is required.")
                    .Must(bytes => bytes.Length <= MaxImageBytes)
                    .WithMessage($"Image must not exceed {MaxImageBytes / 1024 / 1024} MB.")
                    .Must(bytes => FileTypeHelper.IsImage(bytes!))
                    .WithMessage("Invalid image file format.");
            });

            When(x => x.Status.HasValue, () =>
            {
                RuleFor(x => x.Status.Value)
                    .Must(s => Enum.IsDefined(typeof(OrganizationStatus), s))
                    .WithMessage("Invalid OrganizationStatus value.");
            });
        }
    }
}