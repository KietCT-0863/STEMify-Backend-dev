using MongoDB.Bson.Serialization.Attributes;

namespace Emulator.Repository.Entities;

/// <summary>
/// Represents transformation data (position, rotation, scale)
/// </summary>
public class Transform
{
    [BsonElement("position")]
    public Vector3 Position { get; set; } = Vector3.Zero;

    [BsonElement("rotation")]
    public Vector3 Rotation { get; set; } = Vector3.Zero;

    [BsonElement("scale")]
    [BsonIgnoreIfNull]
    public Vector3? Scale { get; set; }
}
