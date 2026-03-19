namespace Identity.Application.Groups.Queries.GetGroupById;

public class GroupStudentDto
{
    public Guid OrganizationUserId { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int? SubscriptionOrderId { get; set; }
    public DateTime JoinedAt { get; set; }
    public bool IsActive { get; set; }
}

