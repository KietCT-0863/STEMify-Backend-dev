using Identity.Application.Common.Interfaces.Grpc;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Dtos.BulkProvisioning;
using Identity.Domain.Enums;
using Identity.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Queries.BulkProvisioning.ValidateInvitationToken;

public class ValidateInvitationTokenQueryHandler
    : IRequestHandler<ValidateInvitationTokenQuery, InvitationValidationDto>
{
    private readonly IInvitationRepository _invitationRepository;
    private readonly IOrderLicenseService _orderLicenseService;
    private readonly ILogger<ValidateInvitationTokenQueryHandler> _logger;

    public ValidateInvitationTokenQueryHandler(
        IInvitationRepository invitationRepository,
        IOrderLicenseService orderLicenseService,
        ILogger<ValidateInvitationTokenQueryHandler> logger)
    {
        _invitationRepository = invitationRepository;
        _orderLicenseService = orderLicenseService;
        _logger = logger;
    }

    public async Task<InvitationValidationDto> Handle(
        ValidateInvitationTokenQuery request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Validating invitation token");

        // 1. Find invitation by token
        var invitation = await _invitationRepository.GetByTokenAsync(
            request.Token,
            cancellationToken);

        if (invitation == null)
        {
            throw new InvitationNotFoundException(request.Token);
        }

        // 2. Check if expired
        var isExpired = invitation.IsExpired();
        if (isExpired)
        {
            return new InvitationValidationDto
            {
                IsValid = false,
                ErrorMessage = "Invitation has expired",
                InviteeEmail = invitation.InviteeEmail.Value,
                ExpiresAt = invitation.ExpiresAt
            };
        }

        // 3. Check status
        if (invitation.Status != InvitationStatus.Pending)
        {
            var errorMessage = invitation.Status switch
            {
                InvitationStatus.Accepted => "Invitation has already been accepted",
                InvitationStatus.Revoked => "Invitation has been revoked",
                InvitationStatus.Failed => "Invitation has failed",
                _ => $"Invitation is not in valid state ({invitation.Status})"
            };

            return new InvitationValidationDto
            {
                IsValid = false,
                ErrorMessage = errorMessage,
                InviteeEmail = invitation.InviteeEmail.Value,
                Status = invitation.Status,
                ExpiresAt = invitation.ExpiresAt
            };
        }

        // 4. Get organization info
        var organization = await _orderLicenseService.GetOrganizationAsync(
            invitation.OrganizationId,
            cancellationToken);

        // 5. Return valid invitation details
        return new InvitationValidationDto
        {
            IsValid = true,
            InvitationId = invitation.Id,
            InviteeEmail = invitation.InviteeEmail.Value,
            TargetRole = invitation.TargetRole,
            OrganizationId = invitation.OrganizationId,
            OrganizationName = organization.Name,
            FirstName = invitation.FirstName,
            LastName = invitation.LastName,
            Status = invitation.Status,
            ExpiresAt = invitation.ExpiresAt
        };
    }
}
