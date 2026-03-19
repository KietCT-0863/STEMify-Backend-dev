using Shared.Exceptions;

namespace Identity.Domain.Exceptions;

/// <summary>
/// Exception thrown when an external authentication provider is not supported
/// </summary>
public class ExternalProviderNotSupportedException : DomainException
{
    public ExternalProviderNotSupportedException(string provider)
        : base(
            $"External authentication provider '{provider}' is not supported",
            "EXTERNAL_PROVIDER_NOT_SUPPORTED"
        )
    { }
}
