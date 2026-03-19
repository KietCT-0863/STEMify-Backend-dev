using System.Text.Json;

namespace Emulator.API.Utilities
{
    public static class JsonElementConverter
    {
        /// <summary>
        /// Reusable JsonSerializerOptions for all gRPC services
        /// Cached to avoid creating new instances per request (CA1869)
        /// </summary>
        public static readonly JsonSerializerOptions DefaultOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase
        };
        /// <summary>
        /// Converts a Dictionary containing JsonElement values to a Dictionary with primitive values
        /// </summary>
        public static Dictionary<string, object> ConvertJsonElementsToPrimitives(Dictionary<string, object> input)
        {
            var result = new Dictionary<string, object>();

            foreach (var kvp in input)
            {
                result[kvp.Key] = ConvertValue(kvp.Value);
            }

            return result;
        }

        /// <summary>
        /// Recursively converts JsonElement or JsonElement-containing objects to primitives
        /// </summary>
        private static object ConvertValue(object value)
        {
            return value switch
            {
                JsonElement element => ConvertJsonElement(element),
                Dictionary<string, object> dict => ConvertJsonElementsToPrimitives(dict),
                List<object> list => list.Select(ConvertValue).ToList(),
                _ => value
            };
        }

        /// <summary>
        /// Converts a JsonElement to its appropriate primitive type
        /// </summary>
        private static object ConvertJsonElement(JsonElement element)
        {
            return element.ValueKind switch
            {
                JsonValueKind.String => element.GetString() ?? string.Empty,
                JsonValueKind.Number => element.TryGetInt32(out var intValue) ? intValue :
                                       element.TryGetInt64(out var longValue) ? longValue :
                                       element.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null!,
                JsonValueKind.Array => element.EnumerateArray()
                    .Select(e => ConvertJsonElement(e))
                    .ToList(),
                JsonValueKind.Object => element.EnumerateObject()
                    .ToDictionary(
                        prop => prop.Name,
                        prop => ConvertJsonElement(prop.Value)),
                _ => element.ToString() ?? string.Empty
            };
        }
    }
}
