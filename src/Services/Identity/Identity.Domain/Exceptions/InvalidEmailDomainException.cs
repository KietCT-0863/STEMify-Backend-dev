using Shared.Exceptions;

namespace Identity.Domain.Exceptions;

/// <summary>
/// Exception thrown when an email domain is invalid or not allowed
/// </summary>
public class InvalidEmailDomainException : DomainException
{
    public InvalidEmailDomainException(string email, string allowedDomain)
        : base(
            $"Email '{email}' does not belong to the allowed domain '{allowedDomain}'",
            "INVALID_EMAIL_DOMAIN"
        )
    {
    }

    public InvalidEmailDomainException(string domain)
        : base(
            $"The email domain '{domain}' is not valid or not allowed",
            "INVALID_EMAIL_DOMAIN"
        )
    {
    }
}
