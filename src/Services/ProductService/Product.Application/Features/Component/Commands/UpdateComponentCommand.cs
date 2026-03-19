using FluentValidation;
using MediatR;
using Shared.Helper;
using Shared.Protos.Product;

namespace Product.Application.Features.Component.Commands
{
    public class UpdateComponentCommand : IRequest<ComponentResponse>
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public byte[]? ImageBytes { get; set; }
    }

    public class UpdateComponentCommandValidator : AbstractValidator<UpdateComponentCommand>
    {
        public UpdateComponentCommandValidator()
        {
            RuleFor(x => x.Id)
                .GreaterThan(0)
                .WithMessage("Component ID must be greater than 0.");

            When(x => x.Name != null, () =>
            {
                RuleFor(x => x.Name)
                    .NotEmpty().WithMessage("Component name cannot be empty if provided.")
                    .MaximumLength(255).WithMessage("Component name must not exceed 255 characters.");
            });

            When(x => x.ImageBytes != null, () =>
            {
                RuleFor(x => x.ImageBytes)
                    .Must(FileTypeHelper.IsImage).WithMessage("Invalid image file format.")
                    .Must(bytes => bytes == null || bytes.Length <= 5 * 1024 * 1024)
                    .WithMessage("Image size must not exceed 5MB.");
            });
        }
    }
}
