using FluentValidation;
using Shared.Helper;

namespace Product.Application.Models
{
    public class KitComponentImageUploadDto
    {
        public byte[] ImageBytes { get; set; } = [];
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int Quantity { get; set; }
        public bool IsMainComponent { get; set; } = false;
    }
    public class KitComponentImageUploadValidator : AbstractValidator<KitComponentImageUploadDto>
    {
        public KitComponentImageUploadValidator()
        {
            RuleFor(x => x.ImageBytes)
                .NotEmpty().WithMessage("Image is required.")
                .Must(FileTypeHelper.IsImage).WithMessage("Invalid image file format.");

            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Name is required.")
                .MaximumLength(255);
        }
    }
}
