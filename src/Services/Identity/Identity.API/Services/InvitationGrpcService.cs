using Grpc.Core;
using Identity.Application.Commands.BulkProvisioning.AcceptInvitation;
using Identity.Application.Commands.BulkProvisioning.InviteSingleUser;
using Identity.Application.Commands.BulkProvisioning.ResendInvitation;
using Identity.Application.Commands.BulkProvisioning.RevokeInvitation;
using Identity.Application.Common.Interfaces.Services;
using Identity.Application.Queries.BulkProvisioning.ValidateInvitationToken;
using Identity.Domain.Enums;
using MediatR;
using Shared.Protos.User;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Identity.API.Services;


public class InvitationGrpcService : GrpcInvitation.GrpcInvitationBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<InvitationGrpcService> _logger;
    private readonly IGoogleOAuthService _googleOAuthService;
    private readonly IOAuthStateService _oAuthStateService;
    private readonly IConfiguration _configuration;

    public InvitationGrpcService(
        IMediator mediator,
        ILogger<InvitationGrpcService> logger,
        IGoogleOAuthService googleOAuthService,
        IOAuthStateService oAuthStateService,
        IConfiguration configuration)
    {
        _mediator = mediator;
        _logger = logger;
        _googleOAuthService = googleOAuthService;
        _oAuthStateService = oAuthStateService;
        _configuration = configuration;
    }

    /// <summary>
    /// Accept an invitation via Google SSO
    /// </summary>
    public override async Task<AcceptInvitationResponse> AcceptInvitation(
        AcceptInvitationRequest request,
        ServerCallContext context)
    {
        try
        {
            var command = new AcceptInvitationCommand
            {
                InvitationToken = request.InvitationToken,
                GoogleEmail = request.GoogleEmail,
                GoogleId = request.GoogleId,
                FirstName = request.FirstName,
                LastName = request.LastName,
                ProfilePictureUrl = request.ProfilePictureUrl
            };

            var result = await _mediator.Send(command, context.CancellationToken);

            return new AcceptInvitationResponse
            {
                Id = result.Id.ToString(),
                Email = result.Email,
                FirstName = result.FirstName ?? string.Empty,
                LastName = result.LastName ?? string.Empty,
                FullName = result.FullName ?? string.Empty,
                Role = result.Role.ToString(),
                ProfilePictureUrl = result.ProfilePictureUrl ?? string.Empty,
                EmailConfirmed = result.EmailConfirmed,
                CreatedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                    DateTime.SpecifyKind(result.CreatedAt, DateTimeKind.Utc)),
                OrganizationId = result.OrganizationId ?? 0,
                OrganizationName = result.OrganizationName ?? string.Empty
            };
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Invitation not found: {Token}", request.InvitationToken);
            throw new RpcException(new Status(StatusCode.NotFound, "Invitation not found"));
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid invitation state: {Token}", request.InvitationToken);
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error accepting invitation: {Token}", request.InvitationToken);
            throw new RpcException(new Status(StatusCode.Internal,
                "An error occurred while accepting invitation"));
        }
    }

    /// <summary>
    /// Resend an invitation email
    /// </summary>
    public override async Task<ResendInvitationResponse> ResendInvitation(
        ResendInvitationRequest request,
        ServerCallContext context)
    {
        try
        {
            var command = new ResendInvitationCommand
            {
                InvitationId = Guid.Parse(request.InvitationId)
            };

            await _mediator.Send(command, context.CancellationToken);

            return new ResendInvitationResponse
            {
                IsSuccess = true,
                Message = "Invitation email resent successfully"
            };
        }
        catch (KeyNotFoundException)
        {
            throw new RpcException(new Status(StatusCode.NotFound,
                $"Invitation {request.InvitationId} not found"));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resending invitation {InvitationId}", request.InvitationId);
            throw new RpcException(new Status(StatusCode.Internal,
                "An error occurred while resending invitation"));
        }
    }

    /// <summary>
    /// Revoke an invitation
    /// </summary>
    public override async Task<RevokeInvitationResponse> RevokeInvitation(
        RevokeInvitationRequest request,
        ServerCallContext context)
    {
        try
        {
            var command = new RevokeInvitationCommand
            {
                InvitationId = Guid.Parse(request.InvitationId),
                Reason = request.Reason
            };

            await _mediator.Send(command, context.CancellationToken);

            return new RevokeInvitationResponse
            {
                IsSuccess = true,
                Message = "Invitation revoked successfully"
            };
        }
        catch (KeyNotFoundException)
        {
            throw new RpcException(new Status(StatusCode.NotFound,
                $"Invitation {request.InvitationId} not found"));
        }
        catch (InvalidOperationException ex)
        {
            throw new RpcException(new Status(StatusCode.FailedPrecondition, ex.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error revoking invitation {InvitationId}", request.InvitationId);
            throw new RpcException(new Status(StatusCode.Internal,
                "An error occurred while revoking invitation"));
        }
    }

    /// <summary>
    /// Validate invitation token (before user logs in)
    /// </summary>
    public override async Task<InvitationValidationResponse> ValidateInvitationToken(
        ValidateInvitationTokenRequest request,
        ServerCallContext context)
    {
        try
        {
            var query = new ValidateInvitationTokenQuery
            {
                Token = request.Token
            };

            var result = await _mediator.Send(query, context.CancellationToken);

            var response = new InvitationValidationResponse
            {
                IsValid = result.IsValid,
                ErrorMessage = result.IsValid ? string.Empty : (result.ErrorMessage ?? "Invalid invitation"),
                InvitationId = result.InvitationId?.ToString() ?? string.Empty
            };

            return response;
        }
        catch (KeyNotFoundException)
        {
            throw new RpcException(new Status(StatusCode.NotFound, "Invitation not found"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating invitation token: {Token}", request.Token);
            throw new RpcException(new Status(StatusCode.Internal,
                "An error occurred while validating invitation"));
        }
    }

    /// <summary>
    /// List invitations by organization
    /// </summary>
    public override async Task<ListInvitationsResponse> ListInvitations(
        ListInvitationsRequest request,
        ServerCallContext context)
    {
        try
        {
            var query = new Identity.Application.Queries.BulkProvisioning.ListInvitations.ListInvitationsQuery
            {
                OrganizationId = request.OrganizationId,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            var result = await _mediator.Send(query, context.CancellationToken);

            var response = new ListInvitationsResponse
            {
                TotalCount = result.TotalCount,
                PageNumber = result.PageNumber,
                PageSize = result.PageSize
            };

            foreach (var item in result.Items)
            {
                response.Items.Add(new InvitationSummary
                {
                    InvitationId = item.InvitationId.ToString(),
                    InviteeEmail = item.InviteeEmail,
                    Accepted = item.Accepted,
                    InvitedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                        DateTime.SpecifyKind(item.InvitedAt, DateTimeKind.Utc))
                });
            }

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing invitations for organization {OrganizationId}",
                request.OrganizationId);
            throw new RpcException(new Status(StatusCode.Internal,
                "An error occurred while retrieving invitations"));
        }
    }

    /// <summary>
    /// Initiate invitation acceptance via backend-driven Google OAuth
    /// Returns redirect URL 
    /// </summary>
    public override async Task<InitiateInvitationAcceptanceResponse> InitiateInvitationAcceptance(
        InitiateInvitationAcceptanceRequest request,
        ServerCallContext context)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Token))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "Invitation token is required"));
            }

            // 1. Validate invitation token exists and is not expired
            var validationQuery = new ValidateInvitationTokenQuery { Token = request.Token };
            var validationResult = await _mediator.Send(validationQuery, context.CancellationToken);

            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Invalid invitation token: {Token}. Reason: {Reason}",
                    request.Token, validationResult.ErrorMessage);

                var frontendUrl = _configuration["ClientApp"] ?? "https://localhost:3000";
                var errorUrl = $"{frontendUrl}/invitation/error?message={Uri.EscapeDataString(validationResult.ErrorMessage ?? "Invalid invitation token")}";

                return new InitiateInvitationAcceptanceResponse { RedirectUrl = errorUrl };
            }

            _logger.LogInformation("Initiating OAuth flow for invitation token {Token}", request.Token);

            // 2. Generate PKCE challenge
            var (codeVerifier, codeChallenge) = _googleOAuthService.GeneratePKCEChallenge();

            // 3. Create signed OAuth state containing invitation token and code verifier
            var state = _oAuthStateService.CreateState(request.Token, codeVerifier);

            // 4. Build Google OAuth authorization URL
            var authUrl = _googleOAuthService.BuildAuthorizationUrl(state, codeChallenge);

            _logger.LogInformation("Redirecting to Google OAuth for invitation {InvitationId}",
                validationResult.InvitationId);

            // 5. Return redirect URL (API Gateway or client will perform actual redirect)
            return new InitiateInvitationAcceptanceResponse { RedirectUrl = authUrl };
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating invitation acceptance for token {Token}", request.Token);

            var frontendUrl = _configuration["ClientApp"] ?? "https://localhost:3000";
            var errorUrl = $"{frontendUrl}/invitation/error?message={Uri.EscapeDataString("An error occurred while processing your invitation")}";

            return new InitiateInvitationAcceptanceResponse { RedirectUrl = errorUrl };
        }
    }

    /// <summary>
    /// Handle OAuth callback from Google
    /// Verifies code, extracts user info, accepts invitation, returns redirect URL to frontend
    /// </summary>
    public override async Task<HandleOAuthCallbackResponse> HandleOAuthCallback(
        HandleOAuthCallbackRequest request,
        ServerCallContext context)
    {
        var frontendUrl = _configuration["ClientApp"] ?? "https://localhost:3000";

        try
        {
            // 1. Check for OAuth errors
            if (!string.IsNullOrEmpty(request.Error))
            {
                _logger.LogWarning("OAuth error from Google: {Error}", request.Error);
                return CreateErrorResponse($"Google authentication failed: {request.Error}", frontendUrl);
            }

            if (string.IsNullOrWhiteSpace(request.Code) || string.IsNullOrWhiteSpace(request.State))
            {
                _logger.LogWarning("Missing code or state in OAuth callback");
                return CreateErrorResponse("Invalid OAuth callback parameters", frontendUrl);
            }

            // 2. Validate and decrypt OAuth state
            if (!_oAuthStateService.ValidateState(request.State, out var invitationToken, out var codeVerifier))
            {
                _logger.LogWarning("Invalid or expired OAuth state");
                return CreateErrorResponse("Invalid or expired invitation link. Please request a new invitation.", frontendUrl);
            }

            _logger.LogInformation("Processing OAuth callback for invitation token {Token}", invitationToken);

            // 3. Exchange authorization code for ID token and access token
            var (idToken, accessToken) = await _googleOAuthService.ExchangeCodeForTokensAsync(
                request.Code,
                codeVerifier,
                context.CancellationToken);

            // 4. Verify ID token and extract claims
            var claims = await _googleOAuthService.VerifyIdTokenAsync(idToken, context.CancellationToken);

            // 5. Extract user information from claims
            var (googleId, email, firstName, lastName, profilePictureUrl) =
                _googleOAuthService.ExtractUserInfo(claims);

            _logger.LogInformation("Extracted user info from Google: {Email}", email);

            // 6. Accept invitation and create user account
            var acceptCommand = new AcceptInvitationCommand
            {
                InvitationToken = invitationToken,
                GoogleEmail = email,
                GoogleId = googleId,
                FirstName = firstName,
                LastName = lastName,
                ProfilePictureUrl = profilePictureUrl
            };

            var userResult = await _mediator.Send(acceptCommand, context.CancellationToken);

            _logger.LogInformation("Successfully accepted invitation for user {Email}, ID: {UserId}",
                email, userResult.Id);

            // 7. Return redirect URL to frontend with success
            var successUrl = $"{frontendUrl}/invitation/success?user_id={Uri.EscapeDataString(userResult.Id.ToString())}";

            return new HandleOAuthCallbackResponse
            {
                RedirectUrl = successUrl,
                IsSuccess = true,
                Message = "Invitation accepted successfully"
            };
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Invitation not found during callback");
            return CreateErrorResponse("Invitation not found or has already been used", frontendUrl);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during invitation acceptance");
            return CreateErrorResponse(ex.Message, frontendUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing OAuth callback");
            return CreateErrorResponse("An unexpected error occurred. Please try again or contact support.", frontendUrl);
        }
    }

    public override async Task<InviteUsersResponse> InviteUsers(
        InviteUsersRequest request,
        ServerCallContext context)
    {
        try
        {
            var requesterId = Guid.Parse(ExtractUserIdOrThrow(context));

            // Parse organization ID
            if (!int.TryParse(request.OrganizationId, out var organizationId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "organization_id must be an integer"));
            }

            var response = new InviteUsersResponse();

            foreach (var item in request.Users)
            {
                try
                {
                    // Parse role
                    if (!Enum.TryParse<Identity.Domain.Enums.OrganizationRole>(item.Role, true, out var role))
                    {
                        throw new RpcException(new Status(StatusCode.InvalidArgument,
                            $"Invalid role: {item.Role}. Must be Student, Teacher, or OrganizationAdmin"));
                    }

                    // Parse subscription order ID if provided
                    int? subscriptionOrderId = null;
                    if (!string.IsNullOrWhiteSpace(item.SubscriptionOrderId))
                    {
                        if (!int.TryParse(item.SubscriptionOrderId, out var subId))
                        {
                            throw new RpcException(new Status(StatusCode.InvalidArgument,
                                "subscription_order_id must be an integer when provided"));
                        }
                        subscriptionOrderId = subId;
                    }

                    var command = new InviteSingleUserCommand
                    {
                        OrganizationId = organizationId,
                        Email = item.Email,
                        Role = role,
                        LicenseType = string.IsNullOrWhiteSpace(item.LicenseType) ? null : item.LicenseType,
                        FirstName = string.IsNullOrWhiteSpace(item.FirstName) ? null : item.FirstName,
                        LastName = string.IsNullOrWhiteSpace(item.LastName) ? null : item.LastName,
                        FullName = string.IsNullOrWhiteSpace(item.FullName) ? null : item.FullName,
                        GroupName = string.IsNullOrWhiteSpace(item.GroupName) ? null : item.GroupName,
                        ExternalId = string.IsNullOrWhiteSpace(item.ExternalId) ? null : item.ExternalId,
                        InvitedBy = requesterId,
                        SubscriptionOrderId = subscriptionOrderId,
                        ExpirationDays = item.ExpirationDays > 0 ? item.ExpirationDays : 30
                    };

                    var result = await _mediator.Send(command, context.CancellationToken);

                    response.Items.Add(new InviteSingleUserResponse
                    {
                        InvitationId = result.InvitationId.ToString(),
                        Email = result.Email,
                        Role = result.Role,
                        LicenseType = result.LicenseType,
                        InvitedAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                            DateTime.SpecifyKind(result.InvitedAt, DateTimeKind.Utc)),
                        ExpiresAt = Google.Protobuf.WellKnownTypes.Timestamp.FromDateTime(
                            DateTime.SpecifyKind(result.ExpiresAt, DateTimeKind.Utc)),
                        EmailSent = result.EmailSent,
                        InvitationToken = result.InvitationToken ?? string.Empty
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to invite user {Email} in batch", item.Email);
                    // Skip failed item; continue processing next
                    continue;
                }
            }

            return response;
        }
        catch (RpcException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inviting users for organization {OrganizationId}", request.OrganizationId);
            throw new RpcException(new Status(StatusCode.Internal,
                "An error occurred while processing your invitation list"));
        }
    }

    private static string ExtractUserIdOrThrow(ServerCallContext context)
    {
        var httpContext = context.GetHttpContext();

        // Try principal claims first
        var principal = httpContext.User;
        var userId = principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                     ?? principal?.FindFirst("sub")?.Value
                     ?? httpContext.Request.Headers["X-User-Id"].FirstOrDefault();

        // Fallback: parse Authorization header (no signature validation)
        if (string.IsNullOrWhiteSpace(userId))
        {
            var authHeader = httpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(authHeader))
            {
                var token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                    ? authHeader.Substring("Bearer ".Length)
                    : authHeader;
                var handler = new JwtSecurityTokenHandler();
                if (handler.CanReadToken(token))
                {
                    var jwt = handler.ReadJwtToken(token);
                    userId = jwt.Claims.FirstOrDefault(c => c.Type == "sub")?.Value
                          ?? jwt.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value;
                }
            }
        }

        if (string.IsNullOrWhiteSpace(userId))
            throw new RpcException(new Status(StatusCode.Unauthenticated, "Missing user identity"));

        return userId;
    }

    private static HandleOAuthCallbackResponse CreateErrorResponse(string errorMessage, string frontendUrl)
    {
        var errorUrl = $"{frontendUrl}/invitation/error?message={Uri.EscapeDataString(errorMessage)}";
        return new HandleOAuthCallbackResponse
        {
            RedirectUrl = errorUrl,
            IsSuccess = false,
            Message = errorMessage
        };
    }
}
