using Identity.Domain.Enums;

namespace Identity.Application.Dtos.BulkProvisioning;

/// <summary>
/// DTO with detailed status information for bulk import job
/// </summary>
public class BulkImportJobStatusDto
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }
    public BulkImportStatus Status { get; set; }
    public int TotalCount { get; set; }
    public int ProcessedCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public decimal ProgressPercentage { get; set; }
    public decimal SuccessRate { get; set; }
    public TimeSpan? EstimatedTimeRemaining { get; set; }
    public double? ProcessingRate { get; set; } // items per second
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? ErrorMessage { get; set; }
    public List<BulkImportFailureDto> Failures { get; set; } = new();
}

/// <summary>
/// DTO representing a single failure in bulk import
/// </summary>
public class BulkImportFailureDto
{
    public string Email { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public DateTime FailedAt { get; set; }
}
