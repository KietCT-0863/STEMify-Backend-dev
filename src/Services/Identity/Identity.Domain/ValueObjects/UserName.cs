using System.Text.RegularExpressions;

namespace Identity.Domain.ValueObjects;

public record UserName
{
    private static readonly Regex UserNameRegex = new(@"^[a-zA-Z0-9._-]+$", RegexOptions.Compiled);

    public string Value { get; }

    private UserName(string value)
    {
        Value = value;
    }

    public static UserName Create(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            throw new ArgumentException("Username cannot be empty", nameof(userName));

        userName = userName.Trim();

        if (userName.Length < 3)
            throw new ArgumentException("Username must be at least 3 characters", nameof(userName));

        if (userName.Length > 50)
            throw new ArgumentException(
                "Username cannot be more than 50 characters",
                nameof(userName)
            );

        if (!UserNameRegex.IsMatch(userName))
            throw new ArgumentException(
                "Username can only contain letters, numbers, dots, underscores and hyphens",
                nameof(userName)
            );

        if (userName.StartsWith('.') || userName.EndsWith('.'))
            throw new ArgumentException(
                "Username cannot start or end with a dot",
                nameof(userName)
            );

        if (userName.Contains(".."))
            throw new ArgumentException(
                "Username cannot contain two consecutive dots",
                nameof(userName)
            );

        return new UserName(userName);
    }

    public static implicit operator string(UserName userName) => userName.Value;

    public override string ToString() => Value;
}
