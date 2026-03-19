using FluentValidation;
using Shared.Helper;

namespace Product.Application.Models
{
    public class KitImageUploadDto
    {
        public byte[] ImageBytes { get; set; }
        public string AltText { get; set; }
    }

    public class KitImageUploadDtoValidator : AbstractValidator<KitImageUploadDto>
    {
        public KitImageUploadDtoValidator()
        {
            RuleFor(x => x.ImageBytes)
                .NotEmpty().WithMessage("Image is required.")
                .Must(FileTypeHelper.IsImage).WithMessage("Invalid image file format.");

            RuleFor(x => x.AltText)
                .NotEmpty().WithMessage("Alt text is required.")
                .MaximumLength(255);
        }
    }
}
