using Identity.Domain.Entities;

namespace Identity.Application.Common.Interfaces.Services;

public interface IInvitationEmailService
{
       Task SendInvitationEmailAsync(
        Invitation invitation,
        int organizationId,
        CancellationToken cancellationToken = default);
}

