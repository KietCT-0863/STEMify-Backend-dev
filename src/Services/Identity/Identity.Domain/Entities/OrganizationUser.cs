using Identity.Domain.Common;
using Identity.Domain.Enums;
using Identity.Domain.Events;

namespace Identity.Domain.Entities;

/// <summary>
/// Represents a user's subscription within an organization.
/// 
/// BUSINESS RULES:
/// - A user can have multiple subscriptions (roles) within the same organization
/// - Each subscription has its own role-specific properties (StudentDateOfBirth, TeacherSpecialization, etc.)
/// - Unique constraint: (OrganizationId, UserId) ensures no duplicate subscriptions per organization
/// - Subscriptions can be deactivated (soft delete) for audit purposes, but data is never hard deleted
/// - Profile properties are subscription-scoped, not user-scoped
/// 
/// Example: User A can be:
/// - Teacher in Organization 1, Subscription 101 (Specialization = Math)
/// - Student in Organization 1, Subscription 103 (Major = Computer Science)
/// </summary>
public class OrganizationUser : BaseEntity<Guid>
{
    public int OrganizationId { get; private set; } 

    public Guid UserId { get; private set; } 

    public ApplicationUser User { get; private set; } = null!;

    public OrganizationRole OrganizationRole { get; private set; }
        
    public int? GroupId { get; private set; } 
    
    public Group? Group { get; private set; }

    // Role-specific properties (subscription-scoped)
    public string? Bio { get; private set; }
    public DateTime? StudentDateOfBirth { get; private set; }
    public string? StudentMajor { get; private set; }
    public string? TeacherSpecialization { get; private set; }

    // Metadata
    public DateTime JoinedAt { get; private set; }
    public DateTime? LeftAt { get; private set; }

    public void ActivateMembership()
    {

        UpdatedAt = DateTime.UtcNow;
    }

    private OrganizationUser() { }

    /// <summary>
    /// Called when user accepts invitation or is manually added
    /// </summary>
    public static OrganizationUser Create(
        int organizationId,
        Guid userId,
        OrganizationRole organizationRole,
        string licenseType,
        string? licenseAssignmentId = null,
        int subscriptionOrderId = 0,
        int? groupId = null)
    {
        var orgUser = new OrganizationUser
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = userId,
            OrganizationRole = organizationRole,
            GroupId = groupId,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        orgUser.AddDomainEvent(new UserJoinedOrganizationEvent(
            userId: userId,
            organizationId: organizationId,
            organizationRole: organizationRole,
            joinedAt: orgUser.JoinedAt
        ));

        return orgUser;
    }

    public static OrganizationUser CreatePending(
        int organizationId,
        Guid userId,
        OrganizationRole organizationRole,
        string licenseType,
        string? licenseAssignmentId = null,
        int? subscriptionOrderId = null,
        int? groupId = null)
    {
        var orgUser = new OrganizationUser
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = userId,
            OrganizationRole = organizationRole,
            GroupId = groupId,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        return orgUser;
    }

    // /// <summary>
    // /// Activate pending organization user 
    // /// </summary>
    // public void Activate()
    // {
    //     if (IsActive)
    //         return;

    //     IsActive = true;
    //     UpdatedAt = DateTime.UtcNow;

    //     AddDomainEvent(new UserJoinedOrganizationEvent(
    //         userId: UserId,
    //         organizationId: OrganizationId,
    //         organizationRole: OrganizationRole,
    //         licenseType: string.Empty,
    //         licenseAssignmentId: null,
    //         joinedAt: JoinedAt
    //     ));
    // }

