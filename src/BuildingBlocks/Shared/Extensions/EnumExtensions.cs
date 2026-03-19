namespace Shared.Extensions
{
    public static class EnumExtensions
    {
        public static TEnum? ToEnumOrNull<TEnum>(this string? value, bool ignoreCase = true)
            where TEnum : struct, Enum
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return Enum.TryParse<TEnum>(value, ignoreCase, out var result)
                ? result
                : (TEnum?)null;
        }
    }
}
