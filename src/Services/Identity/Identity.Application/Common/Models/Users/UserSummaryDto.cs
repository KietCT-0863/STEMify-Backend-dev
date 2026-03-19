namespace Identity.Application.Common.Models.Users;

/// <summary>
/// Summary DTO for user information with TPT support
/// </summary>
public class UserSummaryDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string UserType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Optional type-specific properties
    public string? Bio { get; set; }
    public string? Specialization { get; set; } // For teachers
    public string? Major { get; set; } // For students
    public int? Age { get; set; } // For students
}
