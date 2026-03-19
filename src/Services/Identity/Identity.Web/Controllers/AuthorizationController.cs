using System.Collections.Immutable;
using System.IO;
using System.Security.Claims;
using System.Text.Json;
using Identity.Application.Common.Interfaces;
using Identity.Application.Services;
using Identity.Domain.Entities;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.Web.Controllers;

[ApiController]
[Route("connect")]
public class AuthorizationController : ControllerBase
{
    private readonly IAuthorizationProcessingService _authorizationService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly JwtOrganizationClaimsBuilder _organizationClaimsBuilder;
    private readonly ILogger<AuthorizationController> _logger;

    public AuthorizationController(
        IAuthorizationProcessingService authorizationService,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictScopeManager scopeManager,
        JwtOrganizationClaimsBuilder organizationClaimsBuilder,
        ILogger<AuthorizationController> logger
    )
    {
        _authorizationService = authorizationService;
        _userManager = userManager;
        _signInManager = signInManager;
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _scopeManager = scopeManager;
        _organizationClaimsBuilder = organizationClaimsBuilder;
        _logger = logger;
    }

    [HttpGet("authorize")]
    [HttpPost("authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request =
            HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException(
                "The OpenID Connect request cannot be retrieved."
            );

        var authResult = await HttpContext.AuthenticateAsync(IdentityConstants.ApplicationScheme);

        if (!authResult.Succeeded)
        {
            var provider = request.GetParameter("provider")?.ToString();
            
            if (!string.IsNullOrEmpty(provider))
            {
                var returnUrl = Request.PathBase + Request.Path + Request.QueryString.Value;
                
              
                var isExternalLoginFlow = request.GetParameter("external_login")?.ToString() == "true";
                
                if (!isExternalLoginFlow)
                {
                    var queryString = Request.QueryString.Value ?? string.Empty;
                    var separator = queryString.Contains("?") ? "&" : "?";
                    returnUrl = $"{returnUrl}{separator}external_login=true";
                }
                
                var externalLoginUrl = Url.Action(
                    "ExternalLogin",
                    "Account",
                    new { provider = provider, returnUrl = returnUrl }
                );
                
                _logger.LogInformation(
                    "User not authenticated, redirecting to external login provider: {Provider}, ReturnUrl: {ReturnUrl}",
                    provider,
                    returnUrl
                );
                
                return Redirect(externalLoginUrl!);
            }
            
            // No provider specified, redirect to regular login
            return Challenge(
                new AuthenticationProperties
                {
                    RedirectUri = Request.PathBase + Request.Path + Request.QueryString.Value,
                },
                IdentityConstants.ApplicationScheme
            );
        }

        
        var user =
            await _userManager.GetUserAsync(authResult.Principal!)
            ?? throw new InvalidOperationException("User not found.");


        var application =
            await _applicationManager.FindByClientIdAsync(request.ClientId!)
            ?? throw new InvalidOperationException(
                $"Details concerning the calling client application cannot be found."
            );

        var authorizations = new List<object>();
        var subject = await _userManager.GetUserIdAsync(user);
        var client = await _applicationManager.GetIdAsync(application);
        var requestedScopes = request.GetScopes().ToList();
        
       if (!requestedScopes.Contains(Scopes.OpenId))
        {
            requestedScopes.Add(Scopes.OpenId);
            _logger.LogInformation(
                "Added 'openid' scope to authorization request for client {ClientId} (required for ID tokens)",
                request.ClientId
            );
        }
        
        var scopes = requestedScopes.ToImmutableArray();
        
       

        await foreach (
            var authorization in _authorizationManager.FindAsync(
                subject: subject,
                client: client!,
                status: Statuses.Valid,
                type: AuthorizationTypes.Permanent,
                scopes: scopes
            )
        )
        {
            authorizations.Add(authorization);
        }

        var consentType = await _applicationManager.GetConsentTypeAsync(application);
        var consentGranted = HttpContext.Request.Query["consent_granted"].ToString() == "true";

        _logger.LogInformation(
            " Consent flow analysis - Client: {ClientId}, User: {UserId}, ConsentType: {ConsentType}, ExistingAuthorizations: {AuthCount}, ConsentGranted: {ConsentGranted}",
            request.ClientId,
            user.Id,
            consentType,
            authorizations.Count,
            consentGranted
        );

        if (consentType == ConsentTypes.Implicit)
        {
            _logger.LogInformation(
                "Implicit consent client detected for {ClientId}, automatically accepting consent for user {UserId}",
                request.ClientId,
                user.Id
            );

            if (!authorizations.Any())
            {
                try
                {
                    var authorizationDescriptor = new OpenIddictAuthorizationDescriptor
                    {
                        ApplicationId = client,
                        Subject = subject,
                        Type = AuthorizationTypes.Permanent,
                    };

                    authorizationDescriptor.Scopes.UnionWith(scopes);

                    var newAuthorization = await _authorizationManager.CreateAsync(authorizationDescriptor);
                    authorizations.Add(newAuthorization);

                    _logger.LogInformation(
                        "Automatically created authorization for implicit consent client {ClientId} and user {UserId}",
                        request.ClientId,
                        user.Id
                    );
                }
                catch (Exception ex)
                {
                    var existingAuths = new List<object>();
                    await foreach (
                        var auth in _authorizationManager.FindAsync(
                            subject: subject,
                            client: client!,
                            status: Statuses.Valid,
                            type: AuthorizationTypes.Permanent,
                            scopes: scopes
                        )
                    )
                    {
                        existingAuths.Add(auth);
                    }

                    if (existingAuths.Any())
                    {
                       
                        authorizations.Clear();
                        authorizations.AddRange(existingAuths);
                    }
                    else
                    {
                        _logger.LogError(
                            ex,
                            "Failed to create authorization for implicit consent client {ClientId} and user {UserId}",
                            request.ClientId,
                            user.Id
                        );

                        // If we can't create the authorization, fall back to explicit consent
                        return Redirect($"/Account/Consent{Request.QueryString}");
                    }
                }
            }
        }
        else if (consentType != ConsentTypes.Implicit && !authorizations.Any() && !consentGranted)
        {
            _logger.LogInformation(
                "Explicit consent required for client {ClientId}, user {UserId}. Redirecting to consent page.",
                request.ClientId,
                user.Id
            );

            if (request.GetParameter("prompt")?.ToString()?.Contains("none") == true)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(
                        new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                                Errors.ConsentRequired,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                                "Interactive user consent is required.",
                        }
                    )
                );
            }

