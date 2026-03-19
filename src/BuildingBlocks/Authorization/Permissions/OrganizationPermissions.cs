namespace BuildingBlocks.Authorization.Permissions;

public static class OrganizationPermissions
{
    // ========================================
    // ORGANIZATION MANAGEMENT
    // ========================================

    public const string ViewOrganization = "organization:view";
    public const string UpdateOrganization = "organization:update";
    public const string DeleteOrganization = "organization:delete";
    public const string ManageOrganizationSettings = "organization:manage_settings";
    public const string ManageOrganizationBilling = "organization:manage_billing";

    // ========================================
    // MEMBER MANAGEMENT
    // ========================================

    public const string ViewMembers = "members:view";
    public const string InviteMembers = "members:invite";
    public const string RemoveMembers = "members:remove";
    public const string UpdateMemberRoles = "members:update_roles";
    public const string ViewMemberDetails = "members:view_details";

    // ========================================
    // SUBSCRIPTION MANAGEMENT
    // ========================================

    public const string ViewSubscriptions = "subscriptions:view";
    public const string ManageSubscriptions = "subscriptions:manage";
    public const string AssignLicenses = "subscriptions:assign_licenses";
    public const string RevokeLicenses = "subscriptions:revoke_licenses";

    // ========================================
    // COURSE MANAGEMENT
    // ========================================

    public const string ViewCourses = "courses:view";
    public const string CreateCourses = "courses:create";
    public const string UpdateCourses = "courses:update";
    public const string DeleteCourses = "courses:delete";
    public const string PublishCourses = "courses:publish";
    public const string ManageCourseContent = "courses:manage_content";

    // ========================================
    // ASSIGNMENT MANAGEMENT
    // ========================================

    public const string ViewAssignments = "assignments:view";
    public const string CreateAssignments = "assignments:create";
    public const string UpdateAssignments = "assignments:update";
    public const string DeleteAssignments = "assignments:delete";
    public const string GradeAssignments = "assignments:grade";
    public const string ViewSubmissions = "assignments:view_submissions";

    // ========================================
    // CLASS MANAGEMENT
    // ========================================

    public const string ViewClasses = "classes:view";
    public const string CreateClasses = "classes:create";
    public const string UpdateClasses = "classes:update";
    public const string DeleteClasses = "classes:delete";
    public const string ManageClassEnrollment = "classes:manage_enrollment";

    // ========================================
    // STUDENT PERMISSIONS
    // ========================================

    public const string ViewOwnProfile = "student:view_own_profile";
    public const string UpdateOwnProfile = "student:update_own_profile";
    public const string EnrollInClasses = "student:enroll_classes";
    public const string SubmitAssignments = "student:submit_assignments";
    public const string ViewOwnGrades = "student:view_own_grades";
    public const string ViewCourseMaterials = "student:view_course_materials";

    // ========================================
    // TEACHER PERMISSIONS
    // ========================================

    public const string ViewTeacherDashboard = "teacher:view_dashboard";
    public const string ManageOwnClasses = "teacher:manage_own_classes";
    public const string ViewStudentProgress = "teacher:view_student_progress";
    public const string ExportGrades = "teacher:export_grades";

    // ========================================
    // ANALYTICS & REPORTING
    // ========================================

    public const string ViewAnalytics = "analytics:view";
    public const string ViewReports = "reports:view";
    public const string ExportReports = "reports:export";
    public const string ViewAuditLogs = "audit:view_logs";

    // ========================================
    // NOTIFICATION MANAGEMENT
    // ========================================

    public const string SendNotifications = "notifications:send";
    public const string ManageNotificationSettings = "notifications:manage_settings";

    public static IEnumerable<string> GetAllPermissions()
    {
        return typeof(OrganizationPermissions)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => f.GetValue(null) as string)
            .Where(v => v != null)
            .Cast<string>();
    }

    
    public static bool IsValidPermission(string permission)
    {
        return GetAllPermissions().Contains(permission);
    }

    public static Dictionary<string, List<string>> GetPermissionsByCategory()
    {
        var permissions = GetAllPermissions().ToList();
        return permissions
            .GroupBy(p => p.Split(':')[0]) 
            .ToDictionary(
                g => g.Key,
                g => g.ToList()
            );
    }
}
