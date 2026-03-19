namespace Identity.Application.Dtos.BulkProvisioning;

public class SingleInvitationDto
{
    public Guid InvitationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string LicenseType { get; set; } = string.Empty;
    public DateTime InvitedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool EmailSent { get; set; }
    public string? InvitationToken { get; set; }
}

