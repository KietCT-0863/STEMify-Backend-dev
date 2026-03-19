using Identity.Domain.Enums;

namespace Identity.Application.Dtos.BulkProvisioning;

/// <summary>
/// DTO for invitation validation result
/// </summary>
public class InvitationValidationDto
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }

    // Invitation details (if valid)
    public Guid? InvitationId { get; set; }
    public string InviteeEmail { get; set; } = string.Empty;
    public OrganizationRole? TargetRole { get; set; }
    public int? OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public InvitationStatus? Status { get; set; }
    public DateTime ExpiresAt { get; set; }
}
