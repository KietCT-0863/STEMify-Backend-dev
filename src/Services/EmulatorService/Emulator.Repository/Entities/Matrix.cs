using MongoDB.Bson.Serialization.Attributes;

namespace Emulator.Repository.Entities;

/// <summary>
/// Represents a transformation matrix
/// </summary>
public class Matrix
{
    [BsonElement("position")]
    public Vector3 Position { get; set; } = Vector3.Zero;

    [BsonElement("rotation")]
    public Vector3 Rotation { get; set; } = Vector3.Zero;

    [BsonElement("scale")]
    public Vector3 Scale { get; set; } = Vector3.One;

    [BsonElement("_comment")]
    [BsonIgnoreIfNull]
    public string? Comment { get; set; }
}
