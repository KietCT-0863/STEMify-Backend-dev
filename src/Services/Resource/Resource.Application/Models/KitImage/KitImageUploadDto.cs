using FluentValidation;

namespace Resource.Application.Models.KitImage
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
                .Must(BeAValidImage).WithMessage("Invalid image file format.");

            RuleFor(x => x.AltText)
                .NotEmpty().WithMessage("Alt text is required.")
                .MaximumLength(255);
        }

        private bool BeAValidImage(byte[]? bytes)
        {
            if (bytes == null || bytes.Length < 4) return false;

            // JPEG/JPG: FF D8 FF
            if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
                return true;

            // PNG: 89 50 4E 47
            if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                return true;

            // GIF: GIF87a or GIF89a
            if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46)
                return true;

            // BMP: 42 4D
            if (bytes[0] == 0x42 && bytes[1] == 0x4D)
                return true;

            // TIFF: 49 49 2A 00 (little endian) or 4D 4D 00 2A (big endian)
            if ((bytes[0] == 0x49 && bytes[1] == 0x49 && bytes[2] == 0x2A && bytes[3] == 0x00) ||
                (bytes[0] == 0x4D && bytes[1] == 0x4D && bytes[2] == 0x00 && bytes[3] == 0x2A))
                return true;

            // WEBP: 52 49 46 46 ("RIFF")...."WEBP"
            if (bytes.Length >= 12 &&
                bytes[0] == 0x52 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x46 &&
                bytes[8] == 0x57 && bytes[9] == 0x45 && bytes[10] == 0x42 && bytes[11] == 0x50)
                return true;

            return false; // not recognized
        }
    }
}
