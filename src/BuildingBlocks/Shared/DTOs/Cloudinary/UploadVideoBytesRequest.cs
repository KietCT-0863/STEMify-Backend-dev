using FluentValidation;

namespace Shared.DTOs.Cloudinary
{
    public class UploadVideoBytesRequest
    {
        public byte[] FileBytes { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
    }

    public class UploadVideoBytesRequestValidator : AbstractValidator<UploadVideoBytesRequest>
    {
        public UploadVideoBytesRequestValidator()
        {
            RuleFor(x => x.FileBytes)
                .NotNull()
                .WithMessage("File bytes are required.")
                .Must(bytes => bytes.Length > 0)
                .WithMessage("File bytes cannot be empty.");

            RuleFor(x => x.FileName).NotEmpty().WithMessage("File name is required.");

            RuleFor(x => x.ContentType)
                .NotEmpty()
                .WithMessage("Content type is required.")
                .Must(type => type.StartsWith("video/"))
                .WithMessage("Content type must be a video.");
        }
    }
}
