using Identity.Domain.Common;
using Identity.Domain.Enums;
using Identity.Domain.Events;
using Identity.Domain.Services;

namespace Identity.Domain.Entities;

/// <summary>
/// Represents a group within an organization.
/// 
/// BUSINESS RULES:
/// - A group belongs to one organization
/// - Group name must be unique within an organization
/// - Group code (if provided) must be unique within an organization
/// - A group can have 0 or many students (via OrganizationUser.GroupId)
/// - Groups can be archived (soft delete) but data is never hard deleted
/// - When a group is archived, students' GroupId is set to null
/// 
/// RELATIONSHIP:
/// - OrganizationUser → Group: 0..1 (a Student can belong to 0 or 1 Group)
/// - Group → OrganizationUser: N (a Group can have many Students)
/// </summary>
public class Group : BaseEntity<int>
{
    public int OrganizationId { get; private set; } 

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public string? Code { get; private set; } 
    public GroupGrade? Grade { get; private set; }
    public GroupStatus Status { get; private set; }
    public Guid CreatedByUserId { get; private set; }

    public ICollection<OrganizationUser> Students { get; set; } = new List<OrganizationUser>();

    private Group() { } 


    public static Group Create(
        int organizationId,
        string name,
        Guid createdByUserId,
        string? description = null,
        string? code = null,
        GroupGrade? grade = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Group name cannot be empty", nameof(name));

        if (organizationId <= 0)
            throw new ArgumentException("Organization ID must be greater than zero", nameof(organizationId));

        var group = new Group
        {
            Id = 0, 
            OrganizationId = organizationId,
            Name = name.Trim(),
            Description = !string.IsNullOrWhiteSpace(description) ? description.Trim() : null,
            Code = !string.IsNullOrWhiteSpace(code) ? code.Trim() : null,
            Grade = grade,
            Status = GroupStatus.Active,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // group.AddDomainEvent(new GroupCreatedEvent(...));

        return group;
    }


    public static Group CreateWithCode(
        int organizationId,
        string name,
        Guid createdByUserId,
        string? organizationCode,
        string? groupSegment,
        string? description = null,
        GroupGrade? grade = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Group name cannot be empty", nameof(name));

        if (organizationId <= 0)
            throw new ArgumentException("Organization ID must be greater than zero", nameof(organizationId));

        var fullCode = GroupCodeBuilder.BuildFullGroupCode(organizationCode, groupSegment, organizationId);

        var group = new Group
        {
            Id = 0, 
            OrganizationId = organizationId,
            Name = name.Trim(),
            Description = !string.IsNullOrWhiteSpace(description) ? description.Trim() : null,
            Code = fullCode,
            Grade = grade,
            Status = GroupStatus.Active,
            CreatedByUserId = createdByUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // group.AddDomainEvent(new GroupCreatedEvent(...));

        return group;
    }

    public void UpdateInfo(string name, string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Group name cannot be empty", nameof(name));

        if (Status == GroupStatus.Archived)
            throw new InvalidOperationException("Cannot update archived group");

        Name = name.Trim();
        Description = !string.IsNullOrWhiteSpace(description) ? description.Trim() : null;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Archive()
    {
        if (Status == GroupStatus.Archived)
            return; 

        Status = GroupStatus.Archived;
        UpdatedAt = DateTime.UtcNow;
        
    }

    public void Activate()
    {
        if (Status == GroupStatus.Active)
            return; 

        Status = GroupStatus.Active;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateCode(string? code)
    {
        Code = !string.IsNullOrWhiteSpace(code) ? code.Trim() : null;
        UpdatedAt = DateTime.UtcNow;
    }
}

