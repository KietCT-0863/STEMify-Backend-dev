using System.Text.RegularExpressions;

namespace Identity.Domain.ValueObjects;

/// <summary>
/// Value object representing an email domain (e.g., "gmail.com", "university.edu")
/// Used for organization domain verification
/// </summary>
public record EmailDomain
{
    private static readonly Regex DomainRegex = new(
        @"^[a-zA-Z0-9][a-zA-Z0-9-]{0,61}[a-zA-Z0-9]?(\.[a-zA-Z0-9][a-zA-Z0-9-]{0,61}[a-zA-Z0-9]?)*\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase
    );

    public string Value { get; }

    private EmailDomain(string value)
    {
        Value = value;
    }

    public static EmailDomain Create(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            throw new ArgumentException("Email domain cannot be empty", nameof(domain));

        domain = domain.Trim().ToLowerInvariant();

        if (!DomainRegex.IsMatch(domain))
            throw new ArgumentException(
                $"Invalid email domain format: {domain}",
                nameof(domain)
            );

        if (domain.Length > 255)
            throw new ArgumentException(
                "Email domain is too long (maximum 255 characters)",
                nameof(domain)
            );

        return new EmailDomain(domain);
    }

    /// <summary>
    /// Checks if an email address belongs to this domain
    /// </summary>
    public bool Matches(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        var parts = email.Split('@');
        if (parts.Length != 2)
            return false;

        var emailDomain = parts[1].ToLowerInvariant();
        return emailDomain == Value;
    }

    public static implicit operator string(EmailDomain domain) => domain.Value;

    public override string ToString() => Value;
}
