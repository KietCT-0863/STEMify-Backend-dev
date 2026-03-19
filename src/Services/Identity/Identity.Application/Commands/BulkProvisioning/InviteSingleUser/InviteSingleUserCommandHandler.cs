using Common.Logging.Metrics;
using Elastic.CommonSchema;
using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Interfaces.Grpc;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Common.Interfaces.Services;
using Identity.Application.Dtos.BulkProvisioning;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Commands.BulkProvisioning.InviteSingleUser;

public class InviteSingleUserCommandHandler : IRequestHandler<InviteSingleUserCommand, SingleInvitationDto>
{
    private readonly IInvitationRepository _invitationRepository;
    private readonly IUserRepository _userRepository;
    private readonly IOrganizationUserRepository _organizationUserRepository;
    private readonly IOrderLicenseService _orderLicenseService;
    private readonly IInvitationEmailService _invitationEmailService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ILogger<InviteSingleUserCommandHandler> _logger;

    public InviteSingleUserCommandHandler(
        IInvitationRepository invitationRepository,
        IUserRepository userRepository,
        IOrganizationUserRepository organizationUserRepository,
        IOrderLicenseService orderLicenseService,
        IInvitationEmailService invitationEmailService,
        UserManager<ApplicationUser> userManager,
        IIdentityUnitOfWork unitOfWork,
        ILogger<InviteSingleUserCommandHandler> logger)
    {
        _invitationRepository = invitationRepository;
        _userRepository = userRepository;
        _organizationUserRepository = organizationUserRepository;
        _orderLicenseService = orderLicenseService;
        _invitationEmailService = invitationEmailService;
        _userManager = userManager;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<SingleInvitationDto> Handle(
        InviteSingleUserCommand request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Processing single user invitation for {Email} in organization {OrganizationId}",
            request.Email,
            request.OrganizationId);

        // 1. Get organization info and resolve subscription to use
        var organization = await _orderLicenseService.GetOrganizationForBulkProvisioningAsync(
            request.OrganizationId, cancellationToken);

        if (!organization.IsActive)
        {
            throw new InvalidOperationException(
                $"Organization {request.OrganizationId} is not active");
        }

        int subscriptionOrderIdToUse;
        if (request.SubscriptionOrderId.HasValue)
        {
            var specified = organization.Subscriptions.FirstOrDefault(s => s.SubscriptionOrderId == request.SubscriptionOrderId.Value);
            if (specified != null && specified.IsActive)
            {
                subscriptionOrderIdToUse = specified.SubscriptionOrderId;
            }
            else
            {
                var fallback = organization.Subscriptions.FirstOrDefault(s => s.IsActive);
                if (fallback == null)
                {
                    _logger.LogError("Specified subscription {SubId} not valid/active and no active subscription found for organization {OrganizationId}", 
                        request.SubscriptionOrderId, request.OrganizationId);
                    throw new InvalidOperationException($"No valid subscription found for organization {request.OrganizationId}");
                }
                _logger.LogWarning("Provided SubscriptionOrderId {SubId} but it is not valid/active for organization {OrganizationId}. Falling back to {FallbackSubId}", 
                    request.SubscriptionOrderId, request.OrganizationId, fallback.SubscriptionOrderId);
                subscriptionOrderIdToUse = fallback.SubscriptionOrderId;
            }
        }
        else
        {
            var active = organization.Subscriptions.FirstOrDefault(s => s.IsActive);
            if (active == null)
            {
                _logger.LogError("No active subscription found for organization {OrganizationId}", request.OrganizationId);
                throw new InvalidOperationException($"No active subscription found for organization {request.OrganizationId}");
            }
            subscriptionOrderIdToUse = active.SubscriptionOrderId;
        }

        var existingInvitation = await _invitationRepository.ExistsForEmailAndSubscriptionAsync(
            request.OrganizationId,
            request.Email,
            subscriptionOrderIdToUse,
            cancellationToken);

        if (existingInvitation)
        {
            
            throw new InvalidOperationException(
                $"An invitation already exists for {request.Email} with subscription {subscriptionOrderIdToUse} in this organization");
        }

        // 3. Get or create user (Status = Pending)
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null)
        {
            // Create user with Pending status
            var firstName = request.FirstName ?? "User";
            var lastName = request.LastName ?? string.Empty;
            var password = $"{Guid.NewGuid():N}aA!1"; // Temporary password

            var newUser = Identity.Domain.Entities.User.Create(
                id: Guid.NewGuid(),
                email: request.Email,
                userName: request.Email,
                firstName: firstName,
                lastName: lastName,
                role: UserRole.Member);

            // User is created with Status = Pending (default from ApplicationUser constructor)
            // EmailConfirmed = false (default)
            var createResult = await _userManager.CreateAsync(newUser, password);
            if (!createResult.Succeeded)
            {
                var errors = string.Join(", ", createResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create user: {errors}");
            }

            await _userManager.AddToRoleAsync(newUser, UserRole.Member.ToString());
            user = newUser;

            _logger.LogInformation(
                "Created user {UserId} with Pending status for {Email}",
                user.Id, request.Email);
        }
        else
        {
            // User exists - ensure it's in Pending status if not active
            if (user.Status != UserStatus.Active && user.Status != UserStatus.Pending)
            {
                _logger.LogWarning(
                    "User {UserId} exists but with status {Status}. Cannot invite.",
                    user.Id, user.Status);
                throw new InvalidOperationException(
                    $"User with email {request.Email} exists but has status {user.Status}. Cannot invite.");
            }
            _logger.LogDebug(
                "User {UserId} already exists for {Email}",
                user.Id, request.Email);
        }

        // 4. Check if OrganizationUser already exists for this organization
        var existingOrgUser = await _organizationUserRepository.GetByUserAndOrganizationAsync(
            user.Id, 
            request.OrganizationId, 
            cancellationToken);
        OrganizationUser orgUser;
        // 5. Determine license type
        var licenseType = string.IsNullOrEmpty(request.LicenseType) ? request.Role.ToString() : request.LicenseType;

        if (existingOrgUser != null)
        {
            orgUser = existingOrgUser;
        } else
        {
        // 6. Create Pending OrganizationUser
        orgUser = OrganizationUser.CreatePending(
            organizationId: request.OrganizationId,
            userId: user.Id,
            organizationRole: request.Role,
            licenseType: licenseType,
            licenseAssignmentId: null,
            subscriptionOrderId: subscriptionOrderIdToUse);

        await _organizationUserRepository.AddAsync(orgUser, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
       
        

        // 7. Reserve license 
        var licenseResult = await _orderLicenseService.ReserveLicenseAsync(
            request.OrganizationId,
            orgUser.Id.ToString(),
            licenseType,
            subscriptionOrderIdToUse,
            cancellationToken);

        if (!licenseResult.Success)
        {
            throw new LicenseAllocationException(
                request.OrganizationId,
                licenseType,
                licenseResult.ErrorMessage ?? "License reservation failed");
        }



        // 7. Create invitation
        var invitation = Invitation.Create(
            organizationId: request.OrganizationId,
            email: request.Email,
            targetRole: request.Role,
            licenseType: licenseType,
            invitedBy: request.InvitedBy,
            processedByJobId: null, 
            fullName: request.FullName,
            firstName: request.FirstName,
            lastName: request.LastName,
            groupName: request.GroupName,
            externalId: request.ExternalId,
            subscriptionOrderId: subscriptionOrderIdToUse,
            expirationDays: request.ExpirationDays
        );

        if (invitation == null)
        {
            throw new InvalidOperationException(
                $"Failed to create invitation for {request.Email}. Invitation.Create returned null.");
        }

        await _invitationRepository.AddAsync(invitation, cancellationToken);

        // 8. Save all changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Successfully created invitation for {Email} (InvitationId: {InvitationId}). UserId: {UserId}, OrgUserId: {OrgUserId}, LicenseAssignmentId: {LicenseId}",
            request.Email,
            invitation.Id,
            user.Id,
            orgUser.Id,
            licenseResult.LicenseAssignmentId);

        // 9. Send invitation email
        var emailSent = false;
        try
        {
            await _invitationEmailService.SendInvitationEmailAsync(
                invitation,
                request.OrganizationId,
                cancellationToken);
            
            invitation.MarkAsSent();
            emailSent = true;
            
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            IdentityMetrics.RecordBulkInvitationProcessed("sent");
            
            _logger.LogInformation(
                "Invitation email sent successfully to {Email} for invitation {InvitationId}",
                request.Email,
                invitation.Id);
        }
        catch (Exception)
        {
            IdentityMetrics.RecordBulkInvitationProcessed("failed");
            
            // Continue - invitation is saved, email can be resent later
        }

        // 10. Return DTO
        return new SingleInvitationDto
        {
            InvitationId = invitation.Id,
            Email = invitation.InviteeEmail.Value,
            Role = invitation.TargetRole.ToString(),
            LicenseType = invitation.LicenseType,
            InvitedAt = invitation.InvitedAt,
            ExpiresAt = invitation.ExpiresAt,
            EmailSent = emailSent,
            InvitationToken = invitation.Token.Value
        };
    }
}

