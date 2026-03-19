using MongoDB.Bson.Serialization.Attributes;

namespace Emulator.Repository.Entities;

/// <summary>
/// Represents bounding box for a component
/// </summary>
public class Bounds
{
    [BsonElement("min")]
    public Vector3 Min { get; set; } = Vector3.Zero;

    [BsonElement("max")]
    public Vector3 Max { get; set; } = Vector3.Zero;
}
