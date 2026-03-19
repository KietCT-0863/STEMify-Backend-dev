using FluentValidation;
using Identity.Application.Common.Validation;
using Microsoft.AspNetCore.Http;

namespace Identity.Application.Commands.BulkProvisioning.UploadBulkInvitationCsv;

public class UploadBulkInvitationCsvCommandValidator : AbstractValidator<UploadBulkInvitationCsvCommand>
{
    private const long MaxFileSizeBytes = BulkProvisioningValidationConstants.MaxCsvFileSizeBytes;
    private const int MaxRowCount = BulkProvisioningValidationConstants.MaxRowsPerUpload;
    private static readonly string[] AllowedExtensions = BulkProvisioningValidationConstants.AllowedFileExtensions;

    public UploadBulkInvitationCsvCommandValidator()
    {
        RuleFor(x => x.OrganizationId)
            .GreaterThan(0)
            .WithMessage("Organization ID must be greater than 0");

        RuleFor(x => x.CreatedBy)
            .NotEmpty()
            .WithMessage("CreatedBy user ID is required");

        RuleFor(x => x.CsvFile)
            .NotNull()
            .WithMessage("CSV file is required")
            .Must(BeValidFileSize)
            .WithMessage($"File size must not exceed {MaxFileSizeBytes / (1024 * 1024)} MB")
            .Must(BeValidFileExtension)
            .WithMessage($"File must be a CSV file ({string.Join(", ", AllowedExtensions)})");
    }

    private bool BeValidFileSize(IFormFile? file)
    {
        if (file == null) return false;
        return file.Length > 0 && file.Length <= MaxFileSizeBytes;
    }

    private bool BeValidFileExtension(IFormFile? file)
    {
        if (file == null) return false;

        var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        return !string.IsNullOrEmpty(extension) && AllowedExtensions.Contains(extension);
    }
}
