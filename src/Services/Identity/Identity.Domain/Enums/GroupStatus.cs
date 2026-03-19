using System.ComponentModel;

namespace Identity.Domain.Enums;

public enum GroupStatus
{
    [Description("Active")]
    Active = 1,

    [Description("Archived")]
    Archived = 2,
}

