using MongoDB.Bson.Serialization.Attributes;

namespace Emulator.Repository.Entities;

/// <summary>
/// Action definition for animations and transformations
/// </summary>
public class ActionDefinition
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("type")]
    public string Type { get; set; } = string.Empty;

    [BsonElement("targets")]
    [BsonIgnoreIfNull]
    public List<string>? Targets { get; set; } // List of target IDs

    [BsonElement("connectionGroup")]
    [BsonIgnoreIfNull]
    public string? ConnectionGroup { get; set; }

    [BsonElement("assemblyId")]
    [BsonIgnoreIfNull]
    public string? AssemblyId { get; set; }

    [BsonElement("duration")]
    public double Duration { get; set; }

    [BsonElement("sequenceDelay")]
    [BsonIgnoreIfNull]
    public double? SequenceDelay { get; set; }

    [BsonElement("instantAppear")]
    [BsonIgnoreIfNull]
    public bool? InstantAppear { get; set; }

    [BsonElement("showRealtimeControls")]
    [BsonIgnoreIfNull]
    public bool? ShowRealtimeControls { get; set; }

    [BsonElement("animation")]
    [BsonIgnoreIfNull]
    public AnimationDefinition? Animation { get; set; }

    [BsonElement("componentTransforms")]
    [BsonIgnoreIfNull]
    public Dictionary<string, ComponentTransform>? ComponentTransforms { get; set; }

    [BsonElement("connectorArmTransforms")]
    [BsonIgnoreIfNull]
    public Dictionary<string, Dictionary<string, Vector3>>? ConnectorArmTransforms { get; set; }

    [BsonElement("interpolation")]
    [BsonIgnoreIfNull]
    public string? Interpolation { get; set; }

    [BsonElement("assemblyState")]
    [BsonIgnoreIfNull]
    public string? AssemblyState { get; set; }

    [BsonElement("description")]
    [BsonIgnoreIfNull]
    public string? Description { get; set; }

    [BsonElement("rotationSpeed")]
    [BsonIgnoreIfNull]
    public double? RotationSpeed { get; set; }

    [BsonElement("_comment")]
    [BsonIgnoreIfNull]
    public string? Comment { get; set; }
}

// ==================== Animation ====================
public class AnimationDefinition
{
    [BsonElement("colorHighlight")]
    [BsonIgnoreIfNull]
    public string? ColorHighlight { get; set; }

    [BsonElement("pulseEffect")]
    [BsonIgnoreIfNull]
    public bool? PulseEffect { get; set; }

    [BsonElement("curve")]
    [BsonIgnoreIfNull]
    public string? Curve { get; set; }

    [BsonElement("keyframes")]
    [BsonIgnoreIfNull]
    public List<Keyframe>? Keyframes { get; set; }

    [BsonElement("strawAnimation")]
    [BsonIgnoreIfNull]
    public string? StrawAnimation { get; set; }

    [BsonElement("connectorAnimation")]
    [BsonIgnoreIfNull]
    public string? ConnectorAnimation { get; set; }

    [BsonElement("rotationInterpolation")]
    [BsonIgnoreIfNull]
    public string? RotationInterpolation { get; set; }

    [BsonElement("positionInterpolation")]
    [BsonIgnoreIfNull]
    public string? PositionInterpolation { get; set; }

    [BsonElement("simultaneousTransform")]
    [BsonIgnoreIfNull]
    public bool? SimultaneousTransform { get; set; }
}

public class Keyframe
{
    [BsonElement("time")]
    public double Time { get; set; }

    [BsonElement("opacity")]
    [BsonIgnoreIfNull]
    public double? Opacity { get; set; }

    [BsonElement("scale")]
    [BsonIgnoreIfNull]
    public double? Scale { get; set; }
}

// ==================== Component Transform ====================
public class ComponentTransform
{
    [BsonElement("type")]
    public string Type { get; set; } = string.Empty;

    [BsonElement("matrix")]
    public Matrix Matrix { get; set; } = new();

    [BsonElement("pivot")]
    public string Pivot { get; set; } = string.Empty;

    [BsonElement("transformAsUnit")]
    public bool TransformAsUnit { get; set; }

    [BsonElement("constraints")]
    [BsonIgnoreIfNull]
    public TransformConstraints? Constraints { get; set; }
}

public class TransformConstraints
{
    [BsonElement("maintainRelativePositions")]
    public bool MaintainRelativePositions { get; set; }

    [BsonElement("preventBreaking")]
    public bool PreventBreaking { get; set; }
}