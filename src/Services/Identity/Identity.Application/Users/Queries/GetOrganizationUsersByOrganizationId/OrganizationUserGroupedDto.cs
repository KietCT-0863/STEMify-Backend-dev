namespace Identity.Application.Users.Queries.GetOrganizationUsersByOrganizationId;

public class OrganizationUserGroupedDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime? LastLoginAt { get; set; }
    
    public List<SubscriptionInfoDto> Subscriptions { get; set; } = [];
}

public class SubscriptionInfoDto
{
    public Guid OrganizationUserId { get; set; }
    public int OrganizationId { get; set; }
    public string OrganizationRole { get; set; } = string.Empty;
    public string LicenseType { get; set; } = string.Empty;
    public string? LicenseAssignmentId { get; set; }
    public int? SubscriptionOrderId { get; set; }
    public bool IsActive { get; set; }
    public DateTime JoinedAt { get; set; }
    public string? GroupName { get; set; }
    public string? GroupCode { get; set; }
    
    public string? Bio { get; set; }
    public DateTime? StudentDateOfBirth { get; set; }
    public string? StudentMajor { get; set; }
    public string? TeacherSpecialization { get; set; }
}

