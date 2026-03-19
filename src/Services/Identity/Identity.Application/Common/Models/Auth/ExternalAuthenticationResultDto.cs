namespace Identity.Application.Common.Models.Auth;

/// <summary>
/// DTO representing the result of external authentication process
/// </summary>
public class ExternalAuthenticationResultDto
{
    public bool Succeeded { get; set; }

    public bool IsNewUser { get; set; }

    public Guid? UserId { get; set; }

    public string? Email { get; set; }

    public string? FullName { get; set; }

    public string? ErrorMessage { get; set; }

    public string? ErrorCode { get; set; }

    public bool RequiresProfileCompletion { get; set; }

    public static ExternalAuthenticationResultDto Success(
        Guid userId,
        string email,
        string fullName,
        bool isNewUser,
        bool requiresProfileCompletion = false
    )
    {
        return new ExternalAuthenticationResultDto
        {
            Succeeded = true,
            IsNewUser = isNewUser,
            UserId = userId,
            Email = email,
            FullName = fullName,
            RequiresProfileCompletion = requiresProfileCompletion
        };
    }

    public static ExternalAuthenticationResultDto Failure(string errorMessage, string errorCode)
    {
        return new ExternalAuthenticationResultDto
        {
            Succeeded = false,
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
    }
}
