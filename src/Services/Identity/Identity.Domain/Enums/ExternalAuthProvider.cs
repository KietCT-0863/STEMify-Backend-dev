using System.ComponentModel;

namespace Identity.Domain.Enums;

/// <summary>
/// Represents external authentication providers supported by the system
/// </summary>
public enum ExternalAuthProvider
{
    [Description("None")]
    None = 0,

    [Description("Google")]
    Google = 1
}
