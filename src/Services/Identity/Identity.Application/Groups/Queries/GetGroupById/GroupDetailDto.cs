using Identity.Application.Groups.Dtos;
using Identity.Domain.Enums;

namespace Identity.Application.Groups.Queries.GetGroupById;

public class GroupDetailDto
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Code { get; set; }
    public GroupStatus Status { get; set; }
    public Guid CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<GroupStudentDto> Students { get; set; } = new();
    public int StudentCount { get; set; } 
    public int TotalStudentCount { get; set; } 
    public int? FilteredSubscriptionOrderId { get; set; }
}

