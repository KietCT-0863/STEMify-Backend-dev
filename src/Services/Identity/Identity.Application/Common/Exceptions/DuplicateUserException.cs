namespace Identity.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when attempting to create a user that already exists
/// </summary>
public class DuplicateUserException : ValidationException
{
    public DuplicateUserException(string field, string value)
        : base(
            new[]
            {
                new FluentValidation.Results.ValidationFailure(
                    field,
                    $"{field} '{value}' đã tồn tại. Vui lòng đăng nhập."
                ),
            }
        ) { }
}
