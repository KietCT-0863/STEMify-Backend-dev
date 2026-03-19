namespace Identity.Application.Dtos.Grpc;

public class BulkLicenseCheckDto
{
    public bool AllAvailable { get; set; }
    public Dictionary<string, LicenseCheckResultDto> Results { get; set; } = new();
    public string Message { get; set; } = string.Empty;

    public bool IsLicenseTypeAvailable(string licenseType)
    {
        return Results.TryGetValue(licenseType, out var result) && result.Available;
    }

    public int GetAvailableCount(string licenseType)
    {
        return Results.TryGetValue(licenseType, out var result) ? result.AvailableCount : 0;
    }
}

/// <summary>
/// Individual license type check result
/// </summary>
public class LicenseCheckResultDto
{
    public string LicenseType { get; set; } = string.Empty;
    public bool Available { get; set; }
    public int AvailableCount { get; set; }
    public int RequestedCount { get; set; }
}
