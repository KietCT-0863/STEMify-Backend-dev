using System.Security.Cryptography;

namespace Identity.Domain.ValueObjects;

/// <summary>
/// Value object representing a secure invitation token
/// </summary>
public record InvitationToken
{
    private const int TokenByteSize = 32; // 256 bits
    private const int TokenLength = 43; // Base64 URL-safe encoding result length

    public string Value { get; }

    private InvitationToken(string value)
    {
        Value = value;
    }

    public static InvitationToken Generate()
    {
        var randomBytes = RandomNumberGenerator.GetBytes(TokenByteSize);
        var token = Convert.ToBase64String(randomBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");

        return new InvitationToken(token);
    }

    /// <summary>
    /// Creates an invitation token from an existing string value
    /// Used when reading from database or validating user input
    /// </summary>
    public static InvitationToken FromString(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Invitation token cannot be empty", nameof(token));

        token = token.Trim();

        if (token.Length != TokenLength)
            throw new ArgumentException(
                $"Invalid invitation token length. Expected {TokenLength} characters",
                nameof(token)
            );

        // Validate token only contains valid Base64 URL-safe characters
        if (!token.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'))
            throw new ArgumentException(
                "Invitation token contains invalid characters",
                nameof(token)
            );

        return new InvitationToken(token);
    }

    public static implicit operator string(InvitationToken token) => token.Value;

    public override string ToString() => Value;
}