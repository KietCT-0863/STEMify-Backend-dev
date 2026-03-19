using System.Text.RegularExpressions;

namespace Identity.Domain.ValueObjects;

public record FullName
{
    private static readonly Regex NameRegex = new(
        @"^[\p{L}\s'-]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public string Value { get; }

    private FullName(string value)
    {
        Value = value;
    }

    public static FullName Create(string fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be empty", nameof(fullName));

        fullName = fullName.Trim();

        // Normalize spaces
        fullName = Regex.Replace(fullName, @"\s+", " ");

        if (fullName.Length < 2)
            throw new ArgumentException(
                "Full name must be at least 2 characters",
                nameof(fullName)
            );

        if (fullName.Length > 100)
            throw new ArgumentException(
                "Full name cannot be more than 100 characters",
                nameof(fullName)
            );

        if (!NameRegex.IsMatch(fullName))
            throw new ArgumentException(
                "Full name can only contain letters, spaces, quotes and hyphens",
                nameof(fullName)
            );

        return new FullName(fullName);
    }

    public static implicit operator string(FullName fullName) => fullName.Value;

    public override string ToString() => Value;
}
