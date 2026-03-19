namespace Identity.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when password validation fails
/// </summary>
public class InvalidPasswordException : ValidationException
{
    public InvalidPasswordException(string message = "Current password is incorrect")
        : base(new[] { new FluentValidation.Results.ValidationFailure("password", message) }) { }
}
