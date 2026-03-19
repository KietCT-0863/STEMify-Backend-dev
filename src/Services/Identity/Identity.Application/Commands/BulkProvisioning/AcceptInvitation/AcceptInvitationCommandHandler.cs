using Common.Logging.Metrics;
using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Interfaces.Grpc;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Dtos.Grpc;
using Identity.Application.Dtos.Users;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Events;
using Identity.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Commands.BulkProvisioning.AcceptInvitation;

/// <summary>
/// Handler for accepting invitation and creating user + organization membership
/// </summary>
public class AcceptInvitationCommandHandler : IRequestHandler<AcceptInvitationCommand, UserDto>
{
    private readonly IInvitationRepository _invitationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly IOrganizationUserLicenseReadRepository _licenseReadRepository;
    private readonly IOrderLicenseService _orderLicenseService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ILogger<AcceptInvitationCommandHandler> _logger;

    public AcceptInvitationCommandHandler(
        IInvitationRepository invitationRepository,
        IUserRepository userRepository,
        IOrganizationUserRepository organizationUserRepository,
        IOrganizationUserLicenseReadRepository licenseReadRepository,
        IOrderLicenseService orderLicenseService,
        UserManager<ApplicationUser> userManager,
        IIdentityUnitOfWork unitOfWork,
        ILogger<AcceptInvitationCommandHandler> logger)
    {
        _invitationRepository = invitationRepository;
        _userRepository = userRepository;
        _organizationUserRepository = organizationUserRepository;
        _licenseReadRepository = licenseReadRepository;
        _orderLicenseService = orderLicenseService;
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<UserDto> Handle(
        AcceptInvitationCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing invitation acceptance for email {Email}, token {Token}",
            request.GoogleEmail,
            request.InvitationToken);

        // 1. Find invitation by token
        var invitation = await _invitationRepository.GetByTokenAsync(
            request.InvitationToken,
            cancellationToken);

        if (invitation == null)
        {
            throw new InvitationNotFoundException(request.InvitationToken);
        }

        // 2. Validate invitation
        if (invitation.IsExpired())
        {
            throw new InvitationExpiredException(invitation.Id, invitation.ExpiresAt);
        }

        if (invitation.Status != InvitationStatus.Pending)
        {
            throw new InvalidInvitationStatusException(
                invitation.Id,
                invitation.Status,
                "accept");
        }

        // 3. Validate email matches
        if (!invitation.InviteeEmail.Value.Equals(request.GoogleEmail, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Email mismatch. Invitation is for {invitation.InviteeEmail.Value}, but trying to accept with {request.GoogleEmail}");
        }

        _logger.LogInformation(
            "Invitation {InvitationId} validated successfully for {Email}",
            invitation.Id,
            request.GoogleEmail);

        // 4. Get or create user
        var user = await GetOrCreateUserAsync(request, invitation, cancellationToken);

        // 5. Get organization name for DTO
        var organization = await _orderLicenseService.GetOrganizationForBulkProvisioningAsync(
            invitation.OrganizationId,
            cancellationToken);

        // 6. Determine subscription order ID
        int subscriptionOrderId;
        if (invitation.SubscriptionOrderId.HasValue)
        {
            subscriptionOrderId = invitation.SubscriptionOrderId.Value;
            _logger.LogDebug(
                "Using subscriptionOrderId {SubscriptionOrderId} from invitation {InvitationId}",
                subscriptionOrderId,
                invitation.Id);
        }
        else
        {
            // Find first active subscription
            var activeSubscription = organization.Subscriptions
                .FirstOrDefault(s => s.IsActive);

            if (activeSubscription != null)
            {
                subscriptionOrderId = activeSubscription.SubscriptionOrderId;
            }
            else
            {
                subscriptionOrderId = 0;
                _logger.LogWarning(
                    "No active subscription found for organization {OrganizationId}. Using default subscriptionOrderId 0",
                    invitation.OrganizationId);
            }
        }

        // 7. Check if user is already active in organization 
        var existingOrgUser = await _organizationUserRepository.GetByUserAndOrganizationAsync(
            user.Id,
            invitation.OrganizationId,
            cancellationToken);

        if (existingOrgUser != null)
        {
            // Check if OrganizationUser is already active in this specific subscription
            var isActiveInSubscription = await _licenseReadRepository.IsOrganizationUserActiveInSubscriptionAsync(
                existingOrgUser.Id,
                subscriptionOrderId,
                cancellationToken);

            if (isActiveInSubscription)
            {
                throw new DuplicateOrganizationUserException(user.Id, invitation.OrganizationId);
            }
        }

        // 8. Activate reserved license (Pending -> Active) or create new if not reserved
        var organizationUserId = existingOrgUser?.Id.ToString() ?? string.Empty;
        
        var licenseAssignment = await _orderLicenseService.ActivateReservedLicenseAsync(
            invitation.OrganizationId,
            organizationUserId,
            invitation.LicenseType,
            subscriptionOrderId,
            cancellationToken);

        // If activation failed, try to assign new license (backward compatibility)
        if (!licenseAssignment.Success)
        {
            _logger.LogWarning(
                "Failed to activate reserved license for {Email}, trying to assign new license: {Error}",
                user.Email, licenseAssignment.ErrorMessage);

            licenseAssignment = await _orderLicenseService.AssignLicenseAsync(
                invitation.OrganizationId,
                user.Email!,
                invitation.LicenseType,
                subscriptionOrderId,
                cancellationToken);
        }

        if (!licenseAssignment.Success)
        {
            throw new LicenseAllocationException(
                invitation.OrganizationId,
                invitation.LicenseType,
                licenseAssignment.ErrorMessage ?? "License assignment failed");
        }

        _logger.LogInformation(
            "License {LicenseType} activated/assigned for user {UserId} in organization {OrganizationId}. " +
            "LicenseAssignmentId: {LicenseAssignmentId}",
            invitation.LicenseType,
            user.Id,
            invitation.OrganizationId,
            licenseAssignment.LicenseAssignmentId);

        // 9. Create or update OrganizationUser
        OrganizationUser orgUser;
        if (existingOrgUser != null)
        {
            orgUser = existingOrgUser;
            _logger.LogInformation(
                "Using existing OrganizationUser {OrgUserId} for user {UserId}",
                orgUser.Id, user.Id);
        }
        else
        {
            orgUser = OrganizationUser.Create(
                userId: user.Id,
                organizationId: invitation.OrganizationId,
                organizationRole: invitation.TargetRole,
                licenseType: invitation.LicenseType,
                licenseAssignmentId: licenseAssignment.LicenseAssignmentId?.ToString(),
                subscriptionOrderId: subscriptionOrderId);

            await _organizationUserRepository.AddAsync(orgUser, cancellationToken);
            _logger.LogInformation(
                "Created new OrganizationUser {OrgUserId} for user {UserId}",
                orgUser.Id, user.Id);
        }

        // 10. Activate user (if not already Active or Deleted)
        if (user.Status != UserStatus.Active && user.Status != UserStatus.Deleted)
        {
            user.Activate();
        }
        user.ConfirmEmail();

        // 11. Mark invitation as accepted (will publish InvitationAcceptedEvent)
        invitation.MarkAsAccepted(user.Id);
        await _invitationRepository.UpdateAsync(invitation, cancellationToken);

        // 12. Save all changes with domain events via Outbox
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "User {UserId} successfully accepted invitation {InvitationId} and joined organization {OrganizationId}",
            user.Id,
            invitation.Id,
            invitation.OrganizationId);


        IdentityMetrics.RecordBulkInvitationProcessed("accepted");
        if (user.CreatedAt >= DateTime.UtcNow.AddMinutes(-5)) // Newly created user
        {
            IdentityMetrics.RecordUserRegistration(user.Role.ToString());
        }

        // 11. Return UserDto
        return new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            FirstName = user.FirstName,
            LastName = user.LastName,
            FullName = $"{user.FirstName} {user.LastName}".Trim(),
            Role = user.Role,
            OrganizationRole = invitation.TargetRole,
            ProfilePictureUrl = request.ProfilePictureUrl,
            EmailConfirmed = user.EmailConfirmed,
            CreatedAt = user.CreatedAt,
            OrganizationId = invitation.OrganizationId,
            OrganizationName = organization.Name
        };
    }

    /// <summary>
    /// Get existing user or create new one based on Google authentication
    /// </summary>
    private async Task<ApplicationUser> GetOrCreateUserAsync(
        AcceptInvitationCommand request,
        Invitation invitation,
        CancellationToken cancellationToken)
    {
        try
        { // Try to find existing user by email
            var existingUser = await _userManager.FindByEmailAsync(request.GoogleEmail);
            if (existingUser != null)
            {
                _logger.LogInformation(
                    "Found existing user {UserId} with email {Email}",
                    existingUser.Id,
                    request.GoogleEmail);

                // Existing user found - they can accept invitation for a new organization
                return existingUser;
            }

            // Extract name info
            var firstName = request.FirstName ?? invitation.FirstName ?? "User";
            var lastName = request.LastName ?? invitation.LastName ?? string.Empty;

            // Create new user based on role using factory methods
            var newUser = User.Create(
                id: Guid.NewGuid(),
                email: request.GoogleEmail,
                userName: request.GoogleEmail,
                firstName: firstName,
                lastName: lastName,
                role: UserRole.Member);

            // Set email as confirmed (Google SSO verified)
            newUser.EmailConfirmed = true;

            var createResult = await _userManager.CreateAsync(newUser);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create user: {errors}");
            }

            // Assign role
            await _userManager.AddToRoleAsync(newUser, UserRole.Member.ToString());

            _logger.LogInformation(
                "Created new user {UserId} with role {Role} for email {Email}",
                newUser.Id,
                invitation.TargetRole,
                request.GoogleEmail);

            return newUser;
        } catch (Exception e) { Console.WriteLine(e);
            throw;
        }
       
    }
}
