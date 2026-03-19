using System.Security.Claims;
using Identity.Application.Auth.Commands.ProcessAuthorization;
using Identity.Application.Common.Interfaces;
using Identity.Domain.Entities;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Identity.Infrastructure.Services;

public class AuthorizationProcessingService : IAuthorizationProcessingService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictAuthorizationManager _authorizationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly IHttpContextAccessor _httpContext;

    public AuthorizationProcessingService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictScopeManager scopeManager,
        IHttpContextAccessor httpContext
    )
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _applicationManager = applicationManager;
        _authorizationManager = authorizationManager;
        _scopeManager = scopeManager;
        _httpContext = httpContext;
    }

    public async Task<AuthorizationResult> ProcessAuthorizationAsync(
        ProcessAuthorizationCommand request
    )
    {
        var http = _httpContext.HttpContext!;
        var oidRequest =
            http.GetOpenIddictServerRequest()
            ?? throw new InvalidOperationException("Cannot retrieve OIDC request.");

        // 1) prompt=login?
        var promptParam = oidRequest.GetParameter("prompt")?.ToString();
        var prompts = string.IsNullOrEmpty(promptParam)
            ? Array.Empty<string>()
            : promptParam.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (prompts.Contains("login"))
        {
            // remove prompt=login and challenge to Identity
            var newPrompts = prompts.Where(p => p != "login").ToArray();
            var qs = http.Request.HasFormContentType
                ? http.Request.Form.Where(p => p.Key != "prompt").ToList()
                : http.Request.Query.Where(p => p.Key != "prompt").ToList();
            qs.Add(KeyValuePair.Create("prompt", new StringValues(string.Join(" ", newPrompts))));
            var redirect = http.Request.PathBase + http.Request.Path + QueryString.Create(qs);
            return AuthorizationResult.Challenge(IdentityConstants.ApplicationScheme, redirect);
        }

        // 2) authenticate cookie & max_age
        var auth = await http.AuthenticateAsync(IdentityConstants.ApplicationScheme);
        var maxAge = oidRequest.MaxAge;
        if (
            !auth.Succeeded
            || (
                maxAge.HasValue
                && auth.Properties?.IssuedUtc.HasValue == true
                && DateTimeOffset.UtcNow - auth.Properties.IssuedUtc.Value
                    > TimeSpan.FromSeconds(maxAge.Value)
            )
        )
        {
            if (prompts.Contains("none"))
                return AuthorizationResult.ForbidConsentRequired();

            var redirect =
                http.Request.PathBase
                + http.Request.Path
                + QueryString.Create(
                    http.Request.HasFormContentType
                        ? http.Request.Form.ToList()
                        : http.Request.Query.ToList()
                );
            return AuthorizationResult.Challenge(IdentityConstants.ApplicationScheme, redirect);
        }

        // 3) retrieve user + application
        var user =
            await _userManager.GetUserAsync(auth.Principal!)
            ?? throw new InvalidOperationException("User not found.");
        var application =
            await _applicationManager.FindByClientIdAsync(request.ClientId!)
            ?? throw new InvalidOperationException("Client not found.");

        // 4) find existing permanent authorizations - simplified for now
        var subject = await _userManager.GetUserIdAsync(user);
        var clientId = await _applicationManager.GetIdAsync(application);
        var scopes = oidRequest.GetScopes();

        // Skip complex authorization lookup for now
        var authorizationsList = new List<object>();
        await foreach (
            var authorization in _authorizationManager.FindAsync(
                subject,
                clientId!,
                Statuses.Valid,
                AuthorizationTypes.Permanent,
                scopes
            )
        )
        {
            authorizationsList.Add(authorization);
        }

        // 5) consent flow
        var consentType = await _applicationManager.GetConsentTypeAsync(application);
        if (consentType == ConsentTypes.External && !authorizationsList.Any())
            return AuthorizationResult.ForbidConsentRequired();

        if (
            consentType == ConsentTypes.Implicit
            || (consentType == ConsentTypes.External && authorizationsList.Any())
            || (
                consentType == ConsentTypes.Explicit
                && authorizationsList.Any()
                && !prompts.Contains("consent")
            )
        )
        {
            // build principal & sign in
            var principal = await _signInManager.CreateUserPrincipalAsync(user);
            principal.SetScopes(scopes);

            var resourcesList = new List<string>();
            await foreach (var resource in _scopeManager.ListResourcesAsync(scopes))
            {
                resourcesList.Add(resource);
            }
            principal.SetResources(resourcesList);

            // create permanent auth if needed
            var authorization = authorizationsList.LastOrDefault();
            if (authorization == null)
            {
                var authDescriptor = new OpenIddictAuthorizationDescriptor();
                authDescriptor.Subject = subject;
                authDescriptor.ApplicationId = clientId!;
                authDescriptor.Type = AuthorizationTypes.Permanent;
                authDescriptor.Scopes.UnionWith(scopes);

                authorization = await _authorizationManager.CreateAsync(authDescriptor);
            }

            // ensure sub claim
            var userIdentity = (ClaimsIdentity)principal.Identity!;
            if (userIdentity.FindFirst(Claims.Subject) is null)
                userIdentity.AddClaim(new Claim(Claims.Subject, subject));

            if (authorization != null)
            {
                var authId = await _authorizationManager.GetIdAsync(authorization);
                principal.SetAuthorizationId(authId);
            }

            // set destinations
            foreach (var claim in principal.Claims)
                claim.SetDestinations(GetDestinations(claim, principal));

            return AuthorizationResult.SignIn(principal);
        }

        // 6) prompt=none + explicit/systematic without auth ⇒ forbid consent
        if (
            (consentType is ConsentTypes.Explicit or ConsentTypes.Systematic)
            && prompts.Contains("none")
        )
            return AuthorizationResult.ForbidConsentRequired();

        // 7) mọi trường hợp còn lại: redirect to consent form
        var consentUrl = $"/Account/Consent" + http.Request.QueryString;
        return AuthorizationResult.Redirect(consentUrl);
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

            // Never include the security stamp in the access and identity tokens, as it's a secret value.
            case "AspNet.Identity.SecurityStamp":
                yield break;

            default:
                yield return Destinations.AccessToken;
                yield break;
        }
    }
}
