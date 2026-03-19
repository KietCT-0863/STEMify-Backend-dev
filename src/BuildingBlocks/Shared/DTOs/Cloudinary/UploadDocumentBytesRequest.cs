using FluentValidation;

namespace Shared.DTOs.Cloudinary
{
    public class UploadDocumentBytesRequest
    {
        public byte[] FileBytes { get; set; }
        public string FileName { get; set; }
    }

    public class UploadDocumentBytesRequestValidator : AbstractValidator<UploadDocumentBytesRequest>
    {
        public UploadDocumentBytesRequestValidator()
        {
            RuleFor(x => x.FileBytes)
                .NotNull()
                .WithMessage("File bytes are required.")
                .Must(bytes => bytes.Length > 0)
                .WithMessage("File bytes cannot be empty.");

            RuleFor(x => x.FileName).NotEmpty().WithMessage("File name is required.");
        }
    }
}
