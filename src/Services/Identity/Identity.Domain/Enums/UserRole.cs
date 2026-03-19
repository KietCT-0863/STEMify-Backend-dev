using System.ComponentModel;

namespace Identity.Domain.Enums;

/// <summary>
/// Platform-level user role (gateway level)
/// Organization-specific roles are defined in OrganizationRole enum
/// </summary>
public enum UserRole
{
    [Description("System Administrator")]
    Admin = 1,

    [Description("System Staff")]
    Staff = 2,

    [Description("Platform Member")]
    Member = 3,

    [Obsolete("Use OrganizationRole.Student instead. This role is deprecated and kept only for backward compatibility with old tokens.")]
    [Description("Student (Deprecated)")]
    Student = 4,

    [Obsolete("Use OrganizationRole.Teacher instead. This role is deprecated and kept only for backward compatibility with old tokens.")]
    [Description("Teacher (Deprecated)")]
    Teacher = 5,


    [Obsolete("Guest role is deprecated. Use Member role instead.")]
    [Description("Guest (Deprecated)")]
    Guest = 7,
}
