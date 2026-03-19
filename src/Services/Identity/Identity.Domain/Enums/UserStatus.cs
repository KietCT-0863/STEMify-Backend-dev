using System.ComponentModel;

namespace Identity.Domain.Enums;

public enum UserStatus
{
    [Description("Pending")]
    Pending = 1,

    [Description("Active")]
    Active = 2,

    [Description("Disabled")]
    Disabled = 3,

    [Description("Deleted")]
    Deleted = 4,

    [Description("Locked")]
    Locked = 5,
}
