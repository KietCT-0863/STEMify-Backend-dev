using Identity.Application.Auth.Commands.ProcessTokenExchange;
using Identity.Application.Common.Interfaces;
using Identity.Domain.Entities;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.Infrastructure.Services;

/// <summary>
/// Infrastructure implementation of OAuth2 token exchange
/// Contains all OpenIddict-specific logic
/// </summary>
public class TokenExchangeService : ITokenExchangeService
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly IOpenIddictTokenManager _tokenManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public TokenExchangeService(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictScopeManager scopeManager,
        IOpenIddictTokenManager tokenManager,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IHttpContextAccessor httpContextAccessor
    )
    {
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _scopeManager = scopeManager;
        _tokenManager = tokenManager;
        _userManager = userManager;
        _signInManager = signInManager;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<TokenExchangeResult> ExchangeTokenAsync(
        ProcessTokenExchangeCommand request,
        CancellationToken cancellationToken
    )
    {
        var context =
            _httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HTTP context is not available.");

        var oidcRequest =
            context.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("OpenIddict request cannot be retrieved.");

        try
        {
            return request.GrantType switch
            {
                GrantTypes.AuthorizationCode => await HandleAuthorizationCodeAsync(
                    oidcRequest,
                    request
                ),
                GrantTypes.RefreshToken => await HandleRefreshTokenAsync(oidcRequest, request),
                GrantTypes.Password => await HandlePasswordAsync(oidcRequest, request),
                GrantTypes.ClientCredentials => await HandleClientCredentialsAsync(
                    oidcRequest,
                    request
                ),
                _ => new TokenExchangeResult
                {
                    Success = false,
                    ErrorCode = Errors.UnsupportedGrantType,
                    ErrorDescription = $"Grant type '{request.GrantType}' is not supported.",
                },
            };
        }
        catch (Exception ex)
        {
            return new TokenExchangeResult
            {
                Success = false,
                ErrorCode = Errors.ServerError,
                ErrorDescription = ex.Message,
            };
        }
    }

    private async Task<TokenExchangeResult> HandleAuthorizationCodeAsync(
        OpenIddictRequest request,
        ProcessTokenExchangeCommand command
    )
    {
        // Note: This method will not be called in our new architecture
        // Token exchange is handled directly by AuthorizationController with OpenIddict middleware
        // This is kept for completeness but should not be used

        return new TokenExchangeResult
        {
            Success = false,
            ErrorCode = Errors.ServerError,
            ErrorDescription =
                "Authorization code flow should be handled by OpenIddict middleware directly.",
        };
    }

    private async Task<TokenExchangeResult> HandleRefreshTokenAsync(
        OpenIddictRequest request,
        ProcessTokenExchangeCommand command
    )
    {
        return new TokenExchangeResult
        {
            Success = false,
            ErrorCode = Errors.ServerError,
            ErrorDescription =
                "Refresh token flow should be handled by OpenIddict middleware directly.",
        };
    }

    private async Task<TokenExchangeResult> HandlePasswordAsync(
        OpenIddictRequest request,
        ProcessTokenExchangeCommand command
    )
    {
        // Resource Owner Password Credentials Grant
        var user =
            await _userManager.FindByNameAsync(command.Username!)
            ?? await _userManager.FindByEmailAsync(command.Username!);

        if (user == null || !await _userManager.CheckPasswordAsync(user, command.Password!))
        {
            return new TokenExchangeResult
            {
                Success = false,
                ErrorCode = Errors.InvalidGrant,
                ErrorDescription = "Invalid username or password.",
            };
        }

        return new TokenExchangeResult
        {
            Success = true,
            AccessToken = "handled_by_openiddict_middleware",
            TokenType = "Bearer",
            ExpiresIn = 864000,
            Scope = command.Scope,
        };
    }

    private async Task<TokenExchangeResult> HandleClientCredentialsAsync(
        OpenIddictRequest request,
        ProcessTokenExchangeCommand command
    )
    {
        // Client Credentials Grant - for machine-to-machine
        var application = await _applicationManager.FindByClientIdAsync(command.ClientId!);
        if (application == null)
        {
            return new TokenExchangeResult
            {
                Success = false,
                ErrorCode = Errors.InvalidClient,
                ErrorDescription = "Client application not found.",
            };
        }

        return new TokenExchangeResult
        {
            Success = true,
            AccessToken = "handled_by_openiddict_middleware",
            TokenType = "Bearer",
            ExpiresIn = 864000,
            Scope = command.Scope,
        };
    }
}
