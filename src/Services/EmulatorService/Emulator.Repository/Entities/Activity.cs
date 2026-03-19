using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Text.Json.Serialization;
using Emulator.Repository.Converters;

namespace Emulator.Repository.Entities;

/// <summary>
/// Learning activity/lab with steps and validation
/// </summary>
public class Activity
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("name")]
    public string Name { get; set; } = string.Empty;

    [BsonElement("description")]
    [BsonIgnoreIfNull]
    public string? Description { get; set; }

    [BsonElement("difficulty")]
    public string Difficulty { get; set; } = "beginner";

    [BsonElement("estimatedTime")]
    public int EstimatedTime { get; set; }

    [BsonElement("objectives")]
    public List<string> Objectives { get; set; } = new();

    [BsonElement("steps")]
    public List<ActivityStep> Steps { get; set; } = new();

    [BsonElement("playbackControls")]
    [BsonIgnoreIfNull]
    public PlaybackControls? PlaybackControls { get; set; }
}

// ==================== Activity Step ====================
public class ActivityStep
{
    [BsonElement("actionId")]
    public string ActionId { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("description")]
    [BsonIgnoreIfNull]
    public string? Description { get; set; }

    [BsonElement("expectedResult")]
    [BsonIgnoreIfNull]
    public string? ExpectedResult { get; set; }

    [BsonElement("hints")]
    [BsonIgnoreIfNull]
    public List<Hint>? Hints { get; set; } // List of Hint objects

    [BsonElement("validation")]
    [BsonIgnoreIfNull]
    public StepValidation? Validation { get; set; }
}

// ==================== Hint ====================
[JsonConverter(typeof(Converters.HintJsonConverter))]
public class Hint
{
    [BsonElement("level")]
    public int Level { get; set; }

    [BsonElement("text")]
    public string Text { get; set; } = string.Empty;

    [BsonElement("visual")]
    [BsonIgnoreIfNull]
    public string? Visual { get; set; }

    [BsonElement("commonMistake")]
    [BsonIgnoreIfNull]
    public string? CommonMistake { get; set; }

    [BsonElement("tip")]
    [BsonIgnoreIfNull]
    public string? Tip { get; set; }
}

// ==================== Step Validation ====================
public class StepValidation
{
    [BsonElement("type")]
    public string Type { get; set; } = string.Empty;

    [BsonElement("criteria")]
    public BsonDocument Criteria { get; set; } = new();

    [BsonElement("onValidationFail")]
    [BsonIgnoreIfNull]
    public ValidationFailureAction? OnValidationFail { get; set; }
}

public class ValidationFailureAction
{
    [BsonElement("showHint")]
    public bool ShowHint { get; set; }

    [BsonElement("hintLevel")]
    public int HintLevel { get; set; }

    [BsonElement("allowRetry")]
    public bool AllowRetry { get; set; }

    [BsonElement("autoCorrect")]
    public bool AutoCorrect { get; set; }

    [BsonElement("errorMessage")]
    public string ErrorMessage { get; set; } = string.Empty;

    [BsonElement("suggestedAction")]
    [BsonIgnoreIfNull]
    public string? SuggestedAction { get; set; }

    [BsonElement("autoCorrectAction")]
    [BsonIgnoreIfNull]
    public string? AutoCorrectAction { get; set; }
}

// ==================== Playback Controls ====================
public class PlaybackControls
{
    [BsonElement("allowRewind")]
    public bool AllowRewind { get; set; }

    [BsonElement("allowPause")]
    public bool AllowPause { get; set; }

    [BsonElement("allowSkip")]
    public bool AllowSkip { get; set; }

    [BsonElement("speed")]
    public double Speed { get; set; } = 1.0;
}