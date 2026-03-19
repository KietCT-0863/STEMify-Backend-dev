using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Application.Common.Interfaces.Services;
using Identity.Domain.Enums;
using Identity.Domain.Events;
using Identity.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Commands.BulkProvisioning.ResendInvitation;

/// <summary>
/// Handler for resending invitation email
/// </summary>
public class ResendInvitationCommandHandler : IRequestHandler<ResendInvitationCommand>
{
    private readonly IInvitationRepository _invitationRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ILogger<ResendInvitationCommandHandler> _logger;
    private readonly IInvitationEmailService _invitationEmailService;

    public ResendInvitationCommandHandler(
        IInvitationRepository invitationRepository,
        IIdentityUnitOfWork unitOfWork,
        ILogger<ResendInvitationCommandHandler> logger, IInvitationEmailService invitationEmailService)
    {
        _invitationRepository = invitationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _invitationEmailService = invitationEmailService;
    }

    public async Task Handle(ResendInvitationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Resending invitation {InvitationId}",
            request.InvitationId);

        // 1. Find invitation
        var invitation = await _invitationRepository.FindByIdAsync(
            request.InvitationId,
            cancellationToken);

        if (invitation == null)
        {
            throw new InvitationNotFoundException(request.InvitationId.ToString());
        }

        // 2. Validate invitation can be resent
        if (invitation.Status != InvitationStatus.Pending)
        {
            throw new InvalidInvitationStatusException(
                invitation.Id,
                invitation.Status,
                "resend");
        }

        if (invitation.IsExpired())
        {
            throw new InvitationExpiredException(invitation.Id, invitation.ExpiresAt);
        }

        try
        {
            await _invitationEmailService.SendInvitationEmailAsync(
                invitation,
                invitation.OrganizationId,
                cancellationToken);

            invitation.MarkAsSent();

            await _invitationRepository.UpdateAsync(invitation, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Invitation {InvitationId} resent successfully to {Email}",
                invitation.Id,
                invitation.InviteeEmail.Value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to send invitation email for invitation {InvitationId} to {Email}",
                invitation.Id,
                invitation.InviteeEmail.Value);
            throw; 
        }
    } 
    
}
