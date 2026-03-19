using Identity.Domain.Enums;

namespace Identity.Application.Dtos.BulkProvisioning;

public class BulkImportJobDto
{
    public Guid Id { get; set; }
    public int OrganizationId { get; set; }
    public BulkImportStatus Status { get; set; }
    public int TotalCount { get; set; }
    public int ProcessedCount { get; set; }
    public int SuccessCount { get; set; }
    public int FailedCount { get; set; }
    public decimal ProgressPercentage { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
}
