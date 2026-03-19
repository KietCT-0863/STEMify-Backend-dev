using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Emulator.Repository.Entities;

/// <summary>
/// Main emulation entity representing a 3D assembly lab/activity
/// </summary>
public class Emulation
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    [BsonElement("emulationId")]
    public string EmulationId { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("slug")]
    public string Slug { get; set; } = string.Empty;

    [BsonElement("description")]
    public string? Description { get; set; }

    [BsonElement("thumbnailUrl")]
    [BsonIgnoreIfNull]
    public string? ThumbnailUrl { get; set; }

    // Ownership
    [BsonElement("createdBy")]
    public string CreatedBy { get; set; } = string.Empty;

    [BsonElement("organization")]
    public string? Organization { get; set; }

    [BsonElement("visibility")]
    public string Visibility { get; set; } = "private"; // private, organization, public

    // Version Control
    [BsonElement("version")]
    public string Version { get; set; } = "1.0.0";

    [BsonElement("versionHistory")]
    public List<VersionHistory> VersionHistory { get; set; } = new();

    // Template References 
    // References to reusable templates (not full definitions)
    [BsonElement("templateReferences")]
    [BsonIgnoreIfNull]
    public TemplateReferences? TemplateReferences { get; set; }

    // Emulation Definition
    [BsonElement("definition")]
    public EmulationDefinition Definition { get; set; } = new();

    // Statistics
    [BsonElement("statistics")]
    public EmulationStatistics Statistics { get; set; } = new();

    // Usage Tracking
    [BsonElement("usage")]
    public UsageStats Usage { get; set; } = new();

    // Status & Publishing
    [BsonElement("status")]
    public string Status { get; set; } = "draft"; // draft, review, published, archived

    [BsonElement("publishedAt")]
    public DateTime? PublishedAt { get; set; }

    // Timestamps
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Soft Delete
    [BsonElement("isDeleted")]
    public bool IsDeleted { get; set; }

    [BsonElement("deletedAt")]
    public DateTime? DeletedAt { get; set; }

    // ============================================
    // Delta Sync & Event Sourcing
    // ============================================

    /// <summary>
    /// Current sequence number (latest operation seq)
    /// Incremented with each operation appended
    /// </summary>
    [BsonElement("currentSeq")]
    public long CurrentSeq { get; set; } = 0;

    /// <summary>
    /// Sequence number of last snapshot
    /// Operations between lastSnapshotSeq and currentSeq need to be replayed
    /// </summary>
    [BsonElement("lastSnapshotSeq")]
    public long LastSnapshotSeq { get; set; } = 0;

    /// <summary>
    /// When last snapshot was created
    /// Used to trigger periodic snapshots (e.g., every 5 minutes)
    /// </summary>
    [BsonElement("lastSnapshotAt")]
    [BsonIgnoreIfNull]
    public DateTime? LastSnapshotAt { get; set; }

    /// <summary>
    /// Total number of operations (for metrics)
    /// </summary>
    [BsonElement("totalOperations")]
    public long TotalOperations { get; set; } = 0;

    /// <summary>
    /// Published version metadata (snapshot info)
    /// Used to track which seq was published
    /// </summary>
    [BsonElement("publishedVersion")]
    [BsonIgnoreIfNull]
    public PublishedVersionInfo? PublishedVersion { get; set; }
}

public class PublishedVersionInfo
{
    /// <summary>
    /// Sequence number when published
    /// </summary>
    [BsonElement("seq")]
    public long Seq { get; set; }

    /// <summary>
    /// Snapshot ID (for reference)
    /// </summary>
    [BsonElement("snapshotId")]
    public string? SnapshotId { get; set; }

    /// <summary>
    /// When published
    /// </summary>
    [BsonElement("publishedAt")]
    public DateTime PublishedAt { get; set; }
}

/// <summary>
/// Version history entry
/// </summary>
public class VersionHistory
{
    [BsonElement("version")]
    public string Version { get; set; } = string.Empty;

    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; }

    [BsonElement("changes")]
    public string Changes { get; set; } = string.Empty;

    [BsonElement("snapshotUrl")]
    public string? SnapshotUrl { get; set; }
}

/// <summary>
/// Statistics about the emulation
/// </summary>
public class EmulationStatistics
{
    [BsonElement("instanceCount")]
    public InstanceCount InstanceCount { get; set; } = new();

    [BsonElement("connectionCount")]
    public int ConnectionCount { get; set; }

    [BsonElement("actionCount")]
    public int ActionCount { get; set; }

    [BsonElement("activityCount")]
    public int ActivityCount { get; set; }

    [BsonElement("estimatedComplexity")]
    public string EstimatedComplexity { get; set; } = "low";
}

public class InstanceCount
{
    [BsonElement("straws")]
    public int Straws { get; set; }

    [BsonElement("connectors")]
    public int Connectors { get; set; }

    [BsonElement("total")]
    public int Total { get; set; }
}

/// <summary>
/// Usage statistics
/// </summary>
public class UsageStats
{
    [BsonElement("viewCount")]
    public int ViewCount { get; set; }

    [BsonElement("completionCount")]
    public int CompletionCount { get; set; }

    [BsonElement("averageCompletionTime")]
    public int AverageCompletionTime { get; set; }

    [BsonElement("successRate")]
    public double SuccessRate { get; set; }
}

/// <summary>
/// Stores only IDs, not full template definitions (for size optimization)
/// </summary>
public class TemplateReferences
{
    /// <summary>
    /// List of material template IDs used in this emulation
    /// </summary>
    [BsonElement("materials")]
    public List<string> Materials { get; set; } = [];

    /// <summary>
    /// List of component template IDs used in this emulation
    /// </summary>
    [BsonElement("components")]
    public List<string> Components { get; set; } = [];
}