            // Redirect to consent page (preserving all parameters)
            return Redirect($"/Account/Consent{Request.QueryString}");
        }

        // Step 16-17: Create ClaimsPrincipal and save authorization
        var principal = await _signInManager.CreateUserPrincipalAsync(user);

        // Ensure subject claim is properly set
        var identity = principal.Identity as ClaimsIdentity;
        if (identity != null)
        {
            // Remove existing subject claims if any
            var existingSubjectClaims = identity.FindAll(Claims.Subject).ToList();
            foreach (var claim in existingSubjectClaims)
            {
                identity.RemoveClaim(claim);
            }

            // Add the required subject claim
            identity.AddClaim(new Claim(Claims.Subject, await _userManager.GetUserIdAsync(user)));

            // Ensure other required claims are present
            if (!identity.HasClaim(Claims.Name))
            {
                identity.AddClaim(
                    new Claim(
                        Claims.Name,
                        await _userManager.GetUserNameAsync(user) ?? string.Empty
                    )
                );
            }
            if (!identity.HasClaim(Claims.Email))
            {
                identity.AddClaim(
                    new Claim(Claims.Email, await _userManager.GetEmailAsync(user) ?? string.Empty)
                );
            }
        }

        // Set scopes
        principal.SetScopes(scopes);

        // Set resources
        var resources = new List<string>();
        await foreach (var resource in _scopeManager.ListResourcesAsync(scopes))
        {
            resources.Add(resource);
        }
        principal.SetResources(resources);

        if (!authorizations.Any())
        {
            var finalAuths = new List<object>();
            await foreach (
                var auth in _authorizationManager.FindAsync(
                    subject: subject,
                    client: client!,
                    status: Statuses.Valid,
                    type: AuthorizationTypes.Permanent,
                    scopes: scopes
                )
            )
            {
                finalAuths.Add(auth);
            }

            if (!finalAuths.Any())
            {
                try
                {
                    var authorizationDescriptor = new OpenIddictAuthorizationDescriptor
                    {
                        ApplicationId = client,
                        Subject = subject,
                        Type = AuthorizationTypes.Permanent,
                    };

                    authorizationDescriptor.Scopes.UnionWith(scopes);

                    var newAuthorization = await _authorizationManager.CreateAsync(authorizationDescriptor);
                    principal.SetAuthorizationId(await _authorizationManager.GetIdAsync(newAuthorization));

                    _logger.LogInformation(
                        "Created new authorization for user {UserId} and client {ClientId}",
                        user.Id,
                        request.ClientId
                    );
                }
                catch (Exception ex)
                {
                    // Handle race condition - authorization might have been created concurrently
                    _logger.LogWarning(
                        ex,
                        "Failed to create authorization, checking for existing one for user {UserId} and client {ClientId}",
                        user.Id,
                        request.ClientId
                    );

                    // Re-query one more time
                    var retryAuths = new List<object>();
                    await foreach (
                        var auth in _authorizationManager.FindAsync(
                            subject: subject,
                            client: client!,
                            status: Statuses.Valid,
                            type: AuthorizationTypes.Permanent,
                            scopes: scopes
                        )
                    )
                    {
                        retryAuths.Add(auth);
                    }

                    if (retryAuths.Any())
                    {
                        var authorization = retryAuths.Last();
                        principal.SetAuthorizationId(await _authorizationManager.GetIdAsync(authorization));
                        _logger.LogInformation(
                            "Using authorization created concurrently for user {UserId} and client {ClientId}",
                            user.Id,
                            request.ClientId
                        );
                    }
                    else
                    {
                        _logger.LogError(
                            ex,
                            "Failed to create or find authorization for user {UserId} and client {ClientId}",
                            user.Id,
                            request.ClientId
                        );
                        throw; // Re-throw if we can't resolve it
                    }
                }
            }
            else
            {
                var authorization = finalAuths.Last();
                principal.SetAuthorizationId(await _authorizationManager.GetIdAsync(authorization));
               
            }
        }
        else
        {
            var authorization = authorizations.Last();
            principal.SetAuthorizationId(await _authorizationManager.GetIdAsync(authorization));

            _logger.LogInformation(
                "Using existing authorization for user {UserId} and client {ClientId}",
                user.Id,
                request.ClientId
            );
        }

        // Set claim destinations
        foreach (var claim in principal.Claims)
        {
            claim.SetDestinations(GetDestinations(claim, principal));
        }

        // Debug: Log claims to ensure subject is present
        var subjectClaim = principal.FindFirst(Claims.Subject);
        _logger.LogInformation(
            " Principal claims - Subject: {Subject}, Total Claims: {ClaimCount}",
            subjectClaim?.Value ?? "MISSING",
            principal.Claims.Count()
        );

        foreach (var claim in principal.Claims.Take(10)) // Log first 10 claims
        {
            _logger.LogDebug("Claim: {Type} = {Value}", claim.Type, claim.Value);
        }

        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// OAuth2 Token Endpoint
    /// OpenIddict middleware automatically handles token generation
    /// </summary>
    [HttpPost("token")]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> Exchange()
    {
        var request =
            HttpContext.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException(
                "The OpenID Connect request cannot be retrieved."
            );

        if (request.IsAuthorizationCodeGrantType())
        {
            var principal = (
                await HttpContext.AuthenticateAsync(
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme
                )
            ).Principal!;

            // Retrieve the user profile corresponding to the authorization code
            var user = await _userManager.FindByIdAsync(principal.GetClaim(Claims.Subject)!);
            if (user == null)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(
                        new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                                Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                                "The token is no longer valid.",
                        }
                    )
                );
            }

            // Ensure the user is still allowed to sign in
            if (!await _signInManager.CanSignInAsync(user))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(
                        new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                                Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                                "The user is no longer allowed to sign in.",
                        }
                    )
                );
            }

            var identity = new ClaimsIdentity(
                principal.Claims,
                TokenValidationParameters.DefaultAuthenticationType,
                Claims.Name,
                Claims.Role
            );

            identity
                .SetClaim(Claims.Subject, await _userManager.GetUserIdAsync(user))
                .SetClaim(Claims.Email, await _userManager.GetEmailAsync(user))
                .SetClaim(Claims.Name, user.FullName)
                .SetClaim(Claims.PreferredUsername, await _userManager.GetUserNameAsync(user))
                .SetClaim(Claims.GivenName, user.FirstName)
                .SetClaim(Claims.FamilyName, user.LastName)
                .SetClaim("user_type", user.Role.ToString().ToLowerInvariant())
                .SetClaim("platform_role", user.Role.ToString()); 
            var organizationsJson = await _organizationClaimsBuilder.BuildOrganizationsClaimAsync(user.Id);
            identity.SetClaim("organizations", organizationsJson);

            _logger.LogInformation(
                "Added organizations claim to token for user {UserId}",
                user.Id
            );


            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                identity.AddClaim(new Claim(Claims.Role, role));
            }

            // Set destinations for the claims
            var newPrincipal = new ClaimsPrincipal(identity);
            var tokenScopes = principal.GetScopes();
            newPrincipal.SetScopes(tokenScopes);
            newPrincipal.SetResources(principal.GetResources());

            foreach (var claim in newPrincipal.Claims)
            {
                claim.SetDestinations(GetDestinations(claim, newPrincipal));
            }

            
            return SignIn(newPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsPasswordGrantType())
        {
            var username = request.Username;
            var password = request.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(
                        new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                                Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                                "The username and password are required.",
                        }
                    )
                );
            }

            // Find user by email (username)
            var user = await _userManager.FindByEmailAsync(username);
            if (user == null)
            {
                // Try finding by username as fallback
                user = await _userManager.FindByNameAsync(username);
            }

            if (user == null)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(
                        new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                                Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                                "The username or password is incorrect.",
                        }
                    )
                );
            }

            // Validate password
            if (!await _userManager.CheckPasswordAsync(user, password))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(
                        new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                                Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                                "The username or password is incorrect.",
                        }
                    )
                );
            }

            // Ensure the user is still allowed to sign in
            if (!await _signInManager.CanSignInAsync(user))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(
                        new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                                Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                                "The user is no longer allowed to sign in.",
                        }
                    )
                );
            }

            // Get application info for authorization
            var application =
                await _applicationManager.FindByClientIdAsync(request.ClientId!)
                ?? throw new InvalidOperationException(
                    $"Details concerning the calling client application cannot be found."
                );

            // Get scopes from request
            var scopes = request.GetScopes();
            var subject = await _userManager.GetUserIdAsync(user);
            var client = await _applicationManager.GetIdAsync(application);

            // Check for existing authorization or create new one
            var authorizations = new List<object>();
            await foreach (
                var authorization in _authorizationManager.FindAsync(
                    subject: subject,
                    client: client!,
                    status: Statuses.Valid,
                    type: AuthorizationTypes.Permanent,
                    scopes: scopes
                )
            )
            {
                authorizations.Add(authorization);
            }

            if (!authorizations.Any())
            {
                try
                {
                    var authorizationDescriptor = new OpenIddictAuthorizationDescriptor
                    {
                        ApplicationId = client,
                        Subject = subject,
                        Type = AuthorizationTypes.Permanent,
                    };

                    authorizationDescriptor.Scopes.UnionWith(scopes);

                    var newAuthorization = await _authorizationManager.CreateAsync(
                        authorizationDescriptor
                    );
                    authorizations.Add(newAuthorization);

                    _logger.LogInformation(
                        "Created new authorization for password grant - User: {UserId}, Client: {ClientId}",
                        user.Id,
                        request.ClientId
                    );
                }
                catch (Exception ex)
                {
                    var retryAuths = new List<object>();
                    await foreach (
                        var auth in _authorizationManager.FindAsync(
                            subject: subject,
                            client: client!,
                            status: Statuses.Valid,
                            type: AuthorizationTypes.Permanent,
                            scopes: scopes
                        )
                    )
                    {
                        retryAuths.Add(auth);
                    }

                    if (retryAuths.Any())
                    {
                        authorizations.Clear();
                        authorizations.AddRange(retryAuths);
                       
                    }
                    else
                    {
                        throw; 
                    }
                }
            }

            // Create principal for the access token
            var identity = new ClaimsIdentity(
                TokenValidationParameters.DefaultAuthenticationType,
                Claims.Name,
                Claims.Role
            );

            // Set user claims
            identity
                .SetClaim(Claims.Subject, await _userManager.GetUserIdAsync(user))
                .SetClaim(Claims.Email, await _userManager.GetEmailAsync(user))
                .SetClaim(Claims.Name, user.FullName)
                .SetClaim(Claims.PreferredUsername, await _userManager.GetUserNameAsync(user))
                .SetClaim(Claims.GivenName, user.FirstName)
                .SetClaim(Claims.FamilyName, user.LastName)
                .SetClaim("user_type", user.Role.ToString().ToLowerInvariant())
                .SetClaim("platform_role", user.Role.ToString()); 

            var organizationsJson = await _organizationClaimsBuilder.BuildOrganizationsClaimAsync(user.Id);
            identity.SetClaim("organizations", organizationsJson);

            _logger.LogInformation(
                "Added organizations claim to password grant token for user {UserId}",
                user.Id
            );

            // Add roles
            var roles = await _userManager.GetRolesAsync(user);
            foreach (var role in roles)
            {
                identity.AddClaim(new Claim(Claims.Role, role));
            }

            // Set scopes and resources
            var newPrincipal = new ClaimsPrincipal(identity);
            newPrincipal.SetScopes(scopes);

            var resources = new List<string>();
            await foreach (var resource in _scopeManager.ListResourcesAsync(scopes))
            {
                resources.Add(resource);
            }
            newPrincipal.SetResources(resources);

            // Set authorization ID
            var existingAuthorization = authorizations.Last();
            newPrincipal.SetAuthorizationId(
                await _authorizationManager.GetIdAsync(existingAuthorization)
            );

            // Set destinations for the claims
            foreach (var claim in newPrincipal.Claims)
            {
                claim.SetDestinations(GetDestinations(claim, newPrincipal));
            }

            _logger.LogInformation(
                "Password grant successful - User: {UserId}, Client: {ClientId}",
                user.Id,
                request.ClientId
            );

            // Return tokens (OpenIddict generates automatically)
            return SignIn(newPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        if (request.IsRefreshTokenGrantType())
        {
            // Handle refresh token
            var principal = (
                await HttpContext.AuthenticateAsync(
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme
                )
            ).Principal!;
            var user = await _userManager.FindByIdAsync(principal.GetClaim(Claims.Subject)!);

            if (user == null)
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(
                        new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                                Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                                "The refresh token is no longer valid.",
                        }
                    )
                );
            }

            if (!await _signInManager.CanSignInAsync(user))
            {
                return Forbid(
                    authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                    properties: new AuthenticationProperties(
                        new Dictionary<string, string?>
                        {
                            [OpenIddictServerAspNetCoreConstants.Properties.Error] =
                                Errors.InvalidGrant,
                            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                                "The user is no longer allowed to sign in.",
                        }
                    )
                );
            }

            // Create new principal
            var identity = new ClaimsIdentity(
                principal.Claims,
                TokenValidationParameters.DefaultAuthenticationType,
                Claims.Name,
                Claims.Role
            );
            var newPrincipal = new ClaimsPrincipal(identity);
            newPrincipal.SetScopes(principal.GetScopes());
            newPrincipal.SetResources(principal.GetResources());

            // Refresh the organizations claim to ensure it's up-to-date
            if (user != null)
            {
                var organizationsJson = await _organizationClaimsBuilder.BuildOrganizationsClaimAsync(user.Id);

                // Remove old organizations claim if exists
                var existingOrgsClaim = identity.FindFirst("organizations");
                if (existingOrgsClaim != null)
                {
                    identity.RemoveClaim(existingOrgsClaim);
                }

                // Add fresh organizations claim
                identity.AddClaim(new Claim("organizations", organizationsJson));

                _logger.LogInformation(
                    "Refreshed organizations claim for user {UserId}",
                    user.Id
                );
            }

            foreach (var claim in newPrincipal.Claims)
            {
                claim.SetDestinations(GetDestinations(claim, newPrincipal));
            }

            return SignIn(newPrincipal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        return BadRequest(
            new
            {
                error = Errors.UnsupportedGrantType,
                error_description = "The specified grant type is not supported.",
            }
        );
    }

    /// <summary>
    /// OpenID Connect UserInfo Endpoint
    /// Updated for TPT inheritance pattern
    /// </summary>
    [HttpGet("userinfo")]
    [HttpPost("userinfo")]
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    [IgnoreAntiforgeryToken]
    [Produces("application/json")]
    public async Task<IActionResult> Userinfo()
    {
        var user = await _userManager.FindByIdAsync(User.GetClaim(Claims.Subject)!);
        if (user == null)
        {
            return BadRequest(
                new
                {
                    error = Errors.InvalidToken,
                    error_description = "The specified access token is bound to an account that no longer exists.",
                }
            );
        }

        var claims = new Dictionary<string, object>(StringComparer.Ordinal)
        {
            [Claims.Subject] = await _userManager.GetUserIdAsync(user),
        };

        if (User.HasScope(Scopes.Email))
        {
            claims[Claims.Email] = await _userManager.GetEmailAsync(user) ?? string.Empty;
            claims[Claims.EmailVerified] = await _userManager.IsEmailConfirmedAsync(user);
        }

        if (User.HasScope(Scopes.Profile))
        {
            // Use TPT inheritance to get user-specific information
            claims[Claims.Name] = user.FullName;
            claims[Claims.PreferredUsername] =
                await _userManager.GetUserNameAsync(user) ?? string.Empty;
            claims[Claims.GivenName] = user.FirstName;
            claims[Claims.FamilyName] = user.LastName;

            // Add user type information
            claims["user_type"] = user.Role.ToString().ToLowerInvariant();
            claims["platform_role"] = user.Role.ToString();

            // Add organizations data (full detail)
            var organizationsJson = await _organizationClaimsBuilder.BuildOrganizationsClaimAsync(user.Id);
            claims["organizations"] = JsonSerializer.Deserialize<object>(organizationsJson) ?? new object();
        }

        if (User.HasScope(Scopes.Roles))
        {
            claims[Claims.Role] = await _userManager.GetRolesAsync(user);
        }

        return Ok(claims);
    }

    private static IEnumerable<string> GetDestinations(Claim claim, ClaimsPrincipal principal)
    {
        switch (claim.Type)
        {
            case Claims.Name:
                yield return Destinations.AccessToken;
                if (principal.HasScope(Scopes.Profile))
                    yield return Destinations.IdentityToken;
                yield break;

            case Claims.Email:
                yield return Destinations.AccessToken;
                if (principal.HasScope(Scopes.Email))
                    yield return Destinations.IdentityToken;
                yield break;

            case Claims.Role:
                yield return Destinations.AccessToken;
                if (principal.HasScope(Scopes.Roles))
                    yield return Destinations.IdentityToken;
                yield break;

            case "organizations":
                yield return Destinations.AccessToken;
                yield return Destinations.IdentityToken; 
                yield break;

            case "platform_role":
                yield return Destinations.AccessToken;
                if (principal.HasScope(Scopes.Roles))
                    yield return Destinations.IdentityToken;
                yield break;

            case "AspNet.Identity.SecurityStamp":
                yield break;

            default:
                yield return Destinations.AccessToken;
                yield break;
        }
    }
}
