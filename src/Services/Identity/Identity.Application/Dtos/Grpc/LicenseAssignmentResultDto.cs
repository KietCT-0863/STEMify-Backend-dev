namespace Identity.Application.Dtos.Grpc;

public class LicenseAssignmentResultDto
{
    public bool Success { get; set; }
    public int? LicenseAssignmentId { get; set; }
    public string? ErrorMessage { get; set; }

    public static LicenseAssignmentResultDto CreateSuccess(int licenseAssignmentId)
    {
        return new LicenseAssignmentResultDto
        {
            Success = true,
            LicenseAssignmentId = licenseAssignmentId
        };
    }

    public static LicenseAssignmentResultDto CreateFailure(string errorMessage)
    {
        return new LicenseAssignmentResultDto
        {
            Success = false,
            ErrorMessage = errorMessage
        };
    }
}
