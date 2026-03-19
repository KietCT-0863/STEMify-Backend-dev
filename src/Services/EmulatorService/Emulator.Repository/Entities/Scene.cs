using MongoDB.Bson.Serialization.Attributes;

namespace Emulator.Repository.Entities;

/// <summary>
/// 3D scene configuration (camera, lighting, performance)
/// </summary>
public class Scene
{
    [BsonElement("environment")]
    public Environment Environment { get; set; } = new();

    [BsonElement("lod")]
    [BsonIgnoreIfNull]
    public LodSettings? Lod { get; set; }

    [BsonElement("streaming")]
    [BsonIgnoreIfNull]
    public StreamingSettings? Streaming { get; set; }

    [BsonElement("performance")]
    [BsonIgnoreIfNull]
    public PerformanceSettings? Performance { get; set; }
}

// ==================== Environment ====================
public class Environment
{
    [BsonElement("background")]
    public string Background { get; set; } = string.Empty;

    [BsonElement("lighting")]
    public Lighting Lighting { get; set; } = new();

    [BsonElement("camera")]
    public Camera Camera { get; set; } = new();
}

public class Lighting
{
    [BsonElement("ambient")]
    public string Ambient { get; set; } = string.Empty;

    [BsonElement("directional")]
    public DirectionalLight Directional { get; set; } = new();
}

public class DirectionalLight
{
    [BsonElement("color")]
    public string Color { get; set; } = string.Empty;

    [BsonElement("intensity")]
    public double Intensity { get; set; }

    [BsonElement("position")]
    public Vector3 Position { get; set; } = new();
}

public class Camera
{
    [BsonElement("position")]
    public Vector3 Position { get; set; } = new();

    [BsonElement("target")]
    public Vector3 Target { get; set; } = new();

    [BsonElement("fov")]
    public int Fov { get; set; }
}

// ==================== LOD Settings ====================
public class LodSettings
{
    [BsonElement("enabled")]
    public bool Enabled { get; set; } = false;

    [BsonElement("distances")]
    public List<int> Distances { get; set; } = new();

    [BsonElement("autoAdjust")]
    public bool AutoAdjust { get; set; } = false;
}

// ==================== Streaming Settings ====================
public class StreamingSettings
{
     [BsonElement("enabled")]
     public bool Enabled { get; set; } = false;

    [BsonElement("chunkSize")]
    public string ChunkSize { get; set; } = string.Empty;

    [BsonElement("preloadRadius")]
    public int PreloadRadius { get; set; }
}

// ==================== Performance Settings ====================
public class PerformanceSettings
{
    [BsonElement("targetFPS")]
    public int TargetFPS { get; set; }

    [BsonElement("maxDrawCalls")]
    public int MaxDrawCalls { get; set; }

    [BsonElement("enableOcclusion")]
    public bool EnableOcclusion { get; set; } = false;
}