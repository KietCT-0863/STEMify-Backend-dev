namespace BuildingBlocks.Authorization.Permissions;

public static class RolePermissionsMapping
{
    // ========================================
    // STUDENT PERMISSIONS
    // ========================================

    /// <summary>
    /// Permissions for Student role
    /// Students can view courses, submit assignments, and manage their own profile
    /// </summary>
    public static readonly HashSet<string> StudentPermissions = new()
    {
        // Profile management
        OrganizationPermissions.ViewOwnProfile,
        OrganizationPermissions.UpdateOwnProfile,

        // Course & class access
        OrganizationPermissions.ViewCourses,
        OrganizationPermissions.ViewCourseMaterials,
        OrganizationPermissions.ViewClasses,
        OrganizationPermissions.EnrollInClasses,

        // Assignment submission
        OrganizationPermissions.ViewAssignments,
        OrganizationPermissions.SubmitAssignments,
        OrganizationPermissions.ViewOwnGrades,

        // Limited organization view
        OrganizationPermissions.ViewOrganization,
    };

    // ========================================
    // TEACHER PERMISSIONS
    // ========================================

    /// <summary>
    /// Permissions for Teacher role
    /// Teachers can manage courses, assignments, classes, and grade students
    /// </summary>
    public static readonly HashSet<string> TeacherPermissions = new()
    {
        // All student permissions
        OrganizationPermissions.ViewOwnProfile,
        OrganizationPermissions.UpdateOwnProfile,
        OrganizationPermissions.ViewOrganization,

        // Course management
        OrganizationPermissions.ViewCourses,
        OrganizationPermissions.CreateCourses,
        OrganizationPermissions.UpdateCourses,
        OrganizationPermissions.DeleteCourses,
        OrganizationPermissions.PublishCourses,
        OrganizationPermissions.ManageCourseContent,
        OrganizationPermissions.ViewCourseMaterials,

        // Assignment management
        OrganizationPermissions.ViewAssignments,
        OrganizationPermissions.CreateAssignments,
        OrganizationPermissions.UpdateAssignments,
        OrganizationPermissions.DeleteAssignments,
        OrganizationPermissions.GradeAssignments,
        OrganizationPermissions.ViewSubmissions,

        // Class management
        OrganizationPermissions.ViewClasses,
        OrganizationPermissions.CreateClasses,
        OrganizationPermissions.UpdateClasses,
        OrganizationPermissions.DeleteClasses,
        OrganizationPermissions.ManageClassEnrollment,

        // Teacher-specific
        OrganizationPermissions.ViewTeacherDashboard,
        OrganizationPermissions.ManageOwnClasses,
        OrganizationPermissions.ViewStudentProgress,
        OrganizationPermissions.ExportGrades,

        // Member viewing (limited)
        OrganizationPermissions.ViewMembers,
        OrganizationPermissions.ViewMemberDetails,

        // Analytics
        OrganizationPermissions.ViewAnalytics,
        OrganizationPermissions.ViewReports,
        OrganizationPermissions.ExportReports,

        // Notifications
        OrganizationPermissions.SendNotifications,
    };

    // ========================================
    // ORGANIZATION ADMIN PERMISSIONS
    // ========================================

    /// <summary>
    /// Permissions for OrganizationAdmin role
    /// Full administrative control over the organization
    /// </summary>
    public static readonly HashSet<string> OrganizationAdminPermissions = new()
    {
        // ALL Teacher permissions
        OrganizationPermissions.ViewOwnProfile,
        OrganizationPermissions.UpdateOwnProfile,
        OrganizationPermissions.ViewCourses,
        OrganizationPermissions.CreateCourses,
        OrganizationPermissions.UpdateCourses,
        OrganizationPermissions.DeleteCourses,
        OrganizationPermissions.PublishCourses,
        OrganizationPermissions.ManageCourseContent,
        OrganizationPermissions.ViewCourseMaterials,
        OrganizationPermissions.ViewAssignments,
        OrganizationPermissions.CreateAssignments,
        OrganizationPermissions.UpdateAssignments,
        OrganizationPermissions.DeleteAssignments,
        OrganizationPermissions.GradeAssignments,
        OrganizationPermissions.ViewSubmissions,
        OrganizationPermissions.ViewClasses,
        OrganizationPermissions.CreateClasses,
        OrganizationPermissions.UpdateClasses,
        OrganizationPermissions.DeleteClasses,
        OrganizationPermissions.ManageClassEnrollment,
        OrganizationPermissions.ViewTeacherDashboard,
        OrganizationPermissions.ManageOwnClasses,
        OrganizationPermissions.ViewStudentProgress,
        OrganizationPermissions.ExportGrades,
        OrganizationPermissions.ViewAnalytics,
        OrganizationPermissions.ViewReports,
        OrganizationPermissions.ExportReports,
        OrganizationPermissions.SendNotifications,

        // PLUS Organization management
        OrganizationPermissions.ViewOrganization,
        OrganizationPermissions.UpdateOrganization,
        OrganizationPermissions.DeleteOrganization,
        OrganizationPermissions.ManageOrganizationSettings,
        OrganizationPermissions.ManageOrganizationBilling,

        // Member management
        OrganizationPermissions.ViewMembers,
        OrganizationPermissions.InviteMembers,
        OrganizationPermissions.RemoveMembers,
        OrganizationPermissions.UpdateMemberRoles,
        OrganizationPermissions.ViewMemberDetails,

        // Subscription management
        OrganizationPermissions.ViewSubscriptions,
        OrganizationPermissions.ManageSubscriptions,
        OrganizationPermissions.AssignLicenses,
        OrganizationPermissions.RevokeLicenses,

        // Notifications
        OrganizationPermissions.ManageNotificationSettings,

        // Audit logs
        OrganizationPermissions.ViewAuditLogs,
    };

    // ========================================
    // MAIN MAPPING DICTIONARY
    // ========================================

    /// <summary>
    /// Central mapping of OrganizationRole (as string) to permissions
    /// </summary>
    private static readonly Dictionary<string, HashSet<string>> _rolePermissions = new()
    {
        { "Student", StudentPermissions },
        { "Teacher", TeacherPermissions },
        { "OrganizationAdmin", OrganizationAdminPermissions }
    };

    /// <summary>
    /// Get permissions for a specific role
    /// </summary>
    /// <param name="role">OrganizationRole as string (e.g., "Student", "Teacher", "OrganizationAdmin")</param>
    /// <returns>Set of permissions for the role</returns>
    public static HashSet<string> GetPermissionsForRole(string role)
    {
        if (_rolePermissions.TryGetValue(role, out var permissions))
        {
            return permissions;
        }

        // Return empty set for unknown roles
        return new HashSet<string>();
    }

    public static HashSet<string> GetPermissionsForRoles(IEnumerable<string> roles)
    {
        var allPermissions = new HashSet<string>();

        foreach (var role in roles)
        {
            var rolePermissions = GetPermissionsForRole(role);
            allPermissions.UnionWith(rolePermissions);
        }

        return allPermissions;
    }

    public static bool RoleHasPermission(string role, string permission)
    {
        return GetPermissionsForRole(role).Contains(permission);
    }

    public static List<string> GetRolesWithPermission(string permission)
    {
        return _rolePermissions
            .Where(kvp => kvp.Value.Contains(permission))
            .Select(kvp => kvp.Key)
            .ToList();
    }
    public static IEnumerable<string> GetAllRoles()
    {
        return _rolePermissions.Keys;
    }
}
