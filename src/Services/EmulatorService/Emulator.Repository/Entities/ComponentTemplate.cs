using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Emulator.Repository.Entities;

/// <summary>
/// Represents a reusable component template (e.g., straw, connector)
/// Templates are immutable and cached globally for performance
/// </summary>
public class ComponentTemplate
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    /// <summary>
    /// Unique template identifier (e.g., "green_11_2", "2leg")
    /// </summary>
    [BsonElement("templateId")]
    public string TemplateId { get; set; } = string.Empty;

    /// <summary>
    /// Component type (e.g., "straw", "connector")
    /// </summary>
    [BsonElement("type")]
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Template version for versioning support (default: "1.0")
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
    /// Template definition containing geometry, material, physics, etc.
    /// Stored as flexible document to support different component types
    /// </summary>
    [BsonElement("definition")]
    public Dictionary<string, object> Definition { get; set; } = new();

    /// <summary>
    /// Tags for categorization and search (e.g., ["straw", "green", "medium"])
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
    /// Whether this template is published and available for use
    /// </summary>
    [BsonElement("isPublished")]
    public bool IsPublished { get; set; } = true;

    /// <summary>
    /// Who created this template
    /// </summary>
    [BsonElement("createdBy")]
    [BsonIgnoreIfNull]
    public string? CreatedBy { get; set; }

    /// <summary>
    /// Organization that owns this template (null = public/system template)
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
    /// CDN URL for static hosting (if template is hosted on CDN)
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
