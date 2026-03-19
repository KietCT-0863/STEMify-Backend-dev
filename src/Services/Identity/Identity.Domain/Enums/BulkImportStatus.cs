using System.ComponentModel;

namespace Identity.Domain.Enums;

/// <summary>
/// Represents the status of a bulk user import job
/// </summary>
public enum BulkImportStatus
{
    [Description("Pending")]
    Pending = 1,

    [Description("Processing")]
    Processing = 2,

    [Description("Completed")]
    Completed = 3,

    [Description("Failed")]
    Failed = 4,
}
