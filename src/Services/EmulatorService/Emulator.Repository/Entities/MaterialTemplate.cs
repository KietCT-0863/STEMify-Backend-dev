using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Emulator.Repository.Entities;

/// <summary>
/// Represents a reusable material template for rendering
/// Materials are immutable and cached globally for performance
/// </summary>
public class MaterialTemplate
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>
    /// Unique material identifier (e.g., "plastic_green", "metal_steel")
    /// </summary>
    [BsonElement("materialId")]
    public string MaterialId { get; set; } = string.Empty;

    /// <summary>
    /// Material version for versioning support (default: "1.0")
    /// </summary>
    [BsonElement("version")]
    public string Version { get; set; } = "1.0";

    /// <summary>
    /// Display name for UI
    /// </summary>
    [BsonElement("name")]
    [BsonIgnoreIfNull]
    public string? Name { get; set; }

    /// <summary>
    /// Description for documentation
    /// </summary>
    [BsonElement("description")]
    [BsonIgnoreIfNull]
    public string? Description { get; set; }

    /// <summary>
    /// Material definition for PBR (Physically Based Rendering)
    /// Stored as flexible document to support different material types
    /// </summary>
    [BsonElement("definition")]
    public Dictionary<string, object> Definition { get; set; } = new();

    /// <summary>
    /// Tags for categorization and search (e.g., ["plastic", "green", "matte"])
    /// </summary>
    [BsonElement("tags")]
    [BsonIgnoreIfNull]
    public List<string>? Tags { get; set; }

    /// <summary>
    /// Thumbnail URL for preview
    /// </summary>
    [BsonElement("thumbnailUrl")]
    [BsonIgnoreIfNull]
    public string? ThumbnailUrl { get; set; }

    /// <summary>
    /// Whether this material is published and available for use
    /// </summary>
    [BsonElement("isPublished")]
    public bool IsPublished { get; set; } = true;

    /// <summary>
    /// Who created this material
    /// </summary>
    [BsonElement("createdBy")]
    [BsonIgnoreIfNull]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Organization that owns this material (null = public/system material)
    /// </summary>
    [BsonElement("organizationId")]
    [BsonIgnoreIfNull]
    public string? OrganizationId { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    [BsonElement("createdAt")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update timestamp
    /// </summary>
    [BsonElement("updatedAt")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// CDN URL for static hosting (if material textures are hosted on CDN)
    /// </summary>
    [BsonElement("cdnUrl")]
    [BsonIgnoreIfNull]
    public string? CdnUrl { get; set; }

    /// <summary>
    /// Usage count for analytics
    /// </summary>
    [BsonElement("usageCount")]
    public int UsageCount { get; set; } = 0;
}
