using MediatR;

namespace Identity.Application.Auth.Commands.ProcessAuthorization;

/// <summary>
/// Command to process OAuth authorization
/// </summary>
public class ProcessAuthorizationCommand : IRequest<AuthorizationResult>
{
    public bool IsAuthenticated { get; init; }
    public string? UserId { get; init; }
    public DateTime? AuthenticationTime { get; init; }
    public string RequestPath { get; init; } = string.Empty;
    public string QueryString { get; init; } = string.Empty;
    public string? ClientId { get; init; }
    public string? RedirectUri { get; init; }
    public string? ResponseType { get; init; }
    public string? Scope { get; init; }
    public string? State { get; init; }
    public string? CodeChallenge { get; init; }
    public string? CodeChallengeMethod { get; init; }
    public string? Prompt { get; init; }
    public int? MaxAge { get; init; }
}

/// <summary>
/// Result of authorization processing
/// </summary>
public class AuthorizationResult
{
    public bool Success { get; set; }
    public AuthorizationAction Action { get; set; }
    public string? RedirectUrl { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorDescription { get; set; }
    public string? AuthorizationCode { get; set; }
    public string? AccessToken { get; set; }
    public string? IdToken { get; set; }
    public int? ExpiresIn { get; set; }
    public System.Security.Claims.ClaimsPrincipal? Principal { get; set; }

    public static AuthorizationResult Challenge(string authenticationScheme, string redirectUrl)
    {
        return new AuthorizationResult
        {
            Success = false,
            Action = AuthorizationAction.Challenge,
            RedirectUrl = redirectUrl,
        };
    }

    public static AuthorizationResult ForbidConsentRequired()
    {
        return new AuthorizationResult
        {
            Success = false,
            Action = AuthorizationAction.Forbid,
            ErrorCode = "consent_required",
            ErrorDescription = "User consent is required.",
        };
    }

    public static AuthorizationResult SignIn(System.Security.Claims.ClaimsPrincipal principal)
    {
        return new AuthorizationResult
        {
            Success = true,
            Action = AuthorizationAction.Success,
            Principal = principal,
        };
    }

    public static AuthorizationResult ConsentForm(string appName, string scope)
    {
        return new AuthorizationResult
        {
            Success = false,
            Action = AuthorizationAction.Redirect,
            RedirectUrl = $"/consent?application={appName}&scope={scope}",
        };
    }

    public static AuthorizationResult Redirect(string redirectUrl)
    {
        return new AuthorizationResult
        {
            Success = false,
            Action = AuthorizationAction.Redirect,
            RedirectUrl = redirectUrl,
        };
    }
}

/// <summary>
/// Authorization action types
/// </summary>
public enum AuthorizationAction
{
    Challenge,
    Forbid,
    Error,
    Redirect,
    Success,
}
