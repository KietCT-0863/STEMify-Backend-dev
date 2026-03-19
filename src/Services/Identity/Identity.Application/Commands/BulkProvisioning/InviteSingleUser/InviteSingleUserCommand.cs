using Identity.Application.Dtos.BulkProvisioning;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Commands.BulkProvisioning.InviteSingleUser;

public class InviteSingleUserCommand : IRequest<SingleInvitationDto>
{
    public int OrganizationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public OrganizationRole Role { get; set; }
    public string? LicenseType { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public string? GroupName { get; set; }
    public string? ExternalId { get; set; }
    public Guid InvitedBy { get; set; }
    
    public int? SubscriptionOrderId { get; set; }
    
    /// <summary>
    /// Invitation expiration in days (default: 30)
    /// </summary>
    public int ExpirationDays { get; set; } = 30;
}

