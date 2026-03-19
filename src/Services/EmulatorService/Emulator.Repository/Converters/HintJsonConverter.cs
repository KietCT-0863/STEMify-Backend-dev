using System.Text.Json;
using System.Text.Json.Serialization;
using Emulator.Repository.Entities;

namespace Emulator.Repository.Converters;

public class HintJsonConverter : JsonConverter<Hint>
{
    public override Hint Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            var text = reader.GetString() ?? string.Empty;
            return new Hint { Text = text, Level = 0 };
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            var hint = new Hint
            {
                Level = root.TryGetProperty("level", out var levelProp) && levelProp.TryGetInt32(out var level)
                    ? level
                    : 0,
                Text = root.TryGetProperty("text", out var textProp) ? textProp.GetString() ?? string.Empty : string.Empty,
                Visual = root.TryGetProperty("visual", out var visualProp) ? visualProp.GetString() : null,
                CommonMistake = root.TryGetProperty("commonMistake", out var mistakeProp) ? mistakeProp.GetString() : null,
                Tip = root.TryGetProperty("tip", out var tipProp) ? tipProp.GetString() : null
            };

            return hint;
        }

        throw new JsonException("Invalid Hint payload. Expected string or object.");
    }

    public override void Write(Utf8JsonWriter writer, Hint value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("level", value.Level);
        writer.WriteString("text", value.Text);
        if (!string.IsNullOrWhiteSpace(value.Visual))
        {
            writer.WriteString("visual", value.Visual);
        }
        if (!string.IsNullOrWhiteSpace(value.CommonMistake))
        {
            writer.WriteString("commonMistake", value.CommonMistake);
        }
        if (!string.IsNullOrWhiteSpace(value.Tip))
        {
            writer.WriteString("tip", value.Tip);
        }
        writer.WriteEndObject();
    }
}

