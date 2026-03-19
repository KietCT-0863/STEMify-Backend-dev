using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Emulator.Repository.Entities;

/// <summary>
/// Follows JSON Patch RFC 6902 standard
/// </summary>
public class EmulationOperation
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>
    /// Emulation this operation belongs to
    /// </summary>
    [BsonElement("emulationId")]
    [BsonRequired]
    public string EmulationId { get; set; } = string.Empty;

    /// <summary>
    /// Sequence number (monotonically increasing per emulation)
    /// Used for ordering and conflict detection
    /// </summary>
    [BsonElement("seq")]
    [BsonRequired]
    public long Seq { get; set; }

    /// <summary>
    /// JSON Patch operation type: "add", "remove", "replace", "move", "copy", "test"
    /// RFC 6902: https://tools.ietf.org/html/rfc6902
    /// </summary>
    [BsonElement("op")]
    [BsonRequired]
    public string Op { get; set; } = string.Empty;

    /// <summary>
    /// JSON Pointer path (e.g., "/components/squares/0/componentMatrix/rotation/y")
    /// RFC 6901: https://tools.ietf.org/html/rfc6901
    /// </summary>
    [BsonElement("path")]
    [BsonRequired]
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// New value for add/replace operations
    /// Can be any JSON type: string, number, boolean, object, array, null
    /// </summary>
    [BsonElement("value")]
    [BsonIgnoreIfNull]
    public object? Value { get; set; }

    /// <summary>
    /// Old value (for undo/conflict resolution)
    /// Stored for auditing and undo functionality
    /// </summary>
    [BsonElement("oldValue")]
    [BsonIgnoreIfNull]
    public object? OldValue { get; set; }

    /// <summary>
    /// Source path for move/copy operations
    /// </summary>
    [BsonElement("from")]
    [BsonIgnoreIfNull]
    public string? From { get; set; }

    /// <summary>
    /// When this operation was created (server timestamp)
    /// </summary>
    [BsonElement("timestamp")]
    [BsonRequired]
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// User who performed this operation
    /// </summary>
    [BsonElement("userId")]
    [BsonRequired]
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Metadata about the operation (optional)
    /// </summary>
    [BsonElement("metadata")]
    [BsonIgnoreIfNull]
    public OperationMetadata? Metadata { get; set; }

    /// <summary>
    /// Whether this operation has been included in a snapshot
    /// Used for pruning old operations
    /// </summary>
    [BsonElement("appliedToSnapshot")]
    public bool AppliedToSnapshot { get; set; } = false;

    /// <summary>
    /// Sequence number of snapshot this operation was included in
    /// Null if not yet applied to a snapshot
    /// </summary>
    [BsonElement("snapshotSeq")]
    [BsonIgnoreIfNull]
    public long? SnapshotSeq { get; set; }

    /// <summary>
    /// Soft delete flag (for pruning without data loss)
    /// </summary>
    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// When this operation was soft deleted (for audit trail)
    /// </summary>
    [BsonElement("deletedAt")]
    [BsonIgnoreIfNull]
    public DateTime? DeletedAt { get; set; }
}

/// <summary>
/// Additional metadata about an operation
/// Used for analytics, debugging, and UI features
/// </summary>
public class OperationMetadata
{
    /// <summary>
    /// Type of action: "user", "system", "ai"
    /// </summary>
    [BsonElement("actionType")]
    public string? ActionType { get; set; }

    /// <summary>
    /// Tool that generated this operation
    /// Examples: "transform_tool", "connector_arm_adjuster", "straw_placer"
    /// </summary>
    [BsonElement("tool")]
    public string? Tool { get; set; }

    /// <summary>
    /// Batch ID for grouping related operations
    /// Example: All operations from a single drag gesture share same batchId
    /// </summary>
    [BsonElement("batchId")]
    public string? BatchId { get; set; }

    /// <summary>
    /// Client-side timestamp (for latency measurement)
    /// Difference between clientTimestamp and server timestamp = network latency
    /// </summary>
    [BsonElement("clientTimestamp")]
    [BsonIgnoreIfNull]
    public DateTime? ClientTimestamp { get; set; }

    /// <summary>
    /// Client device information (for analytics)
    /// </summary>
    [BsonElement("deviceInfo")]
    public string? DeviceInfo { get; set; }

    /// <summary>
    /// Session ID (for tracking user sessions)
    /// </summary>
    [BsonElement("sessionId")]
    public string? SessionId { get; set; }
}
