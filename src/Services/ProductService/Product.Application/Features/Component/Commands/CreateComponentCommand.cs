using FluentValidation;
using MediatR;
using Shared.Helper;
using Shared.Protos.Product;

namespace Product.Application.Features.Component.Commands
{
    public class CreateComponentCommand : IRequest<ComponentResponse>
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public byte[]? ImageBytes { get; set; }
    }

    public class CreateComponentCommandValidator : AbstractValidator<CreateComponentCommand>
    {
        public CreateComponentCommandValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Component name is required.")
                .MaximumLength(255).WithMessage("Component name must not exceed 255 characters.");

            RuleFor(x => x.ImageBytes)
                .NotEmpty().WithMessage("Image is required.")
                .Must(FileTypeHelper.IsImage).WithMessage("Invalid image file format.")
                .Must(bytes => bytes == null || bytes.Length <= 5 * 1024 * 1024)
                .WithMessage("Image size must not exceed 5MB.");
        }

    }
}
