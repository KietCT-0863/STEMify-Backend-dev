using Identity.Domain.Enums;

namespace Identity.Application.Dtos.Users;

/// <summary>
/// DTO representing a user
/// </summary>
public class UserDto
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FullName { get; set; }
    public UserRole Role { get; set; }
    public OrganizationRole? OrganizationRole { get; set; }
    public string? ProfilePictureUrl { get; set; }
    public bool EmailConfirmed { get; set; }
    public DateTime CreatedAt { get; set; }

    // Organization context (if user is accepting invitation)
    public int? OrganizationId { get; set; }
    public string? OrganizationName { get; set; }
}
