namespace Identity.Application.Common.Validation;

public static class BulkProvisioningValidationConstants
{
    public const int MaxCsvFileSizeBytes = 10 * 1024 * 1024;

    public const int MaxRowsPerUpload = 1000;

    public const int MinRowsPerUpload = 1;

    public const int MaxConcurrentJobsPerOrganization = 5;

    public static readonly string[] AllowedFileExtensions = { ".csv" };

    public const int InvitationTokenLength = 32;

    public const int InvitationExpirationDays = 7;

  
    public const int MaxResendAttemptsPerDay = 3;

    
    public const int MaxCsvUploadsPerHour = 10;

    public const int MaxApiCallsPerMinute = 30;
}
