using Shared.Exceptions;

namespace Identity.Domain.Exceptions;

/// <summary>
/// Exception thrown when trying to link an external login that is already linked to another account
/// </summary>
public class ExternalLoginAlreadyLinkedException : DomainException
{
    public ExternalLoginAlreadyLinkedException(string provider, string providerKey)
        : base(
            $"External login from {provider} (key: {providerKey}) is already linked to another account",
            "EXTERNAL_LOGIN_ALREADY_LINKED"
        )
    { }

    public ExternalLoginAlreadyLinkedException(string provider)
        : base(
            $"An external login from {provider} is already linked to this account",
            "EXTERNAL_LOGIN_ALREADY_LINKED"
        )
    { }
}
