using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Emulator.Repository.Entities;

/// <summary>
/// Main emulation definition matching octahedron.json structure
/// </summary>
public class EmulationDefinition
{
    [BsonElement("metadata")]
    public Metadata Metadata { get; set; } = new();

    [BsonElement("templates")]
    public Templates Templates { get; set; } = new();

    [BsonElement("_matrixHierarchyDoc")]
    [BsonIgnoreIfNull]
    public MatrixHierarchyDoc? MatrixHierarchyDoc { get; set; }

    [BsonElement("components")]
    [BsonIgnoreIfNull]
    public Components? Components { get; set; }

    [BsonElement("assemblies")]
    public Dictionary<string, AssemblyGroup> Assemblies { get; set; } = new();

    [BsonElement("instances")]
    public Instances Instances { get; set; } = new();

    [BsonElement("connections")]
    public Dictionary<string, List<Connection>> Connections { get; set; } = new();

    [BsonElement("actions")]
    public List<ActionDefinition> Actions { get; set; } = new();

    [BsonElement("activities")]
    public List<Activity> Activities { get; set; } = new();

    [BsonElement("scene")]
    public Scene Scene { get; set; } = new();
}

// ==================== Metadata ====================
public class Metadata
{
    [BsonElement("version")]
    public string Version { get; set; } = "2.0";

    [BsonElement("created")]
    public DateTime Created { get; set; }

    [BsonElement("lastModified")]
    public DateTime? LastModified { get; set; }

    [BsonElement("author")]
    public string Author { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("compressionRatio")]
    public string? CompressionRatio { get; set; }

    [BsonElement("tags")]
    public List<string> Tags { get; set; } = new();

    [BsonElement("difficulty")]
    public string Difficulty { get; set; } = "beginner"; // beginner, intermediate, advanced
}

// ==================== Templates ====================
public class Templates
{
    [BsonElement("materials")]
    public List<TemplateReference> Materials { get; set; } = new();

    [BsonElement("components")]
    public List<TemplateReference> Components { get; set; } = new();
}

public class TemplateReference
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("source")]
    public string Source { get; set; } = string.Empty;

    [BsonElement("_resolved")]
    [BsonIgnoreIfNull]
    public BsonDocument? Resolved { get; set; }
}

// ==================== Matrix Hierarchy Documentation ====================
public class MatrixHierarchyDoc
{
    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("levels")]
    public Dictionary<string, string> Levels { get; set; } = new();

    [BsonElement("transformOrder")]
    public string TransformOrder { get; set; } = string.Empty;

    [BsonElement("note")]
    public string Note { get; set; } = string.Empty;
}

// ==================== Components ====================
public class Components
{
    [BsonElement("squares")]
    public List<ComponentSquare> Squares { get; set; } = new();
}

public class ComponentSquare
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("center")]
    public Vector3 Center { get; set; } = new();

    [BsonElement("elements")]
    public ComponentElements Elements { get; set; } = new();

    [BsonElement("connections")]
    public string Connections { get; set; } = string.Empty;

    [BsonElement("bounds")]
    [BsonIgnoreIfNull]
    public Bounds? Bounds { get; set; }

    [BsonElement("state")]
    public string State { get; set; } = string.Empty;

    [BsonElement("componentMatrix")]
    public Matrix ComponentMatrix { get; set; } = new();
}

public class ComponentElements
{
    [BsonElement("straws")]
    public List<string> Straws { get; set; } = new();

    [BsonElement("connectors")]
    public List<string> Connectors { get; set; } = new();
}

// ==================== Assembly Group ====================
public class AssemblyGroup
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("description")]
    public string Description { get; set; } = string.Empty;

    [BsonElement("components")]
    public List<string> Components { get; set; } = new();

    [BsonElement("subAssemblies")]
    public List<string> SubAssemblies { get; set; } = new();

    [BsonElement("assemblyMatrix")]
    public Matrix AssemblyMatrix { get; set; } = new();

    [BsonElement("state")]
    public string State { get; set; } = string.Empty;
}

// ==================== Instances ====================
public class Instances
{
    [BsonElement("straws")]
    public List<InstanceGroup> Straws { get; set; } = new();

    [BsonElement("connectors")]
    public List<InstanceGroup> Connectors { get; set; } = new();
}

public class InstanceGroup
{
    [BsonElement("templateId")]
    public string TemplateId { get; set; } = string.Empty;

    [BsonElement("instances")]
    public List<Instance> Instances { get; set; } = new();
}

public class Instance
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("transform")]
    public Transform Transform { get; set; } = new();

    [BsonElement("arms")]
    [BsonIgnoreIfNull]
    public Dictionary<string, Vector3>? Arms { get; set; }
}

// ==================== Connection ====================
public class Connection
{
    [BsonElement("strawId")]
    public string StrawId { get; set; } = string.Empty;

    [BsonElement("endpoint")]
    public string Endpoint { get; set; } = string.Empty; // start, end

    [BsonElement("connectorId")]
    public string ConnectorId { get; set; } = string.Empty;

    [BsonElement("port")]
    public int Port { get; set; }
}
