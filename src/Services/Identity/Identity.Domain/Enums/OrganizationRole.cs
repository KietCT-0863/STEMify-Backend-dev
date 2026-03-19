using System;
using System.ComponentModel;

namespace Identity.Domain.Enums;

/// <summary>
/// Organization-level role within a specific subscription
/// Represents the user's role within an organization, NOT their platform role
/// Platform-level roles are defined in UserRole enum
/// </summary>
public enum OrganizationRole
{
    [Description("Student")]
    Student = 1,

    [Description("Teacher")]
    Teacher = 2,

    [Description("Organization Administrator")]
    OrganizationAdmin = 3,
}