using Identity.Application.Common.Models.Auth;
using Identity.Domain.Enums;
using MediatR;

namespace Identity.Application.Authentication.Commands.ProcessExternalLogin;

/// <summary>
/// Command to process external authentication (Google)
/// Creates new user if not exists, or logs in existing user
/// </summary>
public record ProcessExternalLoginCommand : IRequest<ExternalAuthenticationResultDto>
{
    public ExternalLoginInfoDto ExternalLoginInfo { get; init; } = null!;

    
    public UserRole DefaultUserRole { get; init; } = UserRole.Member;

    public string? ReturnUrl { get; init; }
}
