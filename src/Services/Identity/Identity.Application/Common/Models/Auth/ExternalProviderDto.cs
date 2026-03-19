using Identity.Domain.Enums;

namespace Identity.Application.Common.Models.Auth;
public class ExternalProviderDto
{
    public string ProviderName { get; set; } = string.Empty;

    public ExternalAuthProvider ProviderType { get; set; }

    public string ProviderDisplayName { get; set; } = string.Empty;

    public DateTime LinkedAt { get; set; }

    public string? Email { get; set; }
}
