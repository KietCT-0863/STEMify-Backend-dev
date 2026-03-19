using Identity.Application.Common.Models.Auth;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Authentication.Queries.GetExternalProviders;

/// <summary>
/// Handler for GetExternalProvidersQuery
/// </summary>
public class GetExternalProvidersQueryHandler
    : IRequestHandler<GetExternalProvidersQuery, List<ExternalProviderDto>>
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<GetExternalProvidersQueryHandler> _logger;

    public GetExternalProvidersQueryHandler(
        UserManager<ApplicationUser> userManager,
        ILogger<GetExternalProvidersQueryHandler> logger
    )
    {
        _userManager = userManager;
        _logger = logger;
    }

    public async Task<List<ExternalProviderDto>> Handle(
        GetExternalProvidersQuery request,
        CancellationToken cancellationToken
    )
    {
        // Find the user
        var user = await _userManager.FindByIdAsync(request.UserId.ToString());
        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found", request.UserId);
            return new List<ExternalProviderDto>();
        }

        // Get all external logins for this user
        var logins = await _userManager.GetLoginsAsync(user);

        // Map to DTOs
        var externalProviders = logins
            .Select(login => new ExternalProviderDto
            {
                ProviderName = login.LoginProvider,
                ProviderType = MapProviderNameToEnum(login.LoginProvider),
                ProviderDisplayName = login.ProviderDisplayName ?? login.LoginProvider,
                LinkedAt = DateTime.UtcNow, // ASP.NET Identity doesn't track when login was added
                Email = user.Email
            })
            .ToList();

        _logger.LogInformation(
            "Found {Count} external providers for user {UserId}",
            externalProviders.Count,
            request.UserId
        );

        return externalProviders;
    }

    /// <summary>
    /// Maps provider name string to ExternalAuthProvider enum
    /// </summary>
    private static ExternalAuthProvider MapProviderNameToEnum(string providerName)
    {
        return providerName.ToLowerInvariant() switch
        {
            "google" => ExternalAuthProvider.Google,
            _ => ExternalAuthProvider.None
        };
    }
}
