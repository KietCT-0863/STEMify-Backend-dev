using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace Shared.DTOs.Cloudinary
{
    public class UploadImageRequest
    {
        public IFormFile File { get; set; }
    }

    public class UploadImageRequestValidator : AbstractValidator<UploadImageRequest>
    {
        public UploadImageRequestValidator()
        {
            RuleFor(x => x.File)
                .NotNull()
                .WithMessage("File is required.")
                .Must(file => file.Length > 0)
                .WithMessage("File cannot be empty.")
                .Must(file => file.Length <= 5 * 1024 * 1024) // 5 MB limit
                .WithMessage("File size must not exceed 5 MB.")
                .Must(file => file.ContentType.StartsWith("image/"))
                .WithMessage("File must be an image.");
        }
    }
}
