using Identity.Application.Groups.Dtos;
using Identity.Application.Groups.Queries.GetGroupById;
using Identity.Domain.Enums;

namespace Identity.Application.Groups.Queries.GetGroupList;

public class GroupListItemDto
{
    public int Id { get; set; }
    public int OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Code { get; set; }
    public string Status { get; set; }
    public int StudentCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<GroupStudentDto> Students { get; set; } = new();
}

