using Identity.Domain.Enums;
using System.Runtime.CompilerServices;

namespace Identity.Application.Common.Models.Auth;

/// <summary>
/// DTO for user information - Updated for TPT inheritance
/// </summary>
public class UserInfoDto
{
    public string Sub { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool EmailVerified { get; set; }
    public string? UserName { get; set; }
    public string? Name { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? GivenName { get; set; }
    public string? FamilyName { get; set; }
    public string? Picture { get; set; }
    public string? Locale { get; set; }
    public UserStatus Status { get; set; } 

    // TPT-specific properties
    public string? UserType { get; set; }
    public string? Specialization { get; set; } // For teachers
    public string? Major { get; set; } // For students
    public string? Bio { get; set; } // For both teachers and students
    public int? Age { get; set; } // For students
    public int? OrganizationId { get; set; }

    /// <summary>
    /// Convert to dictionary for OpenIddict response - Updated for TPT inheritance
    /// </summary>
    /// <returns></returns>
    public Dictionary<string, object> ToDictionary()
    {
        var claims = new Dictionary<string, object> { ["sub"] = Sub };

        if (!string.IsNullOrEmpty(Email))
        {
            claims["email"] = Email;
            claims["email_verified"] = EmailVerified;
        }

        if (!string.IsNullOrEmpty(Name))
            claims["name"] = Name;

        if (!string.IsNullOrEmpty(GivenName))
            claims["given_name"] = GivenName;

        if (!string.IsNullOrEmpty(FamilyName))
            claims["family_name"] = FamilyName;

        if (!string.IsNullOrEmpty(Picture))
            claims["picture"] = Picture;

        if (!string.IsNullOrEmpty(Locale))
            claims["locale"] = Locale;

        // TPT-specific claims
        if (!string.IsNullOrEmpty(UserType))
            claims["user_type"] = UserType;

        if (!string.IsNullOrEmpty(Specialization))
            claims["specialization"] = Specialization;

        if (!string.IsNullOrEmpty(Major))
            claims["major"] = Major;

        if (!string.IsNullOrEmpty(Bio))
            claims["bio"] = Bio;

        if (Age.HasValue)
            claims["age"] = Age.Value;

        if (OrganizationId.HasValue)
            claims["organization_id"] = OrganizationId.Value;

        return claims;
    }
}
