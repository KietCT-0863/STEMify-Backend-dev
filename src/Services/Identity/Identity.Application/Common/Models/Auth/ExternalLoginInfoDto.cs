using Identity.Domain.Enums;

namespace Identity.Application.Common.Models.Auth;

public class ExternalLoginInfoDto
{
    public string Provider { get; set; } = string.Empty;

    public ExternalAuthProvider ProviderType { get; set; }

    public string ProviderKey { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public string FullName => $"{FirstName} {LastName}".Trim();

    public string? ProfilePictureUrl { get; set; }

    public Dictionary<string, string> AdditionalClaims { get; set; } = new();
}
