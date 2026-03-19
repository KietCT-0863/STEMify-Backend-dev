using System.ComponentModel;

namespace Identity.Domain.Enums;

/// <summary>
/// Represents the status of a user invitation
/// </summary>
public enum InvitationStatus
{
    [Description("Pending")]
    Pending = 1,

    [Description("Accepted")]
    Accepted = 2,

    [Description("Expired")]
    Expired = 3,

    [Description("Failed")]
    Failed = 4,

    [Description("Revoked")]
    Revoked = 5,
}
