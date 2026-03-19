using FluentValidation;

namespace Shared.DTOs.Cloudinary
{
    public class UploadImageBytesRequest
    {
        public byte[] FileBytes { get; set; }
        public string FileName { get; set; }
    }

    public class UploadImageBytesRequestValidator : AbstractValidator<UploadImageBytesRequest>
    {
        public UploadImageBytesRequestValidator()
        {
            RuleFor(x => x.FileBytes)
                .NotNull()
                .WithMessage("File bytes are required.")
                .Must(bytes => bytes.Length > 0)
                .WithMessage("File bytes cannot be empty.")
                .Must(bytes => bytes.Length <= 5 * 1024 * 1024) // 5 MB limit
                .WithMessage("File size must not exceed 5 MB.");
            RuleFor(x => x.FileName).NotEmpty().WithMessage("File name is required.");
        }
    }
}
