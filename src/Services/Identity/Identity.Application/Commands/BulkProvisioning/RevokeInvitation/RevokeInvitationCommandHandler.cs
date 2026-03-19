using Identity.Application.Common.Interfaces;
using Identity.Application.Common.Interfaces.Repositories;
using Identity.Domain.Enums;
using Identity.Domain.Exceptions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Identity.Application.Commands.BulkProvisioning.RevokeInvitation;

public class RevokeInvitationCommandHandler : IRequestHandler<RevokeInvitationCommand>
{
    private readonly IInvitationRepository _invitationRepository;
    private readonly IIdentityUnitOfWork _unitOfWork;
    private readonly ILogger<RevokeInvitationCommandHandler> _logger;

    public RevokeInvitationCommandHandler(
        IInvitationRepository invitationRepository,
        IIdentityUnitOfWork unitOfWork,
        ILogger<RevokeInvitationCommandHandler> logger)
    {
        _invitationRepository = invitationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task Handle(RevokeInvitationCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Revoking invitation {InvitationId}. Revoked by: {RevokedBy}, Reason: {Reason}",
            request.InvitationId,
            request.RevokedBy,
            request.Reason);

        // 1. Find invitation
        var invitation = await _invitationRepository.FindByIdAsync(
            request.InvitationId,
            cancellationToken);

        if (invitation == null)
        {
            throw new InvitationNotFoundException(request.InvitationId.ToString());
        }

        // 2. Validate invitation can be revoked
        if (invitation.Status != InvitationStatus.Pending)
        {
            throw new InvalidInvitationStatusException(
                invitation.Id,
                invitation.Status,
                "revoke");
        }

        // 3. Mark as revoked
        invitation.Revoke();

        // 4. Save changes
        await _invitationRepository.UpdateAsync(invitation, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Invitation {InvitationId} revoked successfully",
            invitation.Id);
    }
}
