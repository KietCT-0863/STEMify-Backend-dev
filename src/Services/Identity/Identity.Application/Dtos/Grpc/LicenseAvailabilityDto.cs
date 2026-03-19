namespace Identity.Application.Dtos.Grpc;

public class LicenseAvailabilityDto
{
    public bool Available { get; set; }
    public int AvailableCount { get; set; }
    public int TotalLicenses { get; set; }
    public int UsedLicenses { get; set; }
    public string Message { get; set; } = string.Empty;

    public static LicenseAvailabilityDto CreateAvailable(int availableCount, int totalLicenses, int usedLicenses)
    {
        return new LicenseAvailabilityDto
        {
            Available = true,
            AvailableCount = availableCount,
            TotalLicenses = totalLicenses,
            UsedLicenses = usedLicenses,
            Message = $"{availableCount} licenses available"
        };
    }

    public static LicenseAvailabilityDto CreateUnavailable(int requestedCount, int availableCount)
    {
        return new LicenseAvailabilityDto
        {
            Available = false,
            AvailableCount = availableCount,
            Message = $"Insufficient licenses. Requested: {requestedCount}, Available: {availableCount}"
        };
    }
}