    public void AssignToGroup(int? groupId)
    {
        GroupId = groupId;
        if (!groupId.HasValue)
        {
            Group = null;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    public void AssignToGroup(Group group)
    {
        ArgumentNullException.ThrowIfNull(group);
        Group = group;
        if (group.Id > 0)
        {
            GroupId = group.Id;
        }
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Promote user to organization admin
    /// Business rule: Cannot change if already OrganizationAdmin
    /// </summary>
    public void PromoteToAdmin()
    {
        if (OrganizationRole == Enums.OrganizationRole.OrganizationAdmin)
            throw new InvalidOperationException("User is already OrganizationAdmin");

        OrganizationRole = Enums.OrganizationRole.OrganizationAdmin;
        UpdatedAt = DateTime.UtcNow;

        // Domain event
        // AddDomainEvent(new UserPromotedToOrgAdminEvent(UserId, OrganizationId));
    }

    
    // public void DemoteToMember()
    // {
    //     if (OrganizationRole != OrganizationRole.OrganizationAdmin)
    //         throw new InvalidOperationException("Only admins can be demoted to member");

    //     OrganizationRole = OrganizationRole.Member;
    //     UpdatedAt = DateTime.UtcNow;

    //     // Domain event
    //     // AddDomainEvent(new UserDemotedToMemberEvent(UserId, OrganizationId));
    // }

    /// <summary>
    /// Remove user from organization (deactivate subscription)
    /// Business rule: Cannot remove organization admin
    /// </summary>
    public void RemoveFromOrganization()
    {
        if (OrganizationRole == Enums.OrganizationRole.OrganizationAdmin)
            throw new InvalidOperationException("Cannot remove organization admin");


        LeftAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        // Domain event
        // AddDomainEvent(new UserLeftOrganizationEvent(UserId, OrganizationId));
    }

    /// <summary>
    /// Reactivate user in organization
    /// </summary>
    public void Reactivate()
    {

        LeftAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Check if user has admin privileges in organization
    /// </summary>
    public bool IsOrgAdmin()
    {
        return OrganizationRole == Enums.OrganizationRole.OrganizationAdmin;
    }

    /// <summary>
    /// Check if user is organization admin (alias for IsOrgAdmin)
    /// </summary>
    public bool IsOwner()
    {
        return OrganizationRole == Enums.OrganizationRole.OrganizationAdmin;
    }

    /// <summary>
    /// Get membership duration
    /// </summary>
    public TimeSpan GetMembershipDuration()
    {
        var endDate = LeftAt ?? DateTime.UtcNow;
        return endDate - JoinedAt;
    }

    // ========================================
    // MULTI-SUBSCRIPTION: Role-Specific Factory Methods
    // ========================================

    /// <summary>
    /// Create Student subscription
    /// </summary>
    public static OrganizationUser CreateStudent(
        int organizationId,
        Guid userId,
        int subscriptionOrderId,
        DateTime dateOfBirth,
        string? major = null,
        string? bio = null,
        string? licenseAssignmentId = null,
        int? groupId = null)
    {
        var orgUser = new OrganizationUser
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = userId,
            OrganizationRole = Enums.OrganizationRole.Student,
            GroupId = groupId,
            // Student-specific properties
            StudentDateOfBirth = dateOfBirth,
            StudentMajor = major,
            Bio = bio,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        orgUser.AddDomainEvent(new UserJoinedOrganizationEvent(
            userId: userId,
            organizationId: organizationId,
            organizationRole: Enums.OrganizationRole.Student,
            joinedAt: orgUser.JoinedAt
        ));

        return orgUser;
    }

    /// <summary>
    /// Create Teacher subscription
    /// </summary>
    public static OrganizationUser CreateTeacher(
        int organizationId,
        Guid userId,
        int subscriptionOrderId,
        string? specialization = null,
        string? bio = null,
        string? licenseAssignmentId = null)
    {
        var orgUser = new OrganizationUser
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = userId,
            OrganizationRole = Enums.OrganizationRole.Teacher,
            // Teacher-specific properties
            TeacherSpecialization = specialization,
            Bio = bio,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        orgUser.AddDomainEvent(new UserJoinedOrganizationEvent(
            userId: userId,
            organizationId: organizationId,
            organizationRole: Enums.OrganizationRole.Teacher,
            joinedAt: orgUser.JoinedAt
        ));

        return orgUser;
    }

    /// <summary>
    /// Create OrganizationAdmin subscription
    /// </summary>
    public static OrganizationUser CreateOrganizationAdmin(
        int organizationId,
        Guid userId,
        int subscriptionOrderId,
        string? bio = null,
        string? licenseAssignmentId = null)
    {
        var orgUser = new OrganizationUser
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = userId,
            OrganizationRole = Enums.OrganizationRole.OrganizationAdmin,
            Bio = bio,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        orgUser.AddDomainEvent(new UserJoinedOrganizationEvent(
            userId: userId,
            organizationId: organizationId,
            organizationRole: Enums.OrganizationRole.OrganizationAdmin,
            joinedAt: orgUser.JoinedAt
        ));

        return orgUser;
    }

    // ========================================
    // Role-Specific Profile Update Methods
    // ========================================

    /// <summary>
    /// Update Student profile properties
    /// </summary>
    public void UpdateStudentProfile(
        DateTime? dateOfBirth = null,
        string? major = null,
        string? bio = null)
    {
        if (OrganizationRole != Enums.OrganizationRole.Student)
            throw new InvalidOperationException("Only Student subscriptions can update student profile");

        if (dateOfBirth.HasValue)
            StudentDateOfBirth = dateOfBirth;

        if (major != null)
            StudentMajor = major;

        if (bio != null)
            Bio = bio;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update Teacher profile properties
    /// </summary>
    public void UpdateTeacherProfile(
        string? specialization = null,
        string? bio = null)
    {
        if (OrganizationRole != Enums.OrganizationRole.Teacher)
            throw new InvalidOperationException("Only Teacher subscriptions can update teacher profile");

        if (specialization != null)
            TeacherSpecialization = specialization;

        if (bio != null)
            Bio = bio;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Update OrganizationAdmin profile properties
    /// </summary>
    public void UpdateOrganizationAdminProfile(string? bio = null)
    {
        if (OrganizationRole != Enums.OrganizationRole.OrganizationAdmin)
            throw new InvalidOperationException("Only OrganizationAdmin subscriptions can update admin profile");

        if (bio != null)
            Bio = bio;

        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Deactivate subscription (soft delete)
    /// Business rule: Keeps data for audit, does not hard delete
    /// </summary>
    public void Deactivate()
    {
        LeftAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Get role-specific properties for JWT token
    /// Returns properties with PascalCase keys 
    /// </summary>
    public Dictionary<string, object> GetRoleProperties()
    {
        var properties = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(Bio))
            properties["Bio"] = Bio;

        switch (OrganizationRole)
        {
            case Enums.OrganizationRole.Student:
                if (StudentDateOfBirth.HasValue)
                    properties["StudentDateOfBirth"] = StudentDateOfBirth.Value;
                if (!string.IsNullOrEmpty(StudentMajor))
                    properties["StudentMajor"] = StudentMajor;
                break;

            case Enums.OrganizationRole.Teacher:
                if (!string.IsNullOrEmpty(TeacherSpecialization))
                    properties["TeacherSpecialization"] = TeacherSpecialization;
                break;

            case Enums.OrganizationRole.OrganizationAdmin:
                // Only bio (already added above)
                break;
        }

        return properties;
    }
}
