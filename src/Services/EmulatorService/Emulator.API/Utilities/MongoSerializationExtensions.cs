using System.Text.Json;
using System.Text.Json.Serialization;
using MongoDB.Bson;
using MongoDB.Bson.IO;
using MongoDB.Bson.Serialization;

namespace Emulator.API.Utilities
{
    public static class MongoSerializationExtensions
    {
        /// <summary>
        /// Converts MongoDB BsonDocument to JSON string with _id -> id mapping for frontend compatibility
        /// </summary>
        public static string ToFrontendJson(this BsonDocument document, JsonSerializerOptions? options = null)
        {
            if (document == null) return "{}";

            var json = document.ToJson(new JsonWriterSettings
            {
                OutputMode = JsonOutputMode.RelaxedExtendedJson
            });

            using var jsonDoc = JsonDocument.Parse(json);
            var converted = ConvertDocument(jsonDoc.RootElement);
            
            return JsonSerializer.Serialize(converted, options ?? JsonElementConverter.DefaultOptions);
        }

        /// <summary>
        /// Converts MongoDB BsonDocument to Dictionary with _id -> id mapping
        /// </summary>
        public static Dictionary<string, object> ToFrontendDictionary(this BsonDocument document)
        {
            if (document == null) return new Dictionary<string, object>();

            var json = document.ToJson(new JsonWriterSettings
            {
                OutputMode = JsonOutputMode.RelaxedExtendedJson
            });

            using var jsonDoc = JsonDocument.Parse(json);
            return ConvertDocumentToDictionary(jsonDoc.RootElement);
        }

        /// <summary>
        /// Converts any object to JSON with _id -> id mapping for frontend compatibility
        /// </summary>
        public static string ToFrontendJson(this object obj, JsonSerializerOptions? options = null)
        {
            if (obj == null) return "{}";

            // Prepare System.Text.Json options with BSON support
            var effectiveOptions = CreateEffectiveOptions(options ?? JsonElementConverter.DefaultOptions);

            // First serialize to JSON using System.Text.Json with BSON converters
            var json = JsonSerializer.Serialize(obj, effectiveOptions);
            
            // Parse and convert _id to id
            using var jsonDoc = JsonDocument.Parse(json);
            var converted = ConvertDocument(jsonDoc.RootElement);
            
            return JsonSerializer.Serialize(converted, options ?? JsonElementConverter.DefaultOptions);
        }

        private static JsonSerializerOptions CreateEffectiveOptions(JsonSerializerOptions baseOptions)
        {
            var opts = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = baseOptions.PropertyNameCaseInsensitive,
                ReadCommentHandling = baseOptions.ReadCommentHandling,
                AllowTrailingCommas = baseOptions.AllowTrailingCommas,
                PropertyNamingPolicy = baseOptions.PropertyNamingPolicy,
                DictionaryKeyPolicy = baseOptions.DictionaryKeyPolicy
            };

            // Ensure we can serialize MongoDB BSON documents cleanly
            opts.Converters.Add(new BsonDocumentJsonConverter());

            return opts;
        }

        private sealed class BsonDocumentJsonConverter : JsonConverter<BsonDocument>
        {
            public override BsonDocument Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                throw new NotSupportedException("Deserialization of BsonDocument is not supported in this context.");
            }

            public override void Write(Utf8JsonWriter writer, BsonDocument value, JsonSerializerOptions options)
            {
                if (value == null)
                {
                    writer.WriteNullValue();
                    return;
                }

                var json = value.ToJson(new JsonWriterSettings
                {
                    OutputMode = JsonOutputMode.RelaxedExtendedJson
                });

                using var jsonDoc = JsonDocument.Parse(json);
                jsonDoc.RootElement.WriteTo(writer);
            }
        }

        private static object ConvertDocument(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.Object => ConvertDocumentToDictionary(element),
                JsonValueKind.Array => element.EnumerateArray().Select(ConvertDocument).ToList(),
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue :
                                       element.TryGetInt64(out var longValue) ? longValue :
                                       element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null!,
                _ => element.ToString() ?? string.Empty
            };
        }

        private static Dictionary<string, object> ConvertDocumentToDictionary(JsonElement element)
        {
            var dict = new Dictionary<string, object>();
            
            foreach (var property in element.EnumerateObject())
            {
                var key = property.Name == "_id" ? "id" : property.Name;
                dict[key] = ConvertDocument(property.Value);
            }
            
            return dict;
        }
    }
}
