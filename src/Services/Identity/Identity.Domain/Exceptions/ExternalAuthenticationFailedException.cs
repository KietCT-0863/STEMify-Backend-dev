using Identity.Domain.Enums;
using Shared.Exceptions;

namespace Identity.Domain.Exceptions;

/// <summary>
/// Exception thrown when external authentication fails
/// </summary>
public class ExternalAuthenticationFailedException : DomainException
{
    public ExternalAuthenticationFailedException(ExternalAuthProvider provider, string reason)
        : base(
            $"External authentication with {provider} failed: {reason}",
            "EXTERNAL_AUTH_FAILED"
        )
    { }

    public ExternalAuthenticationFailedException(string provider, string reason)
        : base(
            $"External authentication with {provider} failed: {reason}",
            "EXTERNAL_AUTH_FAILED"
        )
    { }
}
