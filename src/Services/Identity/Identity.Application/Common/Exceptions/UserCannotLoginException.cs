using Identity.Domain.Enums;

namespace Identity.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when a user cannot login due to status or email confirmation issues
/// </summary>
public class UserCannotLoginException : Exception
{
    public UserCannotLoginException(UserStatus status, bool emailConfirmed)
        : base($"User cannot login. Status: {status}, Email confirmed: {emailConfirmed}") { }
}
